using System.Net;
using System.Text;
using System.Text.Json;
using IdentityService.Api;
using IdentityService.Infrastructure.Data;
using IdentityService.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace IdentityService.Tests.E2E;

[Trait("Category", "E2E")]
public class UserRegistrationConfirmationLoginFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public UserRegistrationConfirmationLoginFlowTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Register_Confirm_Login_EndToEnd()
  {
    // create a single in-memory sqlite connection that will be used by the test host
    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();

    string? capturedUrl = null;

    var factory = _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureTestServices(services =>
      {
        // replace DB registration with our sqlite connection
        services.AddDbContext<IdentityDbContext>(opts => opts.UseSqlite(connection));

        // capture email send and store the URL
        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string>((to, subj, body) =>
            {
              // attempt to extract href from the body (simple) and store
              var start = body.IndexOf("href=\"");
              if (start >= 0)
              {
                start += 6;
                var end = body.IndexOf('"', start);
                if (end > start)
                {
                  capturedUrl = body[start..end];
                }
              }
              return Task.CompletedTask;
            });

        services.AddSingleton(emailMock.Object);

        // ensure JwtSettings are present for token generation
        services.AddSingleton(Options.Create(new Infrastructure.Config.JwtSettings
        {
          SecretKey = "test-secret-key-which-is-long-enough",
          Issuer = "test-issuer",
          Audience = "test-audience",
          ExpirationMinutes = 60
        }));
      });
    });

    // ensure DB schema created before requests
    using (var scope = factory.Services.CreateScope())
    {
      var ctx = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
      ctx.Database.EnsureCreated();
    }

    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    var email = $"e{Guid.NewGuid().ToString()[..8]}@example.com";
    var password = "Password123!";

    // register
    var registerPayload = new { email = email, password = password };
    var regResp = await client.PostAsync("/api/v1/auth/register", new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json"));
    Assert.Equal(HttpStatusCode.Accepted, regResp.StatusCode);

    // wait briefly for email capture (SendAsync is synchronous here but keep safe)
    await Task.Delay(50);
    Assert.False(string.IsNullOrWhiteSpace(capturedUrl), "Expected confirmation URL to be captured from email body.");

    // parse captured URL query parameters
    var uri = new Uri(capturedUrl!);
    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
    var userId = query["userId"];
    var token = query["token"];

    Assert.False(string.IsNullOrWhiteSpace(userId));
    Assert.False(string.IsNullOrWhiteSpace(token));

    // confirm email
    var confirmResp = await client.GetAsync($"/api/v1/auth/confirm-email?userId={WebUtility.UrlEncode(userId)}&token={WebUtility.UrlEncode(token)}");
    Assert.Equal(HttpStatusCode.Redirect, confirmResp.StatusCode);

    // login
    var loginPayload = new { email = email, password = password };
    var loginResp = await client.PostAsync("/api/v1/auth/login", new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json"));
    Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

    var loginBody = await loginResp.Content.ReadAsStringAsync();
    Assert.Contains("token", loginBody);
  }
}
