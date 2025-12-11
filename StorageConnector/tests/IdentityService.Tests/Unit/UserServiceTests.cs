using IdentityService.Infrastructure.Data;
using IdentityService.Infrastructure.Services;
using IdentityService.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace IdentityService.Tests.Unit;

public class UserServiceTests
{
  private static IdentityDbContext CreateInMemoryContext()
  {
    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    var options = new DbContextOptionsBuilder<IdentityDbContext>()
      .UseSqlite(connection)
      .Options;

    var ctx = new IdentityDbContext(options);
    ctx.Database.EnsureCreated();
    return ctx;
  }

  [Fact]
  public async Task CreateAsync_Succeeds_WithValidData()
  {
    using var ctx = CreateInMemoryContext();

    var hasher = new Mock<IPasswordHasher>();
    hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");

    var svc = new UserService(ctx, hasher.Object);

    var (succeeded, errors, userId) = await svc.CreateAsync("a@b.com", "Password123!");

    Assert.True(succeeded);
    Assert.Empty(errors);
    Assert.NotNull(userId);

    var user = await ctx.Users.FindAsync(userId.Value);
    Assert.NotNull(user);
    Assert.Equal("a@b.com", user!.Email);
    Assert.Equal("hashed", user.PasswordHash);
  }

  [Fact]
  public async Task CreateAsync_Fails_WhenEmailDuplicate()
  {
    using var ctx = CreateInMemoryContext();

    // seed existing user
    ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "dup@x.com", PasswordHash = "x", EmailConfirmed = false, CreatedAt = DateTime.UtcNow });
    await ctx.SaveChangesAsync();

    var hasher = new Mock<IPasswordHasher>();
    hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("h");

    var svc = new UserService(ctx, hasher.Object);

    var (succeeded, errors, userId) = await svc.CreateAsync("dup@x.com", "Password123!");

    Assert.False(succeeded);
    Assert.Contains("Email is already registered.", errors);
    Assert.Null(userId);
  }

  [Fact]
  public async Task ValidateCredentials_ReturnsUser_WhenPasswordMatches()
  {
    using var ctx = CreateInMemoryContext();

    var userId = Guid.NewGuid();
    ctx.Users.Add(new User { Id = userId, Email = "valid@u.com", PasswordHash = "stored-hash", EmailConfirmed = true, CreatedAt = DateTime.UtcNow });
    await ctx.SaveChangesAsync();

    var hasher = new Mock<IPasswordHasher>();
    hasher.Setup(h => h.VerifyPassword("Password123!", "stored-hash")).Returns(true);

    var svc = new UserService(ctx, hasher.Object);

    var user = await svc.ValidateCredentialsAsync("valid@u.com", "Password123!");

    Assert.NotNull(user);
    Assert.Equal(userId, user!.Id);
  }

  [Fact]
  public async Task ChangePassword_ReturnsFalse_WhenCurrentPasswordInvalid()
  {
    using var ctx = CreateInMemoryContext();

    var userId = Guid.NewGuid();
    ctx.Users.Add(new User { Id = userId, Email = "u2@u.com", PasswordHash = "stored-hash", EmailConfirmed = true, CreatedAt = DateTime.UtcNow });
    await ctx.SaveChangesAsync();

    var hasher = new Mock<IPasswordHasher>();
    hasher.Setup(h => h.VerifyPassword("wrong", "stored-hash")).Returns(false);

    var svc = new UserService(ctx, hasher.Object);

    var ok = await svc.ChangePasswordAsync(userId, "wrong", "NewPassword1!");

    Assert.False(ok);
  }

  [Fact]
  public async Task GenerateEmailConfirmationToken_PersistsToken()
  {
    using var ctx = CreateInMemoryContext();

    var userId = Guid.NewGuid();
    ctx.Users.Add(new User { Id = userId, Email = "t@t.com", PasswordHash = "h", EmailConfirmed = false, CreatedAt = DateTime.UtcNow });
    await ctx.SaveChangesAsync();

    var hasher = new Mock<IPasswordHasher>();
    var svc = new UserService(ctx, hasher.Object);

    var token = await svc.GenerateEmailConfirmationTokenAsync(userId);

    Assert.False(string.IsNullOrWhiteSpace(token));

    var stored = await ctx.EmailConfirmationTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token);
    Assert.NotNull(stored);
  }
}
