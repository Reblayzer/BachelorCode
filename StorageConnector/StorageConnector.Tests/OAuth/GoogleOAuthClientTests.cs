using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StorageConnector.Infrastructure.Config;
using StorageConnector.Infrastructure.OAuth;
using Xunit;

namespace StorageConnector.Tests.OAuth;

public sealed class GoogleOAuthClientTests
{
    [Fact]
    public void Constructor_Throws_WhenClientIdMissing()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new GoogleOAuthOptions
        {
            ClientId = "",
            ClientSecret = "secret"
        });

        Assert.Throws<InvalidOperationException>(() => new GoogleOAuthClient(httpClient, options));
    }

    [Fact]
    public void BuildAuthorizeUrl_ContainsExpectedParameters()
    {
        var client = CreateClient();

        var url = client.BuildAuthorizeUrl("state-123", "challenge-123", new Uri("https://api.test/callback"), new[] { "scope-a", "scope-b" });

        Assert.Contains("client_id=client-123", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("scope=scope-a+scope-b", url);
        Assert.Contains("state=state-123", url);
        Assert.Contains("code_challenge=challenge-123", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("access_type=offline", url);
        Assert.Contains("prompt=consent", url);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ReturnsTokenSet()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                access_token = "access-token",
                refresh_token = "refresh-token",
                expires_in = 3600,
                scope = "scope-a scope-b",
                token_type = "Bearer"
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var client = CreateClient(handler);

        var result = await client.ExchangeCodeAsync("auth-code", "code-verifier", new Uri("https://api.test/callback"));

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Contains("scope-a", result.Scopes);
        Assert.Contains("scope-b", result.Scopes);
        Assert.True((result.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds > 3500);
        Assert.Contains("code_verifier=code-verifier", handler.LastRequestBody);
        Assert.Contains("grant_type=authorization_code", handler.LastRequestBody);
    }

    [Fact]
    public async Task ExchangeCodeAsync_Throws_WhenRefreshTokenMissing()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                access_token = "access-token",
                expires_in = 3600,
                scope = "scope"
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ExchangeCodeAsync("auth-code", "code-verifier", new Uri("https://api.test/callback")));
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNewExpiry_AndKeepsRefreshTokenWhenMissing()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                access_token = "new-access-token",
                expires_in = 1800,
                scope = "scope"
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var client = CreateClient(handler);

        var result = await client.RefreshAsync("existing-refresh-token");

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("existing-refresh-token", result.RefreshToken);
        Assert.True((result.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds > 1700);
        Assert.Contains("grant_type=refresh_token", handler.LastRequestBody);
    }

    [Fact]
    public async Task RevokeAsync_SwallowsErrors()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        var client = CreateClient(handler);

        await client.RevokeAsync("refresh-token");
        // No exception should be thrown even when Google responds with 400.
    }

    private static GoogleOAuthClient CreateClient(StubHttpMessageHandler? handler = null)
    {
        handler ??= new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new GoogleOAuthOptions
        {
            ClientId = "client-123",
            ClientSecret = "secret-456",
            AccessType = "offline",
            Prompt = "consent"
        });

        return new GoogleOAuthClient(httpClient, options);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync();
            }

            return await _handler(request, cancellationToken);
        }
    }
}
