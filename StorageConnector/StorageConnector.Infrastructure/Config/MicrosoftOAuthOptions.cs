namespace StorageConnector.Infrastructure.Config;

public sealed class MicrosoftOAuthOptions
{
    public string TenantId { get; init; } = "common";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
