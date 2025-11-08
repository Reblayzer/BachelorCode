namespace IdentityService.Domain;

public sealed class User
{
  public Guid Id { get; set; }
  public string Email { get; set; } = string.Empty;
  public string PasswordHash { get; set; } = string.Empty;
  public bool EmailConfirmed { get; set; }
  public DateTime CreatedAt { get; set; }
}
