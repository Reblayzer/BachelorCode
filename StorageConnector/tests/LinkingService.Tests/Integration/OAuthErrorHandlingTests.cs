using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using LinkingService.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using LinkingService.TestHost;

namespace LinkingService.Tests.Integration;

public class OAuthErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public OAuthErrorHandlingTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Callback_ReturnsBadRequest_WhenStateMissing()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        // Ensure test user present in pipeline
        services.AddSingleton<IStartupFilter>(new TestUserStartupFilter());
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var resp = await client.GetAsync($"/api/v1/connect/google/callback?state=missing&code=code-1");

    // State missing -> middleware maps "State expired" to 410 Gone
    Assert.Equal(HttpStatusCode.Gone, resp.StatusCode);
  }

  [Fact]
  public async Task Callback_ReturnsServerError_WhenExchangeThrows()
  {
    var exchangeFail = new Mock<IOAuthClient>();
    string? capturedState = null;
    exchangeFail.Setup(o => o.BuildAuthorizeUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<string[]>()))
      .Callback<string, string, Uri, string[]>((s, cc, uri, scopes) => capturedState = s)
      .Returns("https://accounts.test/authorize");

    exchangeFail.Setup(o => o.ExchangeCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>()))
      .ThrowsAsync(new Exception("upstream error"));

    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IOAuthClient>();
        services.AddSingleton(exchangeFail.Object);
        services.AddSingleton<IStartupFilter>(new TestUserStartupFilter());
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // Call start to produce a state (we captured it via BuildAuthorizeUrl callback)
    var startResp = await client.GetAsync("/api/v1/connect/google/start");
    startResp.EnsureSuccessStatusCode();

    // Use the captured state when calling callback so ExchangeCodeAsync is invoked and throws
    Assert.False(string.IsNullOrWhiteSpace(capturedState));
    var resp = await client.GetAsync($"/api/v1/connect/google/callback?state={capturedState}&code=code-1");

    Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
  }

  // reuse TestUserStartupFilter from other tests
  private sealed class TestUserStartupFilter : IStartupFilter
  {
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
      return app =>
      {
        app.Use(async (ctx, n) =>
        {
          ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000002")], "Test"));
          await n();
        });

        next(app);
      };
    }
  }
}
