using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityIntranet.Modules.ThemePacks.Configuration;

public sealed class ThemePackSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false
    };

    public string Serialize(ThemePackConfiguration configuration)
    {
        var json = JsonSerializer.Serialize(configuration, SerializerOptions);
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
                    SerializerOptions)
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

    private void EnsureValid(
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
