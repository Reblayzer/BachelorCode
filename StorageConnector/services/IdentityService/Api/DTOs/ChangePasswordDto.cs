namespace IdentityService.Api.DTOs;

public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword);
