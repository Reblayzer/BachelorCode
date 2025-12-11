using System.Security.Cryptography;
using LinkingService.Infrastructure.Stores;
using LinkingService.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinkingService.Tests.Unit;

public class EfTokenStoreTests
{
  private static LinkingDbContext CreateInMemoryContext()
  {
    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    var options = new DbContextOptionsBuilder<LinkingDbContext>()
      .UseSqlite(connection)
      .Options;

    var ctx = new LinkingDbContext(options);
    ctx.Database.EnsureCreated();
    return ctx;
  }

  [Fact]
  public void EncryptDecrypt_Roundtrip_Works()
  {
    using var ctx = CreateInMemoryContext();
    var dp = DataProtectionProvider.Create("tests");
    var store = new EfTokenStore(ctx, dp);

    var plaintext = "refresh-token-123";
    var cipher = store.Encrypt(plaintext);

    Assert.False(string.IsNullOrWhiteSpace(cipher));

    var round = store.Decrypt(cipher);
    Assert.Equal(plaintext, round);
  }

  [Fact]
  public void Decrypt_Throws_OnCorruptedCiphertext()
  {
    using var ctx = CreateInMemoryContext();
    var dp = DataProtectionProvider.Create("tests");
    var store = new EfTokenStore(ctx, dp);

    // a random string that is not a valid protected payload
    Assert.Throws<CryptographicException>(() => store.Decrypt("not-a-valid-protected-string"));
  }
}
