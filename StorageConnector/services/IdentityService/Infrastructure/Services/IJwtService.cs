using IdentityService.Domain;

namespace IdentityService.Infrastructure.Services;

public interface IJwtService
{
  string GenerateToken(User user);
}
