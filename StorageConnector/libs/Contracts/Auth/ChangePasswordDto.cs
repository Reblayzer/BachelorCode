namespace Contracts.Auth;

public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword);
