using System.Security.Cryptography;
using System.Text;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;

namespace LinkingService.Application.Services;

public sealed class LinkProviderService
{
    private readonly IEnumerable<IOAuthClient> _oauth;
    private readonly ITokenStore _tokens;
    private readonly IStateStore _states;

    public LinkProviderService(IEnumerable<IOAuthClient> oauth, ITokenStore tokens, IStateStore states)
    {
        _oauth = oauth; _tokens = tokens; _states = states;
    }

    IOAuthClient C(ProviderType p) => _oauth.First(x => x.Provider == p);

    static string Base64Url(int bytes) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    static string CodeChallenge(string verifier)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public async Task<Uri> StartAsync(Guid userId, ProviderType provider, Uri redirectUri, string[] scopes)
    {
        var state = Base64Url(24);
        var verifier = Base64Url(32);
        var challenge = CodeChallenge(verifier);
        await _states.SaveAsync(state, userId, verifier, provider, TimeSpan.FromMinutes(10));
        return new Uri(C(provider).BuildAuthorizeUrl(state, challenge, redirectUri, scopes));
    }

    public async Task ConnectCallbackAsync(string state, string code, Uri redirectUri)
    {
        var result = await _states.TakeAsync(state);
        if (result is null)
            throw new InvalidOperationException("State expired");
        var (userId, codeVerifier, provider) = result.Value;
        var tokens = await C(provider).ExchangeCodeAsync(code, codeVerifier, redirectUri);
        var acc = await _tokens.GetAsync(userId, provider) ?? new ProviderAccount { UserId = userId, Provider = provider };
        acc.UpdateFrom(tokens, _tokens.Encrypt);
        await _tokens.UpsertAsync(acc);
    }
}
