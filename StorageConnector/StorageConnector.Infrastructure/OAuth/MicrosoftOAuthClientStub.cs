using StorageConnector.Application.Interfaces;
using StorageConnector.Domain;

namespace StorageConnector.Infrastructure.OAuth;

public sealed class MicrosoftOAuthClientStub : IOAuthClient {
    public ProviderType Provider => ProviderType.Microsoft;
    public string BuildAuthorizeUrl(string state, string codeChallenge, Uri redirect, string[] scopes)
        => $"https://example.com/ms/auth?state={state}&cc={codeChallenge}&redirect={Uri.EscapeDataString(redirect.ToString())}&scope={Uri.EscapeDataString(string.Join(' ', scopes))}";
    public Task<TokenSet> ExchangeCodeAsync(string code, string v, Uri r)
        => throw new NotImplementedException("Implement real Microsoft OAuth client");
    public Task<TokenSet> RefreshAsync(string refreshToken)
        => throw new NotImplementedException("Implement real Microsoft refresh");
    public Task RevokeAsync(string refreshToken) 
        => Task.CompletedTask;
}