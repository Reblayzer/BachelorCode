using System.Security.Cryptography;
using IdentityService.Data;
using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public sealed class UserService
{
  private readonly IdentityDbContext _db;
  private readonly IPasswordHasher _passwordHasher;

  public UserService(IdentityDbContext db, IPasswordHasher passwordHasher)
  {
    _db = db;
    _passwordHasher = passwordHasher;
  }

  public async Task<(bool Succeeded, IEnumerable<string> Errors, Guid? UserId)> CreateAsync(string email, string password)
  {
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
      return (false, new[] { "Email and password are required." }, null);

    if (password.Length < 8)
      return (false, new[] { "Password must be at least 8 characters long." }, null);

    if (await _db.Users.AnyAsync(u => u.Email == email))
      return (false, new[] { "Email is already registered." }, null);

    var user = new User
    {
      Id = Guid.NewGuid(),
      Email = email,
      PasswordHash = _passwordHasher.HashPassword(password),
      EmailConfirmed = false,
      CreatedAt = DateTime.UtcNow
    };

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    return (true, Array.Empty<string>(), user.Id);
  }

  public async Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId)
  {
    var user = await _db.Users.FindAsync(userId);
    if (user == null) return null;

    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var confirmationToken = new EmailConfirmationToken
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Token = token,
      ExpiresAt = DateTime.UtcNow.AddHours(24),
      IsUsed = false
    };

    _db.EmailConfirmationTokens.Add(confirmationToken);
    await _db.SaveChangesAsync();

    return token;
  }

  public async Task<(Guid? UserId, string? Email)> FindByIdAsync(Guid userId)
  {
    var user = await _db.Users.FindAsync(userId);
    return user != null ? (user.Id, user.Email) : (null, null);
  }

  public async Task<(Guid? UserId, string? Email)> FindByEmailAsync(string email)
  {
    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    return user != null ? (user.Id, user.Email) : (null, null);
  }

  public async Task<bool> ConfirmEmailAsync(Guid userId, string token)
  {
    var confirmationToken = await _db.EmailConfirmationTokens
        .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

    if (confirmationToken == null) return false;

    var user = await _db.Users.FindAsync(userId);
    if (user == null) return false;

    user.EmailConfirmed = true;
    confirmationToken.IsUsed = true;

    await _db.SaveChangesAsync();
    return true;
  }

  public async Task<bool> IsEmailConfirmedAsync(Guid userId)
  {
    var user = await _db.Users.FindAsync(userId);
    return user?.EmailConfirmed ?? false;
  }

  public async Task<User?> ValidateCredentialsAsync(string email, string password)
  {
    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null) return null;

    return _passwordHasher.VerifyPassword(password, user.PasswordHash) ? user : null;
  }

  public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
  {
    var user = await _db.Users.FindAsync(userId);
    if (user == null) return false;

    if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
      return false;

    if (newPassword.Length < 8)
      return false;

    user.PasswordHash = _passwordHasher.HashPassword(newPassword);
    await _db.SaveChangesAsync();

    return true;
  }

  public async Task<string?> GeneratePasswordResetTokenAsync(Guid userId)
  {
    var user = await _db.Users.FindAsync(userId);
    if (user == null) return null;

    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var resetToken = new PasswordResetToken
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Token = token,
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false
    };

    _db.PasswordResetTokens.Add(resetToken);
    await _db.SaveChangesAsync();

    return token;
  }

  public async Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword)
  {
    var resetToken = await _db.PasswordResetTokens
        .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

    if (resetToken == null) return false;

    var user = await _db.Users.FindAsync(userId);
    if (user == null) return false;

    if (newPassword.Length < 8) return false;

    user.PasswordHash = _passwordHasher.HashPassword(newPassword);
    resetToken.IsUsed = true;

    await _db.SaveChangesAsync();
    return true;
  }
}
