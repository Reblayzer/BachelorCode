using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.Extensions.Options;
using Application.Interfaces;
using Domain;
using Infrastructure.Config;

namespace Infrastructure.OAuth;

public sealed class MicrosoftOAuthClient : IOAuthClient
{
    private const string AuthorityTemplate = "https://login.microsoftonline.com/{0}/oauth2/v2.0";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly MicrosoftOAuthOptions _options;
    private readonly string _authorizeEndpoint;
    private readonly string _tokenEndpoint;
    private readonly string _revokeEndpoint;

    public MicrosoftOAuthClient(HttpClient http, IOptions<MicrosoftOAuthOptions> options)
    {
        _http = http;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Microsoft OAuth client ID is not configured (OAuth:Microsoft:ClientId).");
        }

        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("Microsoft OAuth client secret is not configured (OAuth:Microsoft:ClientSecret).");
        }

        var tenant = string.IsNullOrWhiteSpace(_options.TenantId) ? "common" : _options.TenantId;
        var authority = string.Format(AuthorityTemplate, tenant);

        _authorizeEndpoint = $"{authority}/authorize";
        _tokenEndpoint = $"{authority}/token";
        _revokeEndpoint = $"{authority}/logout";
    }

    public ProviderType Provider => ProviderType.Microsoft;

    public string BuildAuthorizeUrl(string state, string codeChallenge, Uri redirectUri, string[] scopes)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = redirectUri.ToString(),
            ["response_type"] = "code",
            ["response_mode"] = "query",
            ["scope"] = string.Join(' ', scopes),
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        if (!string.IsNullOrWhiteSpace(_options.Prompt))
        {
            query["prompt"] = _options.Prompt!;
        }

        var queryString = string.Join("&",
            query.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

        var builder = new UriBuilder(_authorizeEndpoint)
        {
            Query = queryString
        };

        return builder.Uri.ToString();
    }

    public async Task<TokenSet> ExchangeCodeAsync(string code, string codeVerifier, Uri redirectUri)
    {
        var payload = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri.ToString(),
            ["code_verifier"] = codeVerifier
        };

        var response = await _http.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(payload));
        var json = await ReadJsonAsync<MicrosoftTokenResponse>(response);

        if (string.IsNullOrWhiteSpace(json.RefreshToken))
        {
            throw new InvalidOperationException("Microsoft did not return a refresh token. Ensure offline_access scope is granted.");
        }

        return ToTokenSet(json, json.RefreshToken);
    }

    public async Task<TokenSet> RefreshAsync(string refreshToken)
    {
        var payload = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        var response = await _http.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(payload));
        var json = await ReadJsonAsync<MicrosoftTokenResponse>(response);

        return ToTokenSet(json, json.RefreshToken ?? refreshToken);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        try
        {
            var payload = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["token"] = refreshToken
            };

            var response = await _http.PostAsync(_revokeEndpoint, new FormUrlEncodedContent(payload));
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Microsoft logout endpoint does not always support token revocation; ignore errors.
            try { Console.WriteLine("Microsoft RevokeAsync: revoke request failed or not supported."); } catch { }
        }
    }

    private static TokenSet ToTokenSet(MicrosoftTokenResponse response, string refreshToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(response.ExpiresIn, 0));
        var scopes = (response.Scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new TokenSet(
            response.AccessToken ?? throw new InvalidOperationException("Missing access token from Microsoft."),
            refreshToken,
            expiresAt,
            scopes.Length > 0 ? scopes : Array.Empty<string>());
    }

    private async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            try { Console.WriteLine($"Microsoft token endpoint returned {(int)response.StatusCode}: {content}"); } catch { }
            throw new InvalidOperationException($"Microsoft token endpoint returned {(int)response.StatusCode}: {content}");
        }

        var result = JsonSerializer.Deserialize<T>(content, JsonOptions);
        if (result is null)
        {
            try { Console.WriteLine("Microsoft: failed to parse response JSON."); } catch { }
            throw new InvalidOperationException("Failed to parse Microsoft response.");
        }

        return result;
    }

    private sealed record MicrosoftTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("token_type")] string? TokenType);
}
