using System.Security.Claims;
using System.Text.Json;
using LinkingService.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using LinkingProgram = LinkingService.TestHost.Program;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LinkingService.Application.Interfaces;
using LinkingService.Infrastructure.Stores;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Concurrent;

namespace LinkingService.Tests.Integration;

public sealed class OAuthFlowIntegrationTests : IClassFixture<WebApplicationFactory<LinkingProgram>>
{
  private readonly WebApplicationFactory<LinkingProgram> _factory;
  private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

  public OAuthFlowIntegrationTests(WebApplicationFactory<LinkingProgram> factory)
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
            services.AddSingleton<IStateStore>(_ => new CacheStateStore(new MemoryCache(new MemoryCacheOptions())));

            // Use a simple in-memory token store
            services.AddSingleton<ITokenStore, InMemoryTokenStore>();
          });

      // Register an IStartupFilter to inject a test user early in the pipeline (before authentication/authorization)
      builder.ConfigureServices(services =>
          {
            services.AddSingleton<IStartupFilter>(new TestUserStartupFilter());
          });
    });

    var client = customFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // Act
    var resp = await client.GetAsync("/api/v1/connect/google/start");
    resp.EnsureSuccessStatusCode();

    var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    var redirectUrl = doc.RootElement.GetProperty("redirectUrl").GetString();

    Assert.Equal(fakeOauth.AuthorizeResponse, redirectUrl);
    Assert.False(string.IsNullOrWhiteSpace(fakeOauth.LastState));

    // Verify state is actually stored by reading from the host services
    var stateStore = customFactory.Services.GetService<IStateStore>();
    Assert.NotNull(stateStore);
    var taken = await stateStore.TakeAsync(fakeOauth.LastState!);
    Assert.NotNull(taken);
  }

  [Fact]
  public async Task CallbackEndpoint_ExchangesCode_And_StoresTokens()
  {
    var fakeOauth = new RecordingOAuthClient(ProviderType.Google)
    {
      ExchangeResult = new TokenSet("access-x", "refresh-x", DateTimeOffset.UtcNow.AddHours(1), ["scope-a"])
    };

    var tokenStore = new InMemoryTokenStore();
    var stateStore = new CacheStateStore(new MemoryCache(new MemoryCacheOptions()));

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
            services.AddSingleton<IStartupFilter>(new TestUserStartupFilter());
          });
    });

    var client = customFactory2.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // Prepare state in store (simulating Start flow)
    await stateStore.SaveAsync("s-expected", TestUserId, "verifier-1", ProviderType.Google, TimeSpan.FromMinutes(5));

    // Act: call callback
    var resp = await client.GetAsync($"/api/v1/connect/google/callback?state=s-expected&code=code-123");

    // Controller redirects to frontend; ensure 302
    Assert.True((int)resp.StatusCode == 302 || (int)resp.StatusCode == 301);

    // Verify token stored
    var stored = tokenStore.GetStored(TestUserId, ProviderType.Google);
    Assert.NotNull(stored);
    Assert.Equal("scope-a", stored.ScopeCsv);
  }

  // Test helpers
  private sealed class RecordingOAuthClient : IOAuthClient
  {
    public RecordingOAuthClient(ProviderType provider)
    {
      Provider = provider;
    }

    public ProviderType Provider { get; }
    public string AuthorizeResponse { get; init; } = "https://accounts.test/authorize";
    public string? LastState { get; private set; }

    public TokenSet ExchangeResult { get; set; } = new("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(30), ["scope"]);

    public string BuildAuthorizeUrl(string state, string codeChallenge, Uri redirectUri, string[] scopes)
    {
      LastState = state;
      return AuthorizeResponse;
    }

    public Task<TokenSet> ExchangeCodeAsync(string code, string codeVerifier, Uri redirectUri)
    {
      return Task.FromResult(ExchangeResult);
    }

    public Task<TokenSet> RefreshAsync(string refreshToken) => Task.FromResult(ExchangeResult);
    public Task RevokeAsync(string refreshToken) => Task.CompletedTask;
  }



  private sealed class InMemoryTokenStore : ITokenStore
  {
    private readonly ConcurrentDictionary<(Guid, ProviderType), ProviderAccount> _store = new();

    public Task<ProviderAccount?> GetAsync(Guid userId, ProviderType provider)
    {
      _store.TryGetValue((userId, provider), out var a);
      return Task.FromResult(a);
    }

    public Task<IReadOnlyList<ProviderAccount>> GetAllByUserAsync(Guid userId)
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

    public Task DeleteAsync(Guid userId, ProviderType provider)
    {
      _store.TryRemove((userId, provider), out _);
      return Task.CompletedTask;
    }

    public string Encrypt(string plaintext) => plaintext;
    public string Decrypt(string ciphertext) => ciphertext;

    public ProviderAccount? GetStored(Guid userId, ProviderType provider) => _store.TryGetValue((userId, provider), out var a) ? a : null;
  }

  private sealed class TestUserStartupFilter : IStartupFilter
  {
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
      return app =>
      {
        app.Use(async (ctx, n) =>
              {
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()) }, "Test"));
                await n();
              });

        next(app);
      };
    }
  }
}
