using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Application;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using LinkingProgram = LinkingService.TestHost.Program;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Tests.Controllers;

public sealed class ConnectControllerIntegrationTests : IClassFixture<WebApplicationFactory<LinkingProgram>>
{
  private readonly WebApplicationFactory<LinkingProgram> _factory;

  public ConnectControllerIntegrationTests(WebApplicationFactory<LinkingProgram> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task StartEndpoint_ReturnsRedirectUrl_And_StateSaved()
  {
    var fakeOauth = new RecordingOAuthClient(ProviderType.Google);

    var customFactory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
          {
            // Remove existing registrations so our test doubles take precedence
            services.RemoveAll<IOAuthClient>();
            services.RemoveAll<ITokenStore>();
            services.RemoveAll<IStateStore>();

            // Replace IOAuthClient registrations with our recording client
            services.AddSingleton<IOAuthClient>(fakeOauth);

            // Use in-memory state store
            services.AddSingleton<IStateStore>(_ => new Infrastructure.CacheStateStore(new MemoryCache(new MemoryCacheOptions())));

            // Use a simple in-memory token store
            services.AddSingleton<ITokenStore, InMemoryTokenStore>();
          });

      // Register an IStartupFilter to inject a test user early in the pipeline (before authentication/authorization)
      builder.ConfigureServices(services =>
          {
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter>(new TestUserStartupFilter());
          });
    });

    var client = customFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // Act
    var resp = await client.GetAsync("/api/connect/google/start");
    resp.EnsureSuccessStatusCode();

    var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    var redirectUrl = doc.RootElement.GetProperty("redirectUrl").GetString();

    Assert.Equal(fakeOauth.AuthorizeResponse, redirectUrl);
    Assert.False(string.IsNullOrWhiteSpace(fakeOauth.LastState));

    // Verify state is actually stored by reading from the host services
    var stateStore = customFactory.Services.GetService<IStateStore>();
    Assert.NotNull(stateStore);
    var taken = await stateStore!.TakeAsync(fakeOauth.LastState!);
    Assert.NotNull(taken);
  }

  [Fact]
  public async Task CallbackEndpoint_ExchangesCode_And_StoresTokens()
  {
    var fakeOauth = new RecordingOAuthClient(ProviderType.Google)
    {
      ExchangeResult = new TokenSet("access-x", "refresh-x", DateTimeOffset.UtcNow.AddHours(1), new[] { "scope-a" })
    };

    var tokenStore = new InMemoryTokenStore();
    var stateStore = new Infrastructure.CacheStateStore(new MemoryCache(new MemoryCacheOptions()));

    var customFactory2 = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
          {
            services.RemoveAll<IOAuthClient>();
            services.RemoveAll<ITokenStore>();
            services.RemoveAll<IStateStore>();

            services.AddSingleton<IOAuthClient>(fakeOauth);
            services.AddSingleton<ITokenStore>(tokenStore);
            services.AddSingleton<IStateStore>(stateStore);
          });

      builder.ConfigureServices(services =>
          {
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter>(new TestUserStartupFilter());
          });
    });

    var client = customFactory2.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // Prepare state in store (simulating Start flow)
    await stateStore.SaveAsync("s-expected", "verifier-1", ProviderType.Google, TimeSpan.FromMinutes(5));

    // Act: call callback
    var resp = await client.GetAsync($"/api/connect/google/callback?state=s-expected&code=code-123");

    // Controller redirects to frontend; ensure 302
    Assert.True((int)resp.StatusCode == 302 || (int)resp.StatusCode == 301);

    // Verify token stored
    var stored = tokenStore.GetStored("test-user", ProviderType.Google);
    Assert.NotNull(stored);
    Assert.Equal("scope-a", stored!.ScopeCsv);
  }

  // Test helpers
  private sealed class RecordingOAuthClient : Application.Interfaces.IOAuthClient
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

    public TokenSet ExchangeResult { get; set; } = new("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(30), new[] { "scope" });

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



  private sealed class InMemoryTokenStore : ITokenStore
  {
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(string, ProviderType), ProviderAccount> _store = new();

    public Task<ProviderAccount?> GetAsync(string userId, ProviderType provider)
    {
      _store.TryGetValue((userId, provider), out var a);
      return Task.FromResult(a);
    }

    public Task<IReadOnlyList<ProviderAccount>> GetAllByUserAsync(string userId)
    {
      var accounts = _store
        .Where(kv => kv.Key.Item1 == userId)
        .Select(kv => kv.Value)
        .ToList();
      return Task.FromResult<IReadOnlyList<ProviderAccount>>(accounts);
    }

    public Task UpsertAsync(ProviderAccount account)
    {
      _store[(account.UserId, account.Provider)] = account;
      return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId, ProviderType provider)
    {
      _store.TryRemove((userId, provider), out _);
      return Task.CompletedTask;
    }

    public string Encrypt(string plaintext) => plaintext;
    public string Decrypt(string ciphertext) => ciphertext;

    public ProviderAccount? GetStored(string userId, ProviderType provider) => _store.TryGetValue((userId, provider), out var a) ? a : null;
  }

  private sealed class TestUserStartupFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
  {
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
      return app =>
      {
        app.Use(async (ctx, n) =>
              {
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user") }, "Test"));
                await n();
              });

        next(app);
      };
    }
  }
}
