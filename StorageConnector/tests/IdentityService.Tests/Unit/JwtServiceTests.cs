using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityService.Domain;
using IdentityService.Infrastructure.Config;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace IdentityService.Tests.Unit;

public class JwtServiceTests
{
  [Fact]
  public void GenerateToken_IncludesExpectedClaimsAndExpiry()
  {
    var settings = Options.Create(new JwtSettings
    {
      SecretKey = "test-secret-key-which-is-long-enough",
      Issuer = "test-issuer",
      Audience = "test-audience",
      ExpirationMinutes = 60
    });

    var svc = new JwtService(settings);

    var user = new User { Id = Guid.NewGuid(), Email = "u@example.com", EmailConfirmed = true };

    var token = svc.GenerateToken(user);

    Assert.False(string.IsNullOrWhiteSpace(token));

    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);

    Assert.Equal("test-issuer", jwt.Issuer);
    Assert.Equal("test-audience", jwt.Audiences.First());

    var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
    var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
    var confirmed = jwt.Claims.FirstOrDefault(c => c.Type == "email_confirmed");

    Assert.NotNull(idClaim);
    Assert.Equal(user.Id.ToString(), idClaim!.Value);
    Assert.NotNull(emailClaim);
    Assert.Equal(user.Email, emailClaim!.Value);
    Assert.NotNull(confirmed);
    Assert.Equal(user.EmailConfirmed.ToString(), confirmed!.Value);

    var expires = jwt.ValidTo;
    Assert.True((expires - DateTime.UtcNow).TotalMinutes <= 61);
  }

  [Fact]
  public void GenerateToken_Throws_WhenSecretMissing()
  {
    var settings = Options.Create(new JwtSettings
    {
      SecretKey = null!,
      Issuer = "i",
      Audience = "a",
      ExpirationMinutes = 60
    });

    var svc = new JwtService(settings);
    var user = new User { Id = Guid.NewGuid(), Email = "u@example.com", EmailConfirmed = true };

    Assert.Throws<ArgumentNullException>(() => svc.GenerateToken(user));
  }
}
