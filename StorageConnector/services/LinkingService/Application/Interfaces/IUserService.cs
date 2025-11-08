namespace LinkingService.Application.Interfaces;

public interface IUserService
{
  Task<(bool Succeeded, IEnumerable<string> Errors, string? UserId)> CreateAsync(string email, string password);
  Task<string?> GenerateEmailConfirmationTokenAsync(string userId);
  Task<(string? UserId, string? Email)> FindByIdAsync(string userId);
  Task<(string? UserId, string? Email)> FindByEmailAsync(string email);
  Task<bool> ConfirmEmailAsync(string userId, string token);
  Task<bool> IsEmailConfirmedAsync(string userId);
  Task<bool> PasswordSignInAsync(string email, string password, bool isPersistent = true, bool lockoutOnFailure = true);
  Task SignOutAsync();
  Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
  Task<string?> GeneratePasswordResetTokenAsync(string userId);
  Task<bool> ResetPasswordAsync(string userId, string token, string newPassword);
}
