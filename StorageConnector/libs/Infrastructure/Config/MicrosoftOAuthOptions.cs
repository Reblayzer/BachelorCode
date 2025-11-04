namespace Infrastructure.Config;

public sealed class MicrosoftOAuthOptions
{
    public string TenantId { get; init; } = "common";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    // Optional prompt parameter passed to the /authorize endpoint. Use "select_account"
    // to force an account chooser instead of silently signing in with the browser's
    // currently signed-in account.
    public string? Prompt { get; init; }
}
