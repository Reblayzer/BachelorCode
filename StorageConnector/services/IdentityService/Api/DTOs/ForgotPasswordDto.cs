namespace IdentityService.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public sealed record ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;
}
