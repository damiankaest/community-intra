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
}
