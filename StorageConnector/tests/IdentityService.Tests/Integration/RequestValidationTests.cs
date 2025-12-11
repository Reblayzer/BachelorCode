using System.Net;
using System.Text;
using System.Text.Json;
using IdentityService.Api;
using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IdentityService.Tests.Integration;

public class RequestValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public RequestValidationTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Register_ReturnsBadRequest_WhenPasswordMissing()
  {
    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        // ensure email sender exists to avoid startup errors
        services.AddSingleton(new Mock<IEmailSender>().Object);
        services.AddSingleton(new Mock<IConfirmationLinkGenerator>().Object);
      });
    });

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // missing password field
    var payload = new { email = "no-pass@example.com" };
    var resp = await client.PostAsync("/api/v1/auth/register", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
  }
}
