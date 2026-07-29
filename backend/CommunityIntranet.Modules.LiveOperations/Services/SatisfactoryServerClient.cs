using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using CommunityIntranet.BuildingBlocks.LiveOperations;

namespace CommunityIntranet.Modules.LiveOperations.Services;

public sealed class SatisfactoryServerClient(TimeProvider timeProvider)
    : ISatisfactoryServerClient
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private const int MaximumSaveBytes = 200 * 1024 * 1024;

    public async Task<LiveServerStatus> ProbeAsync(
        SatisfactoryServerTarget target,
        CancellationToken cancellationToken)
    {
        var checkedAt = timeProvider.GetUtcNow();
        if (!ServerAddressPolicy.IsValidHost(target.Host)
            || target.Port is < 1 or > 65535)
        {
            return Failure(
                LiveServerConnectionState.ConfigurationError,
                target,
                checkedAt,
                "Host oder Port sind ungültig.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(
                target.Host,
                cancellationToken);
        }
        catch (SocketException)
        {
            return Failure(
                LiveServerConnectionState.Offline,
                target,
                checkedAt,
                "Der Servername konnte nicht aufgelöst werden.");
        }

        var publicAddresses = addresses
            .Where(ServerAddressPolicy.IsPublicAddress)
            .Distinct()
            .ToArray();
        if (publicAddresses.Length == 0)
        {
            return Failure(
                LiveServerConnectionState.ConfigurationError,
                target,
                checkedAt,
                "Die Adresse ist aus Sicherheitsgründen nicht als öffentliches Serverziel erlaubt.");
        }

        string? presentedFingerprint = null;
        var certificateRejected = false;
        var expectedFingerprint = NormalizeFingerprint(
            target.CertificateFingerprint);
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectCallback = (context, token) =>
                ConnectAsync(publicAddresses, context.DnsEndPoint.Port, token)
        };
        handler.SslOptions.RemoteCertificateValidationCallback =
            (_, certificate, _, errors) =>
            {
                if (certificate is null)
                {
                    certificateRejected = true;
                    return false;
                }

                presentedFingerprint = certificate.GetCertHashString(
                    HashAlgorithmName.SHA256);
                if (expectedFingerprint is null)
                {
                    var accepted = errors == SslPolicyErrors.None;
                    certificateRejected = !accepted;
                    return accepted;
                }

                var matches = FingerprintsEqual(
                    expectedFingerprint,
                    presentedFingerprint);
                certificateRejected = !matches;
                return matches;
            };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new UriBuilder(
                Uri.UriSchemeHttps,
                target.Host,
                target.Port,
                "/api/v1").Uri,
            Timeout = RequestTimeout
        };

        try
        {
            using var healthResponse = await SendAsync(
                httpClient,
                "HealthCheck",
                new { ClientCustomData = "community-intra" },
                apiToken: null,
                cancellationToken);
            if (!healthResponse.IsSuccessStatusCode)
            {
                return await ApiFailureAsync(
                    healthResponse,
                    target,
                    checkedAt,
                    presentedFingerprint,
                    cancellationToken);
            }

            using var healthJson = await ReadJsonAsync(
                healthResponse,
                cancellationToken);
            var health = ReadString(
                GetData(healthJson.RootElement),
                "health");

            if (string.IsNullOrWhiteSpace(target.ApiToken))
            {
                return new LiveServerStatus(
                    LiveServerConnectionState.Reachable,
                    target.DisplayName,
                    target.Host,
                    target.Port,
                    health,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    checkedAt,
                    "Der Server antwortet. Für Spieldaten fehlt noch ein API-Token.",
                    presentedFingerprint);
            }

            using var stateResponse = await SendAsync(
                httpClient,
                "QueryServerState",
                new { },
                target.ApiToken,
                cancellationToken);
            if (!stateResponse.IsSuccessStatusCode)
            {
                return await ApiFailureAsync(
                    stateResponse,
                    target,
                    checkedAt,
                    presentedFingerprint,
                    cancellationToken);
            }

            using var stateJson = await ReadJsonAsync(
                stateResponse,
                cancellationToken);
            var data = GetData(stateJson.RootElement);
            var state = GetObject(data, "serverGameState");
            return new LiveServerStatus(
                LiveServerConnectionState.Online,
                target.DisplayName,
                target.Host,
                target.Port,
                health,
                ReadString(state, "activeSessionName"),
                ReadInt32(state, "numConnectedPlayers"),
                ReadInt32(state, "playerLimit"),
                ReadInt32(state, "techTier"),
                SimplifyAssetName(ReadString(state, "activeSchematic")),
                SimplifyGamePhase(ReadString(state, "gamePhase")),
                ReadBoolean(state, "isGameRunning"),
                ReadBoolean(state, "isGamePaused"),
                ReadInt64(state, "totalGameDuration"),
                ReadDouble(state, "averageTickRate"),
                checkedAt,
                "Der Server ist erreichbar und liefert aktuelle Spieldaten.",
                presentedFingerprint);
        }
        catch (HttpRequestException) when (certificateRejected)
        {
            var state = expectedFingerprint is null
                ? LiveServerConnectionState.UntrustedCertificate
                : LiveServerConnectionState.CertificateChanged;
            var message = expectedFingerprint is null
                ? "Der Server nutzt ein noch nicht bestätigtes Zertifikat."
                : "Das Serverzertifikat stimmt nicht mehr mit dem bestätigten Fingerprint überein.";
            return Failure(
                state,
                target,
                checkedAt,
                message,
                presentedFingerprint);
        }
        catch (HttpRequestException)
        {
            return Failure(
                LiveServerConnectionState.Offline,
                target,
                checkedAt,
                "Der Server ist über HTTPS gerade nicht erreichbar.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(
                LiveServerConnectionState.Offline,
                target,
                checkedAt,
                "Der Server hat nicht rechtzeitig geantwortet.");
        }
        catch (JsonException)
        {
            return Failure(
                LiveServerConnectionState.Offline,
                target,
                checkedAt,
                "Die Serverantwort hatte ein unbekanntes Format.");
        }
    }

    public async Task<ServerSaveDownloadResult> DownloadSaveAsync(
        SatisfactoryServerTarget target,
        string? saveName,
        CancellationToken cancellationToken)
    {
        if (!ServerAddressPolicy.IsValidHost(target.Host)
            || target.Port is < 1 or > 65535
            || string.IsNullOrWhiteSpace(target.ApiToken))
        {
            return SaveFailure(
                ServerSaveDownloadState.ConfigurationError,
                "Host, Port oder API-Token fehlen.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(
                target.Host,
                cancellationToken);
        }
        catch (SocketException)
        {
            return SaveFailure(
                ServerSaveDownloadState.Unavailable,
                "Der Servername konnte nicht aufgelöst werden.");
        }

        var publicAddresses = addresses
            .Where(ServerAddressPolicy.IsPublicAddress)
            .Distinct()
            .ToArray();
        if (publicAddresses.Length == 0)
        {
            return SaveFailure(
                ServerSaveDownloadState.ConfigurationError,
                "Die Serveradresse ist aus Sicherheitsgründen nicht erlaubt.");
        }

        var certificateRejected = false;
        var expectedFingerprint = NormalizeFingerprint(
            target.CertificateFingerprint);
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectCallback = (context, token) =>
                ConnectAsync(publicAddresses, context.DnsEndPoint.Port, token)
        };
        handler.SslOptions.RemoteCertificateValidationCallback =
            (_, certificate, _, errors) =>
            {
                if (certificate is null)
                {
                    certificateRejected = true;
                    return false;
                }

                var presented = certificate.GetCertHashString(
                    HashAlgorithmName.SHA256);
                var accepted = expectedFingerprint is null
                    ? errors == SslPolicyErrors.None
                    : FingerprintsEqual(expectedFingerprint, presented);
                certificateRejected = !accepted;
                return accepted;
            };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new UriBuilder(
                Uri.UriSchemeHttps,
                target.Host,
                target.Port,
                "/api/v1").Uri,
            Timeout = TimeSpan.FromMinutes(5)
        };

        try
        {
            using var sessionsResponse = await SendAsync(
                client,
                "EnumerateSessions",
                new { },
                target.ApiToken,
                cancellationToken);
            if (sessionsResponse.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden)
            {
                return SaveFailure(
                    ServerSaveDownloadState.AuthenticationFailed,
                    "Das API-Token darf Spielstände nicht lesen.");
            }

            if (!sessionsResponse.IsSuccessStatusCode)
            {
                return SaveFailure(
                    ServerSaveDownloadState.Unavailable,
                    "Die Save-Liste konnte nicht vom Gameserver geladen werden.");
            }

            using var sessions = await ReadJsonAsync(
                sessionsResponse,
                cancellationToken);
            var availableNames = FindSaveNames(sessions.RootElement)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var requestedName = saveName?.Trim();
            var selectedName = string.IsNullOrWhiteSpace(requestedName)
                ? availableNames.LastOrDefault()
                : availableNames.FirstOrDefault(name =>
                    name.Equals(
                        requestedName,
                        StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                return SaveFailure(
                    ServerSaveDownloadState.NotFound,
                    availableNames.Length == 0
                        ? "Der Gameserver meldet noch keinen Spielstand."
                        : "Der gewählte Spielstand wurde nicht gefunden.");
            }

            using var downloadResponse = await SendAsync(
                client,
                "DownloadSaveGame",
                new { SaveName = selectedName },
                target.ApiToken,
                cancellationToken);
            if (downloadResponse.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden)
            {
                return SaveFailure(
                    ServerSaveDownloadState.AuthenticationFailed,
                    "Das API-Token darf Spielstände nicht herunterladen.");
            }

            if (!downloadResponse.IsSuccessStatusCode)
            {
                return SaveFailure(
                    downloadResponse.StatusCode == HttpStatusCode.NotFound
                        ? ServerSaveDownloadState.NotFound
                        : ServerSaveDownloadState.Unavailable,
                    "Der Spielstand konnte nicht heruntergeladen werden.");
            }

            if (downloadResponse.Content.Headers.ContentLength
                is > MaximumSaveBytes)
            {
                return SaveFailure(
                    ServerSaveDownloadState.ConfigurationError,
                    "Der Spielstand ist größer als 200 MB.");
            }

            var content = await ReadWithLimitAsync(
                downloadResponse.Content,
                MaximumSaveBytes,
                cancellationToken);
            var fileName = selectedName.EndsWith(
                ".sav",
                StringComparison.OrdinalIgnoreCase)
                ? selectedName
                : $"{selectedName}.sav";
            return new ServerSaveDownloadResult(
                ServerSaveDownloadState.Downloaded,
                fileName,
                content,
                "Der aktuelle Spielstand wurde heruntergeladen.");
        }
        catch (HttpRequestException) when (certificateRejected)
        {
            return SaveFailure(
                ServerSaveDownloadState.CertificateError,
                "Das Serverzertifikat ist nicht bestätigt oder hat sich geändert.");
        }
        catch (HttpRequestException)
        {
            return SaveFailure(
                ServerSaveDownloadState.Unavailable,
                "Der Gameserver ist über HTTPS gerade nicht erreichbar.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SaveFailure(
                ServerSaveDownloadState.Unavailable,
                "Der Download hat zu lange gedauert.");
        }
        catch (JsonException)
        {
            return SaveFailure(
                ServerSaveDownloadState.Unavailable,
                "Die Save-Liste des Gameservers hatte ein unbekanntes Format.");
        }
        catch (InvalidDataException)
        {
            return SaveFailure(
                ServerSaveDownloadState.ConfigurationError,
                "Der Spielstand ist größer als 200 MB.");
        }
    }

    private static async ValueTask<Stream> ConnectAsync(
        IReadOnlyList<IPAddress> addresses,
        int port,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(
                address.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp)
            {
                NoDelay = true
            };
            try
            {
                await socket.ConnectAsync(
                    new IPEndPoint(address, port),
                    cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException exception)
            {
                socket.Dispose();
                lastException = exception;
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();
                throw;
            }
        }

        throw new HttpRequestException(
            "No approved server address was reachable.",
            lastException);
    }

    private static IEnumerable<string> FindSaveNames(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(
                        "saveName",
                        StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                    && property.Value.GetString() is { Length: > 0 } value)
                {
                    yield return value;
                }

                foreach (var nested in FindSaveNames(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in FindSaveNames(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static async Task<byte[]> ReadWithLimitAsync(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        await using var source = await content.ReadAsStreamAsync(
            cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(
                buffer,
                cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (destination.Length + read > maximumBytes)
            {
                throw new InvalidDataException("Save exceeds configured limit.");
            }

            await destination.WriteAsync(
                buffer.AsMemory(0, read),
                cancellationToken);
        }

        return destination.ToArray();
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string function,
        object data,
        string? apiToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        if (!string.IsNullOrWhiteSpace(apiToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiToken);
        }

        request.Content = JsonContent.Create(new { function, data });
        return await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    private static async Task<LiveServerStatus> ApiFailureAsync(
        HttpResponseMessage response,
        SatisfactoryServerTarget target,
        DateTimeOffset checkedAt,
        string? fingerprint,
        CancellationToken cancellationToken)
    {
        var state = response.StatusCode is HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden
            ? LiveServerConnectionState.AuthenticationFailed
            : LiveServerConnectionState.Offline;
        var message = state == LiveServerConnectionState.AuthenticationFailed
            ? "Das API-Token fehlt, ist ungültig oder hat nicht genug Rechte."
            : "Der Gameserver hat die Statusabfrage abgelehnt.";

        try
        {
            using var document = await ReadJsonAsync(
                response,
                cancellationToken);
            var errorMessage = ReadString(
                document.RootElement,
                "errorMessage");
            if (!string.IsNullOrWhiteSpace(errorMessage)
                && state != LiveServerConnectionState.AuthenticationFailed)
            {
                message = errorMessage;
            }
        }
        catch (JsonException)
        {
            // The stable user-facing error above is safer than leaking HTML.
        }

        return Failure(state, target, checkedAt, message, fingerprint);
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        return await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);
    }

    private static JsonElement GetData(JsonElement root) =>
        GetObject(root, "data");

    private static JsonElement GetObject(JsonElement parent, string name) =>
        TryGetProperty(parent, name, out var value)
            && value.ValueKind == JsonValueKind.Object
            ? value
            : default;

    private static string? ReadString(JsonElement parent, string name) =>
        TryGetProperty(parent, name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt32(JsonElement parent, string name) =>
        TryGetProperty(parent, name, out var value)
            && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static long? ReadInt64(JsonElement parent, string name) =>
        TryGetProperty(parent, name, out var value)
            && value.TryGetInt64(out var parsed)
            ? parsed
            : null;

    private static double? ReadDouble(JsonElement parent, string name) =>
        TryGetProperty(parent, name, out var value)
            && value.TryGetDouble(out var parsed)
            ? parsed
            : null;

    private static bool? ReadBoolean(JsonElement parent, string name) =>
        TryGetProperty(parent, name, out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static bool TryGetProperty(
        JsonElement parent,
        string name,
        out JsonElement value)
    {
        if (parent.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in parent.EnumerateObject())
            {
                if (property.Name.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? SimplifyGamePhase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        const string marker = "Phase_";
        var index = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            var digits = new string(
                value[(index + marker.Length)..]
                    .TakeWhile(char.IsDigit)
                    .ToArray());
            if (digits.Length > 0)
            {
                return $"Phase {digits}";
            }
        }

        return SimplifyAssetName(value);
    }

    private static string? SimplifyAssetName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var separator = value.LastIndexOf('.');
        var name = separator >= 0 ? value[(separator + 1)..] : value;
        return name
            .Trim('\'')
            .Replace("_C", "", StringComparison.Ordinal)
            .Replace('_', ' ');
    }

    private static string? NormalizeFingerprint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new string(
            value.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        return normalized.Length == 64 ? normalized : null;
    }

    private static bool FingerprintsEqual(string expected, string presented)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(presented));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static LiveServerStatus Failure(
        LiveServerConnectionState state,
        SatisfactoryServerTarget target,
        DateTimeOffset checkedAt,
        string message,
        string? fingerprint = null) =>
        new(
            state,
            target.DisplayName,
            target.Host,
            target.Port,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            checkedAt,
            message,
            fingerprint);

    private static ServerSaveDownloadResult SaveFailure(
        ServerSaveDownloadState state,
        string message) =>
        new(state, null, null, message);
}
