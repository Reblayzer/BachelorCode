using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using IdentityService.Api;
using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace IdentityService.Tests.Integration;

public class EmailConfirmationAndPasswordManagementTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public EmailConfirmationAndPasswordManagementTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task ConfirmEmail_Redirects_WhenTokenValid()
  {
    var userId = Guid.NewGuid();
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            // Mock IUserService.FindByIdAsync and ConfirmEmailAsync
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync((userId, "user@example.com"));
            userServiceMock.Setup(u => u.ConfirmEmailAsync(userId, "token-xyz")).ReturnsAsync(true);
            services.AddSingleton(userServiceMock.Object);

            // Ensure email sender exists
            services.AddSingleton(new Mock<IEmailSender>().Object);
          });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var resp = await client.GetAsync($"/api/v1/auth/confirm-email?userId={userId}&token=token-xyz");

    Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
    Assert.Equal("http://localhost:5173/auth/confirmed", resp.Headers.Location?.ToString());
  }

  [Fact]
  public async Task ResendConfirmation_ReturnsAccepted_WhenUserExists()
  {
    var userId = Guid.NewGuid();
    var email = "resend@example.com";

    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
            userServiceMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(userId)).ReturnsAsync("token-abc");
            services.AddSingleton(userServiceMock.Object);

            var emailMock = new Mock<IEmailSender>();
            services.AddSingleton(emailMock.Object);
          });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email, password = "irrelevant" };
    var resp = await client.PostAsync("/api/v1/auth/resend-confirmation", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
  }


  [Fact]
  public async Task ConfirmEmail_ReturnsBadRequest_WhenTokenInvalid()
  {
    var userId = Guid.NewGuid();
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync((userId, "user@example.com"));
        userServiceMock.Setup(u => u.ConfirmEmailAsync(userId, "bad-token")).ReturnsAsync(false);
        services.AddSingleton(userServiceMock.Object);

        services.AddSingleton(new Mock<IEmailSender>().Object);
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var resp = await client.GetAsync($"/api/v1/auth/confirm-email?userId={userId}&token=bad-token");

    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
  }

  [Fact]
  public async Task ResendConfirmation_ReturnsOk_WhenUserMissing()
  {
    var email = "missing-resend@example.com";

    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((null, null));
        services.AddSingleton(userServiceMock.Object);

        services.AddSingleton(new Mock<IEmailSender>().Object);
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email, password = "irrelevant" };
    var resp = await client.PostAsync("/api/v1/auth/resend-confirmation", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
  }

  [Fact]
  public async Task ForgotPassword_ReturnsOk_WhenEmailExists()
  {
    var userId = Guid.NewGuid();
    var email = "forgot@example.com";

    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
            userServiceMock.Setup(u => u.GeneratePasswordResetTokenAsync(userId)).ReturnsAsync("reset-123");
            services.AddSingleton(userServiceMock.Object);

            services.AddSingleton(new Mock<IEmailSender>().Object);
          });
    });

    var client = factory.CreateClient();

    var payload = new { email };
    var resp = await client.PostAsync("/api/v1/auth/forgot-password", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
  }

  [Fact]
  public async Task ResetPassword_ReturnsOk_WhenTokenValid()
  {
    var userId = Guid.NewGuid();
    var email = "reset@example.com";

    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
            userServiceMock.Setup(u => u.ResetPasswordAsync(userId, "token-1", "NewPass1!")).ReturnsAsync(true);
            services.AddSingleton(userServiceMock.Object);

            services.AddSingleton(new Mock<IEmailSender>().Object);
          });
    });

    var client = factory.CreateClient();

    var payload = new { email, token = "token-1", newPassword = "NewPass1!" };
    var resp = await client.PostAsync("/api/v1/auth/reset-password", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
  }

  // For authenticated endpoints we'll register a test auth handler that sets the NameIdentifier claim
  private WebApplicationFactory<Program> WithTestAuth(Action<IServiceCollection>? configure = null)
  {
    return _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            // Replace authentication with a test scheme and set it as default
            services.AddAuthentication(options =>
                {
                  options.DefaultAuthenticateScheme = "Test";
                  options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            configure?.Invoke(services);
          });
    });
  }

  [Fact]
  public async Task ChangePassword_ReturnsOk_WhenAuthenticated_AndCurrentPasswordValid()
  {
    var userId = Guid.NewGuid();

    var factory = WithTestAuth(services =>
    {
      var userServiceMock = new Mock<IUserService>();
      userServiceMock.Setup(u => u.ChangePasswordAsync(userId, "OldPass1!", "NewPass2!")).ReturnsAsync(true);
      userServiceMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync((userId, "user@ex.com"));
      services.AddSingleton(userServiceMock.Object);

      services.AddSingleton(new Mock<IEmailSender>().Object);
    });

    var client = factory.CreateClient();

    // Add header the TestAuthHandler looks for
    client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

    var payload = new { currentPassword = "OldPass1!", newPassword = "NewPass2!" };
    var resp = await client.PostAsync("/api/v1/auth/change-password", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
  }

  [Fact]
  public async Task ChangePassword_ReturnsBadRequest_WhenCurrentPasswordInvalid()
  {
    var userId = Guid.NewGuid();

    var factory = WithTestAuth(services =>
    {
      var userServiceMock = new Mock<IUserService>();
      userServiceMock.Setup(u => u.ChangePasswordAsync(userId, "OldPass1!", "NewPass2!")).ReturnsAsync(false);
      userServiceMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync((userId, "user@ex.com"));
      services.AddSingleton(userServiceMock.Object);

      services.AddSingleton(new Mock<IEmailSender>().Object);
    });

    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

    var payload = new { currentPassword = "OldPass1!", newPassword = "NewPass2!" };
    var resp = await client.PostAsync("/api/v1/auth/change-password", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
  }

  [Fact]
  public async Task Logout_ReturnsUnauthorized_WhenNotAuthenticated()
  {
    var client = _factory.CreateClient();

    var resp = await client.PostAsync("/api/v1/auth/logout", null);

    Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
  }

  [Fact]
  public async Task Logout_ReturnsNoContent_WhenAuthenticated()
  {
    var userId = Guid.NewGuid();

    var factory = WithTestAuth(services => { services.AddSingleton(new Mock<IEmailSender>().Object); });
    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

    var resp = await client.PostAsync("/api/v1/auth/logout", null);
    Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
  }

  [Fact]
  public async Task Me_ReturnsOk_WhenAuthenticated()
  {
    var userId = Guid.NewGuid();
    var email = "me@example.com";

    var factory = WithTestAuth(services =>
    {
      var userServiceMock = new Mock<IUserService>();
      userServiceMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync((userId, email));
      services.AddSingleton(userServiceMock.Object);
      services.AddSingleton(new Mock<IEmailSender>().Object);
    });

    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

    var resp = await client.GetAsync("/api/v1/auth/me");
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

    var body = await resp.Content.ReadAsStringAsync();
    Assert.Contains(email, body);
  }
}

// Test authentication handler: looks for "X-Test-UserId" header and considers request authenticated
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public const string UserIdHeader = "X-Test-UserId";

  public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.TryGetValue(UserIdHeader, out var values))
    {
      return Task.FromResult(AuthenticateResult.Fail("No test user header"));
    }

    var userIdString = values.FirstOrDefault();
    if (!Guid.TryParse(userIdString, out var userId))
      return Task.FromResult(AuthenticateResult.Fail("Invalid user id"));

    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Name, "test-user") };
    var identity = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, "Test");
    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
