namespace IdentityService.Domain;

public sealed class PasswordResetToken
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public string Token { get; set; } = string.Empty;
  public DateTime ExpiresAt { get; set; }
  public bool IsUsed { get; set; }
}
