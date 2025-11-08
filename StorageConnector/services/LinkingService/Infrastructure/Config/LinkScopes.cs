using LinkingService.Domain;

namespace LinkingService.Infrastructure.Config;

public sealed class LinkScopes
{
    public string[] For(ProviderType p) => p == ProviderType.Google
        ? new[] { "openid", "email", "profile", "https://www.googleapis.com/auth/drive.readonly" }
        : new[] { "offline_access", "Files.Read", "Sites.Read.All" };
}
