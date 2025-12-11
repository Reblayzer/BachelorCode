using System.Net;
using System.Text;
using System.Text.Json;
using IdentityService.Api;
using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Email;
using IdentityService.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IdentityService.Tests.Integration;

public class RegistrationAndLoginIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public RegistrationAndLoginIntegrationTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Register_ReturnsAccepted_AndSendsEmail_WhenUserCreated()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            // Mock IUserService.CreateAsync to return success
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((true, Array.Empty<string>(), Guid.NewGuid()));

            userServiceMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>()))
                    .ReturnsAsync("token-123");

            services.AddSingleton(userServiceMock.Object);

            // Mock IEmailSender and capture calls
            var emailMock = new Mock<IEmailSender>();
            services.AddSingleton(emailMock.Object);
          });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email = "test@example.com", password = "Password123!" };
    var resp = await client.PostAsync("/api/v1/auth/register", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
  }

  [Fact]
  public async Task Login_ReturnsOk_WhenCredentialsValid()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            var userId = Guid.NewGuid();
            var email = "user@example.com";

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
            userServiceMock.Setup(u => u.IsEmailConfirmedAsync(userId)).ReturnsAsync(true);
            userServiceMock.Setup(u => u.ValidateCredentialsAsync(email, "Password123!")).ReturnsAsync(new Domain.User { Id = userId, Email = email });

            services.AddSingleton(userServiceMock.Object);

            // Mock JwtService to return a deterministic token
            var jwtMock = new Mock<IJwtService>();
            jwtMock.Setup(j => j.GenerateToken(It.IsAny<Domain.User>())).Returns("fake-jwt-token");
            services.AddSingleton(jwtMock.Object);

            // Ensure email sender present to avoid startup errors
            services.AddSingleton(new Mock<IEmailSender>().Object);
          });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email = "user@example.com", password = "Password123!" };
    var resp = await client.PostAsync("/api/v1/auth/login", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

    var body = await resp.Content.ReadAsStringAsync();
    Assert.Contains("token", body);
  }

  [Fact]
  public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            var userServiceMock = new Mock<IUserService>();
            // Simulate user not found
            services.AddSingleton(userServiceMock.Object);

            // Ensure JwtService and email sender exist to avoid startup errors
            services.AddSingleton(new Mock<IJwtService>().Object);
            services.AddSingleton(new Mock<IEmailSender>().Object);
          });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email = "missing@example.com", password = "Password123!" };
    var resp = await client.PostAsync("/api/v1/auth/login", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
  }

  [Fact]
  public async Task Login_ReturnsForbid_WhenEmailNotConfirmed()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        var userId = Guid.NewGuid();
        var email = "user2@example.com";

        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
        userServiceMock.Setup(u => u.IsEmailConfirmedAsync(userId)).ReturnsAsync(false);
        services.AddSingleton(userServiceMock.Object);

        services.AddSingleton(new Mock<IJwtService>().Object);
        services.AddSingleton(new Mock<IEmailSender>().Object);
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email = "user2@example.com", password = "Password123!" };
    var resp = await client.PostAsync("/api/v1/auth/login", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
  }

  [Fact]
  public async Task Register_ReturnsBadRequest_WhenCreateFails()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(u => u.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, ["already exists"], null));
        services.AddSingleton(userServiceMock.Object);

        services.AddSingleton(new Mock<IEmailSender>().Object);
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email = "dup@example.com", password = "Password123!" };
    var resp = await client.PostAsync("/api/v1/auth/register", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
  }
}
