using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StorageConnector.Infrastructure.Config;
using StorageConnector.Infrastructure.OAuth;
using Xunit;

namespace StorageConnector.Tests.OAuth;

public sealed class MicrosoftOAuthClientTests
{
    [Fact]
    public void Constructor_Throws_WhenClientIdMissing()
    {
        var client = new HttpClient(new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        var options = Options.Create(new MicrosoftOAuthOptions
        {
            ClientId = "",
            ClientSecret = "secret",
            TenantId = "common"
        });

        Assert.Throws<InvalidOperationException>(() => new MicrosoftOAuthClient(client, options));
    }

    [Fact]
    public void BuildAuthorizeUrl_ContainsExpectedParameters()
    {
        var client = CreateClient();
        var url = client.BuildAuthorizeUrl("state-123", "challenge-abc", new Uri("https://api.test/callback"), new[] { "Files.Read", "Sites.Read.All" });

        Assert.Contains("client_id=client-123", url);
        Assert.Contains("scope=Files.Read+Sites.Read.All", url);
        Assert.Contains("code_challenge=challenge-abc", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("response_type=code", url);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ReturnsTokenSet()
    {
        var handler = new StubHandler((request, _) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                access_token = "access-token",
                refresh_token = "refresh-token",
                expires_in = 3600,
                scope = "Files.Read Sites.Read.All"
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
        Assert.Contains("Files.Read", result.Scopes);
        Assert.Contains("Sites.Read.All", result.Scopes);
        Assert.Contains("code_verifier=code-verifier", handler.LastRequestBody);
        Assert.Contains("grant_type=authorization_code", handler.LastRequestBody);
    }

    [Fact]
    public async Task ExchangeCodeAsync_Throws_WhenRefreshMissing()
    {
        var handler = new StubHandler((_, _) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                access_token = "access-token",
                expires_in = 3600,
                scope = "Files.Read"
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ExchangeCodeAsync("auth-code", "verifier", new Uri("https://api.test/callback")));
    }

    [Fact]
    public async Task RefreshAsync_ReusesRefreshTokenWhenMissing()
    {
        var handler = new StubHandler((_, _) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                access_token = "new-access-token",
                expires_in = 1800,
                scope = "Files.Read Sites.Read.All"
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var client = CreateClient(handler);
        var result = await client.RefreshAsync("existing-refresh");

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("existing-refresh", result.RefreshToken);
        Assert.Contains("grant_type=refresh_token", handler.LastRequestBody);
    }

    [Fact]
    public async Task RevokeAsync_SwallowsFailures()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var client = CreateClient(handler);

        await client.RevokeAsync("refresh-token");
        // No exception thrown.
    }

    private static MicrosoftOAuthClient CreateClient(StubHandler? handler = null)
    {
        handler ??= new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new MicrosoftOAuthOptions
        {
            TenantId = "common",
            ClientId = "client-123",
            ClientSecret = "secret-456"
        });

        return new MicrosoftOAuthClient(httpClient, options);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return await _handler(request, cancellationToken);
        }
    }
}
