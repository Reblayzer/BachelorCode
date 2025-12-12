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

public class EmailServiceFailureHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public EmailServiceFailureHandlingTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Register_ReturnsAccepted_WithConfirmationLink_WhenEmailSenderThrows()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(u => u.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((true, new string[0], Guid.NewGuid()));
        userServiceMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>()))
            .ReturnsAsync("token-1");

        services.AddSingleton(userServiceMock.Object);

        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("smtp error"));
        services.AddSingleton(emailMock.Object);

        services.AddSingleton(new Mock<IJwtService>().Object);
        services.AddSingleton(new Mock<IConfirmationLinkGenerator>().Object);
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email = "x@x.com", password = "Password123!" };
    var resp = await client.PostAsync("/api/v1/auth/register", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);

    using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    Assert.Equal("Registration created, but failed to send confirmation email.", json.RootElement.GetProperty("message").GetString());
    // In development the fallback confirmation link should be returned so the flow can continue.
    Assert.True(json.RootElement.TryGetProperty("confirmationLink", out _));
  }

  [Fact]
  public async Task Resend_ReturnsAccepted_WithConfirmationLink_WhenEmailSenderThrows()
  {
    var userId = Guid.NewGuid();

    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((userId, "u@u.com"));
        userServiceMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>())).ReturnsAsync("token-2");

        services.AddSingleton(userServiceMock.Object);

        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("smtp error"));
        services.AddSingleton(emailMock.Object);
        services.AddSingleton(new Mock<IConfirmationLinkGenerator>().Object);
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var payload = new { email = "u@u.com", password = "Password123!" };
    var resp = await client.PostAsync("/api/v1/auth/resend-confirmation", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    Assert.Equal("Confirmation token generated, but failed to send email.", json.RootElement.GetProperty("message").GetString());
    Assert.True(json.RootElement.TryGetProperty("confirmationLink", out _));
  }
}
