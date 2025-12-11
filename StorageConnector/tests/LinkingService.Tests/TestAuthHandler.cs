using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkingService.Tests;

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
