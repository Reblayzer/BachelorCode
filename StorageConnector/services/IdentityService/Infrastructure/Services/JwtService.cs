using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Infrastructure.Config;
using IdentityService.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Infrastructure.Services;

public sealed class JwtService : IJwtService
{
  private readonly JwtSettings _settings;

  public JwtService(IOptions<JwtSettings> settings)
  {
    _settings = settings.Value;
  }

  public string GenerateToken(User user)
  {
    var claims = new[]
    {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("email_confirmed", user.EmailConfirmed.ToString())
        };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _settings.Issuer,
        audience: _settings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
