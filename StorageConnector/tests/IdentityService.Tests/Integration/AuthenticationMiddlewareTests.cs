using System.Net;
using IdentityService.Api;
using IdentityService.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IdentityService.Tests.Integration;

public class AuthenticationMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public AuthenticationMiddlewareTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Me_ReturnsUnauthorized_WhenNotAuthenticated()
  {
    var client = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
          {
            // Replace external email sender with a no-op mock so startup won't attempt real sends
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType.FullName == "Infrastructure.Email.IEmailSender");
            if (emailDescriptor != null)
              services.Remove(emailDescriptor);

            services.AddSingleton(typeof(IEmailSender), new Mock<IEmailSender>().Object);
          });
    }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var resp = await client.GetAsync("/api/v1/auth/me");

    Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
  }
}
