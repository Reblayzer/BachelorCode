namespace IdentityService.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public sealed record ChangePasswordDto
{
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}
