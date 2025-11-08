using IdentityService.Domain;

namespace IdentityService.Application.Interfaces;

public interface IUserService
{
  Task<(bool Succeeded, IEnumerable<string> Errors, Guid? UserId)> CreateAsync(string email, string password);
  Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId);
  Task<(Guid? UserId, string? Email)> FindByIdAsync(Guid userId);
  Task<(Guid? UserId, string? Email)> FindByEmailAsync(string email);
  Task<bool> ConfirmEmailAsync(Guid userId, string token);
  Task<bool> IsEmailConfirmedAsync(Guid userId);
  Task<User?> ValidateCredentialsAsync(string email, string password);
  Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
  Task<string?> GeneratePasswordResetTokenAsync(Guid userId);
  Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword);
}
