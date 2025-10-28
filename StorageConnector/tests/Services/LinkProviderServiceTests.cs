using Application;
using Application.Interfaces;
using Domain;
using Xunit;

namespace Tests.Services;

public sealed class LinkProviderServiceTests
{
    [Fact]
    public async Task StartAsync_PersistsStateAndBuildsAuthorizeUrl()
    {
        var oauth = new RecordingOAuthClient(ProviderType.Google);
        var stateStore = new FakeStateStore();
        var tokenStore = new FakeTokenStore();
        var service = new LinkProviderService(new[] { oauth }, tokenStore, stateStore);

        var redirectUri = new Uri("https://api.test/connect/google/callback");
        var scopes = new[] { "scope-a", "scope-b" };

        var result = await service.StartAsync("user-1", ProviderType.Google, redirectUri, scopes);

        Assert.Equal(oauth.AuthorizeResponse, result.ToString());
        Assert.Equal(ProviderType.Google, stateStore.LastSavedProvider);
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
            service.ConnectCallbackAsync("user-1", "missing-state", "code", new Uri("https://api.test/connect/google/callback")));
    }

    [Fact]
    public async Task ConnectCallbackAsync_StoresTokens()
    {
        var oauth = new RecordingOAuthClient(ProviderType.Google)
        {
            ExchangeResult = new TokenSet("access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1), new[] { "scope-a", "scope-b" })
        };

        var stateStore = new FakeStateStore();
        stateStore.Seed("expected-state", "code-verifier", ProviderType.Google);

        var tokenStore = new FakeTokenStore();
        var service = new LinkProviderService(new[] { oauth }, tokenStore, stateStore);

        await service.ConnectCallbackAsync("user-1", "expected-state", "code-123", new Uri("https://api.test/connect/google/callback"));

        Assert.Equal("code-123", oauth.LastCode);
        Assert.Equal("code-verifier", oauth.LastCodeVerifier);
        Assert.Equal("refresh-1", tokenStore.LastDecryptedRefreshToken);

        var stored = tokenStore.GetStored("user-1", ProviderType.Google);
        Assert.NotNull(stored);
        Assert.Equal("enc:refresh-1", stored!.EncryptedRefreshToken);
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
        private readonly Dictionary<string, (string verifier, ProviderType provider)> _states = new();

        public string? LastSavedState { get; private set; }
        public string? LastSavedVerifier { get; private set; }
        public ProviderType LastSavedProvider { get; private set; }

        public Task SaveAsync(string state, string codeVerifier, ProviderType provider, TimeSpan ttl)
        {
            LastSavedState = state;
            LastSavedVerifier = codeVerifier;
            LastSavedProvider = provider;
            _states[state] = (codeVerifier, provider);
            return Task.CompletedTask;
        }

        public Task<(string codeVerifier, ProviderType provider)?> TakeAsync(string state)
        {
            if (_states.TryGetValue(state, out var entry))
            {
                _states.Remove(state);
                return Task.FromResult<(string, ProviderType)?>(entry);
            }

            return Task.FromResult<(string, ProviderType)?>(null);
        }

        public void Seed(string state, string verifier, ProviderType provider)
        {
            _states[state] = (verifier, provider);
        }
    }

    private sealed class FakeTokenStore : ITokenStore
    {
        private readonly Dictionary<(string userId, ProviderType provider), ProviderAccount> _accounts = new();

        public string? LastDecryptedRefreshToken { get; private set; }

        public Task<ProviderAccount?> GetAsync(string userId, ProviderType provider)
        {
            _accounts.TryGetValue((userId, provider), out var account);
            return Task.FromResult(account);
        }

        public Task UpsertAsync(ProviderAccount account)
        {
            _accounts[(account.UserId, account.Provider)] = account;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string userId, ProviderType provider)
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

        public ProviderAccount? GetStored(string userId, ProviderType provider) =>
            _accounts.TryGetValue((userId, provider), out var account) ? account : null;
    }
}
