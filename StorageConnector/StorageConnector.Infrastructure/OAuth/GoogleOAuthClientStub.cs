using StorageConnector.Application.Interfaces;
using StorageConnector.Domain;

namespace StorageConnector.Infrastructure.OAuth;

public sealed class GoogleOAuthClientStub : IOAuthClient {
    public ProviderType Provider => ProviderType.Google;
    public string BuildAuthorizeUrl(string state, string codeChallenge, Uri redirect, string[] scopes)
        => $"https://example.com/google/auth?state={state}&cc={codeChallenge}&redirect={Uri.EscapeDataString(redirect.ToString())}&scope={Uri.EscapeDataString(string.Join(' ', scopes))}";
    public Task<TokenSet> ExchangeCodeAsync(string code, string v, Uri r)
        => throw new NotImplementedException("Implement real Google OAuth client");
    public Task<TokenSet> RefreshAsync(string refreshToken) 
        => throw new NotImplementedException("Implement real Google refresh");
    public Task RevokeAsync(string refreshToken) 
        => Task.CompletedTask;
}