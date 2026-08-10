namespace CommunityIntranet.Modules.Identity.Services;

public sealed class IdentityPublicOptions
{
    public const string SectionName = "Identity";
    public string PublicAppUrl { get; set; } = "http://localhost:5173";
}

public sealed class IdentityEmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "CouchClash";
    public bool UseSsl { get; set; } = true;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

public sealed class ExternalLoginOptions
{
    public const string SectionName = "ExternalLogin";
    public OAuthProviderOptions Google { get; set; } = new();
    public OAuthProviderOptions Discord { get; set; } = new();
    public string SteamApiKey { get; set; } = string.Empty;
}

public sealed class OAuthProviderOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
