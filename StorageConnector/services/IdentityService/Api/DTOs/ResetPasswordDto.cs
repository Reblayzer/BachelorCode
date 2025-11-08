namespace IdentityService.Api.DTOs;

public sealed record ResetPasswordDto(string Email, string Token, string NewPassword);
