namespace Contracts.Auth;

public sealed record ResetPasswordDto(string Email, string Token, string NewPassword);
