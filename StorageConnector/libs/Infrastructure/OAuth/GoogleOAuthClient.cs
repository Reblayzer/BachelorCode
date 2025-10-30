using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Options;
using Application.Interfaces;
using Domain;
using Infrastructure.Config;
using System.Text.Json.Serialization;

namespace Infrastructure.OAuth;

public sealed class GoogleOAuthClient : IOAuthClient
{
    private const string AuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly GoogleOAuthOptions _options;

    public GoogleOAuthClient(HttpClient http, IOptions<GoogleOAuthOptions> options)
    {
        _http = http;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Google OAuth client ID is not configured (OAuth:Google:ClientId).");
        }

        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("Google OAuth client secret is not configured (OAuth:Google:ClientSecret).");
        }
    }

    public ProviderType Provider => ProviderType.Google;

    public string BuildAuthorizeUrl(string state, string codeChallenge, Uri redirectUri, string[] scopes)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = redirectUri.ToString(),
            ["response_type"] = "code",
            ["scope"] = string.Join(' ', scopes),
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["access_type"] = _options.AccessType,
        };

        if (!string.IsNullOrWhiteSpace(_options.Prompt))
        {
            query["prompt"] = _options.Prompt;
        }

        var queryString = string.Join("&",
            query.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

        var builder = new UriBuilder(AuthorizeEndpoint)
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

        var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(payload));
        var json = await ReadJsonAsync<GoogleTokenResponse>(response);

        if (string.IsNullOrWhiteSpace(json.RefreshToken))
        {
            throw new InvalidOperationException("Google did not return a refresh token. Ensure prompt=consent and access_type=offline are configured.");
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

        var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(payload));
        var json = await ReadJsonAsync<GoogleTokenResponse>(response);

        return ToTokenSet(json, json.RefreshToken ?? refreshToken);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        try
        {
            var payload = new Dictionary<string, string> { ["token"] = refreshToken };
            var response = await _http.PostAsync(RevokeEndpoint, new FormUrlEncodedContent(payload));
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Google returns various status codes if the token is already revoked; ignore failures.
            try { Console.WriteLine("Google RevokeAsync: revoke request failed or token already revoked."); } catch { }
        }
    }

    private static TokenSet ToTokenSet(GoogleTokenResponse response, string refreshToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(response.ExpiresIn, 0));
        var scopes = (response.Scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new TokenSet(
            response.AccessToken ?? throw new InvalidOperationException("Missing access token from Google."),
            refreshToken,
            expiresAt,
            scopes.Length > 0 ? scopes : Array.Empty<string>());
    }

    private async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            try { Console.WriteLine($"Google token endpoint returned {(int)response.StatusCode}: {content}"); } catch { }
            throw new InvalidOperationException($"Google token endpoint returned {(int)response.StatusCode}: {content}");
        }

        var result = JsonSerializer.Deserialize<T>(content, JsonOptions);
        if (result is null)
        {
            try { Console.WriteLine("Google: failed to parse response JSON."); } catch { }
            throw new InvalidOperationException("Failed to parse Google response.");
        }

        return result;
    }

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("id_token")] string? IdToken);
}
