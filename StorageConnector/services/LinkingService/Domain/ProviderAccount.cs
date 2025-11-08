namespace LinkingService.Domain;

public sealed class ProviderAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public ProviderType Provider { get; init; }
    public string EncryptedRefreshToken { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public string ScopeCsv { get; private set; } = "";

    public void UpdateFrom(TokenSet t, Func<string, string> encrypt)
    {
        EncryptedRefreshToken = encrypt(t.RefreshToken);
        ExpiresAtUtc = t.ExpiresAtUtc;
        ScopeCsv = string.Join(' ', t.Scopes);
    }
}
