namespace StorageConnector.Infrastructure.Config;

public sealed class GoogleOAuthOptions
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string AccessType { get; init; } = "offline";
    public string Prompt { get; init; } = "consent";
}
