using StorageConnector.Domain;

namespace StorageConnector.Application.Interfaces;

public interface IOAuthClient {
    ProviderType Provider { get; }
    string BuildAuthorizeUrl(string state, string codeChallenge, Uri redirectUri, string[] scopes);
    Task<TokenSet> ExchangeCodeAsync(string code, string codeVerifier, Uri redirectUri);
    Task<TokenSet> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
}