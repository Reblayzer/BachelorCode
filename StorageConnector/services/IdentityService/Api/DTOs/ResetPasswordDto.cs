namespace IdentityService.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public sealed record ResetPasswordDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}
