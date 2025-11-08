using LinkingService.Application.Services;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;

namespace LinkingService.Tests.Services;

public sealed class LinkProviderServiceTests
{
    [Fact]
    public async Task StartAsync_PersistsStateAndBuildsAuthorizeUrl()
    {
        var oauth = new RecordingOAuthClient(ProviderType.Google);
        var stateStore = new FakeStateStore();
        var tokenStore = new FakeTokenStore();
        var service = new LinkProviderService(new[] { oauth }, tokenStore, stateStore);

        var userId = Guid.NewGuid();
        var redirectUri = new Uri("https://api.test/connect/google/callback");
        var scopes = new[] { "scope-a", "scope-b" };

        var result = await service.StartAsync(userId, ProviderType.Google, redirectUri, scopes);

        Assert.Equal(oauth.AuthorizeResponse, result.ToString());
        Assert.Equal(ProviderType.Google, stateStore.LastSavedProvider);
        Assert.Equal(userId, stateStore.LastSavedUserId);
        Assert.Equal(oauth.LastState, stateStore.LastSavedState);
        Assert.Equal(redirectUri, oauth.LastRedirectUri);
        Assert.Equal(scopes, oauth.LastScopes);
        Assert.False(string.IsNullOrWhiteSpace(stateStore.LastSavedVerifier));
        Assert.False(string.IsNullOrWhiteSpace(oauth.LastCodeChallenge));
    }

    [Fact]
    public async Task ConnectCallbackAsync_Throws_WhenStateMissing()
    {
        var oauth = new RecordingOAuthClient(ProviderType.Google);
        var service = new LinkProviderService(new[] { oauth }, new FakeTokenStore(), new FakeStateStore());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConnectCallbackAsync("missing-state", "code", new Uri("https://api.test/connect/google/callback")));
    }

    [Fact]
    public async Task ConnectCallbackAsync_StoresTokens()
    {
        var oauth = new RecordingOAuthClient(ProviderType.Google)
        {
            ExchangeResult = new TokenSet("access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1), new[] { "scope-a", "scope-b" })
        };

        var userId = Guid.NewGuid();
        var stateStore = new FakeStateStore();
        stateStore.Seed("expected-state", userId, "code-verifier", ProviderType.Google);

        var tokenStore = new FakeTokenStore();
        var service = new LinkProviderService(new[] { oauth }, tokenStore, stateStore);

        await service.ConnectCallbackAsync("expected-state", "code-123", new Uri("https://api.test/connect/google/callback"));

        Assert.Equal("code-123", oauth.LastCode);
        Assert.Equal("code-verifier", oauth.LastCodeVerifier);
        Assert.Equal("refresh-1", tokenStore.LastDecryptedRefreshToken);

        var stored = tokenStore.GetStored(userId, ProviderType.Google);
        Assert.NotNull(stored);
        Assert.Equal("enc:refresh-1", stored.EncryptedRefreshToken);
        Assert.Equal("scope-a scope-b", stored.ScopeCsv);
    }

    private sealed class RecordingOAuthClient : IOAuthClient
    {
        public RecordingOAuthClient(ProviderType provider)
        {
            Provider = provider;
        }

        public ProviderType Provider { get; }
        public string AuthorizeResponse { get; init; } = "https://accounts.test/authorize";

        public string? LastState { get; private set; }
        public string? LastCodeChallenge { get; private set; }
        public Uri? LastRedirectUri { get; private set; }
        public string[]? LastScopes { get; private set; }
        public string? LastCode { get; private set; }
        public string? LastCodeVerifier { get; private set; }

        public TokenSet ExchangeResult { get; init; } =
            new("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(30), new[] { "scope" });

        public string BuildAuthorizeUrl(string state, string codeChallenge, Uri redirectUri, string[] scopes)
        {
            LastState = state;
            LastCodeChallenge = codeChallenge;
            LastRedirectUri = redirectUri;
            LastScopes = scopes;
            return AuthorizeResponse;
        }

        public Task<TokenSet> ExchangeCodeAsync(string code, string codeVerifier, Uri redirectUri)
        {
            LastCode = code;
            LastCodeVerifier = codeVerifier;
            LastRedirectUri = redirectUri;
            return Task.FromResult(ExchangeResult);
        }

        public Task<TokenSet> RefreshAsync(string refreshToken) => Task.FromResult(ExchangeResult);

        public Task RevokeAsync(string refreshToken) => Task.CompletedTask;
    }

    private sealed class FakeStateStore : IStateStore
    {
        private readonly Dictionary<string, (Guid userId, string verifier, ProviderType provider)> _states = new();

        public string? LastSavedState { get; private set; }
        public Guid LastSavedUserId { get; private set; }
        public string? LastSavedVerifier { get; private set; }
        public ProviderType LastSavedProvider { get; private set; }

        public Task SaveAsync(string state, Guid userId, string codeVerifier, ProviderType provider, TimeSpan ttl)
        {
            LastSavedState = state;
            LastSavedUserId = userId;
            LastSavedVerifier = codeVerifier;
            LastSavedProvider = provider;
            _states[state] = (userId, codeVerifier, provider);
            return Task.CompletedTask;
        }

        public Task<(Guid userId, string codeVerifier, ProviderType provider)?> TakeAsync(string state)
        {
            if (_states.TryGetValue(state, out var entry))
            {
                _states.Remove(state);
                return Task.FromResult<(Guid, string, ProviderType)?>(entry);
            }

            return Task.FromResult<(Guid, string, ProviderType)?>(null);
        }

        public void Seed(string state, Guid userId, string verifier, ProviderType provider)
        {
            _states[state] = (userId, verifier, provider);
        }
    }

    private sealed class FakeTokenStore : ITokenStore
    {
        private readonly Dictionary<(Guid userId, ProviderType provider), ProviderAccount> _accounts = new();

        public string? LastDecryptedRefreshToken { get; private set; }

        public Task<ProviderAccount?> GetAsync(Guid userId, ProviderType provider)
        {
            _accounts.TryGetValue((userId, provider), out var account);
            return Task.FromResult(account);
        }

        public Task<IReadOnlyList<ProviderAccount>> GetAllByUserAsync(Guid userId)
        {
            var accounts = _accounts
                .Where(kv => kv.Key.userId == userId)
                .Select(kv => kv.Value)
                .ToList();
            return Task.FromResult<IReadOnlyList<ProviderAccount>>(accounts);
        }

        public Task UpsertAsync(ProviderAccount account)
        {
            _accounts[(account.UserId, account.Provider)] = account;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid userId, ProviderType provider)
        {
            _accounts.Remove((userId, provider));
            return Task.CompletedTask;
        }

        public string Encrypt(string plaintext)
        {
            LastDecryptedRefreshToken = plaintext;
            return $"enc:{plaintext}";
        }

        public string Decrypt(string ciphertext) => ciphertext.Replace("enc:", "", StringComparison.Ordinal);

        public ProviderAccount? GetStored(Guid userId, ProviderType provider) =>
            _accounts.TryGetValue((userId, provider), out var account) ? account : null;
    }
}
