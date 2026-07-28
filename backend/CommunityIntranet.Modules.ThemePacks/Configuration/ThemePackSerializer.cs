using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityIntranet.Modules.ThemePacks.Configuration;

public sealed class ThemePackSerializer
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false
    };

    public string Serialize(ThemePackConfiguration configuration)
    {
        var json = JsonSerializer.Serialize(configuration, _serializerOptions);
        EnsureValid(configuration, Encoding.UTF8.GetByteCount(json));
        return json;
    }

    public ThemePackConfiguration Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        ThemePackConfiguration configuration;
        try
        {
            configuration = JsonSerializer.Deserialize<ThemePackConfiguration>(
                    json,
                    _serializerOptions)
                ?? throw new InvalidDataException(
                    "Theme pack configuration cannot be null.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "Theme pack configuration is not valid JSON.",
                exception);
        }

        EnsureValid(configuration, Encoding.UTF8.GetByteCount(json));
        return configuration;
    }

    private static void EnsureValid(
        ThemePackConfiguration configuration,
        int serializedByteCount)
    {
        var result = ThemePackConfigurationValidator.Validate(
            configuration,
            serializedByteCount);
        if (!result.IsValid)
        {
            throw new InvalidDataException(
                $"Theme pack configuration is invalid: {string.Join(" ", result.Errors)}");
        }
    }
}
