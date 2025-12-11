using IdentityService.Infrastructure.Services;
using Xunit;

namespace IdentityService.Tests.Unit;

public class PasswordHasherTests
{
  [Fact]
  public void HashAndVerifyPassword_WorksAsExpected()
  {
    var hasher = new PasswordHasher();

    var password = "My$tr0ngP@ss";
    var hash = hasher.HashPassword(password);

    Assert.False(string.IsNullOrWhiteSpace(hash));
    Assert.True(hasher.VerifyPassword(password, hash));
    Assert.False(hasher.VerifyPassword("wrongpass", hash));
  }
}
