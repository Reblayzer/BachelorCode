using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using IdentityService.Api.DTOs;
using IdentityService.Infrastructure.Config;
using IdentityService.Infrastructure.Email;
using IdentityService.Infrastructure.Services;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IJwtService _jwtService;
    private readonly IEmailSender _email;
    private readonly IConfirmationLinkGenerator _linkGenerator;
    private readonly ILogger<AuthController> _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly IHostEnvironment _env;

    public AuthController(
        IUserService users,
        IJwtService jwtService,
        IEmailSender email,
        IConfirmationLinkGenerator linkGenerator,
        ILogger<AuthController> logger,
        IOptions<JwtSettings> jwtSettings,
        IHostEnvironment env)
    {
        _users = users;
        _jwtService = jwtService;
        _email = email;
        _linkGenerator = linkGenerator;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var (succeeded, errors, userId) = await _users.CreateAsync(dto.Email, dto.Password);
        if (!succeeded) return BadRequest(errors);

        var token = await _users.GenerateEmailConfirmationTokenAsync(userId!.Value);
        if (string.IsNullOrEmpty(token))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Failed to generate email confirmation token." });
        }

        var url = _linkGenerator.GenerateEmailConfirmationLink(
            userId.Value.ToString(), token, Request.Scheme, Request.Host.ToString());

        try
        {
            await _email.SendAsync(dto.Email, "Confirm your StorageConnector account",
                $"Click to confirm: <a href=\"{url}\">{url}</a>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", dto.Email);
            // In development, return success but surface the link so the flow can continue.
            return Accepted(new
            {
                message = "Registration created, but failed to send confirmation email.",
                confirmationLink = _env.IsDevelopment() ? url : null
            });
        }

        return Accepted(new { message = "Registration successful. Check your email to confirm." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return BadRequest();

        var (id, _) = await _users.FindByIdAsync(userGuid);
        if (id is null) return NotFound();

        var ok = await _users.ConfirmEmailAsync(userGuid, token);
        if (!ok) return BadRequest();

        return Redirect("http://localhost:5173/auth/confirmed");
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> Resend([FromBody] LoginDto dto)
    {
        var (userId, email) = await _users.FindByEmailAsync(dto.Email);
        if (userId is null) return Ok();

        var token = await _users.GenerateEmailConfirmationTokenAsync(userId.Value);
        if (string.IsNullOrEmpty(token))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Failed to generate email confirmation token." });
        }

        var url = _linkGenerator.GenerateEmailConfirmationLink(
            userId.Value.ToString(), token, Request.Scheme, Request.Host.ToString());

        try
        {
            await _email.SendAsync(email!, "Confirm your StorageConnector account",
                $"Click to confirm: <a href=\"{url}\">{url}</a>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend confirmation email to {Email}", email);
            return Accepted(new
            {
                message = "Confirmation token generated, but failed to send email.",
                confirmationLink = _env.IsDevelopment() ? url : null
            });
        }

        return Accepted(new { message = "Confirmation email sent." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (userId, _) = await _users.FindByEmailAsync(dto.Email);
        if (userId is null) return Unauthorized();

        if (!await _users.IsEmailConfirmedAsync(userId.Value))
            return Forbid();

        var user = await _users.ValidateCredentialsAsync(dto.Email, dto.Password);
        if (user == null) return Unauthorized();

        var token = _jwtService.GenerateToken(user);

        return Ok(new { token, email = user.Email });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // With JWT, logout is handled client-side by removing the token
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var (id, email) = await _users.FindByIdAsync(userId);
        if (id is null)
            return Unauthorized();

        return Ok(new { email = email ?? "" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var succeeded = await _users.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
        if (!succeeded)
            return BadRequest(new { message = "Current password is incorrect or new password is invalid." });

        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var (userId, email) = await _users.FindByEmailAsync(dto.Email);
        if (userId is null || email is null)
            return Ok(new { message = "If that email exists, a password reset link has been sent." });

        var token = await _users.GeneratePasswordResetTokenAsync(userId.Value);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to generate password reset token for user {UserId}", userId);
            return Ok(new { message = "If that email exists, a password reset link has been sent." });
        }

        var url = _linkGenerator.GeneratePasswordResetLink(email, token, Request.Scheme, Request.Host.ToString());

        try
        {
            await _email.SendAsync(email, "Reset your StorageConnector password",
                $"Click to reset your password: <a href=\"{url}\">{url}</a><br><br>If you didn't request this, please ignore this email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
        }

        return Ok(new { message = "If that email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var (userId, _) = await _users.FindByEmailAsync(dto.Email);
        if (userId is null)
            return BadRequest(new { message = "Invalid reset token or email." });

        var succeeded = await _users.ResetPasswordAsync(userId.Value, dto.Token, dto.NewPassword);
        if (!succeeded)
            return BadRequest(new { message = "Invalid or expired reset token." });

        return Ok(new { message = "Password reset successfully." });
    }

    [HttpPost("introspect")]
    public async Task<IActionResult> Introspect([FromBody] IntrospectRequest request)
    {
        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = handler.ValidateToken(request.Token, validationParams, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Ok(new IntrospectResponse(Active: false));
            }

            // Verify user still exists and is active
            var (id, userEmail) = await _users.FindByIdAsync(Guid.Parse(userId));
            if (id is null)
            {
                return Ok(new IntrospectResponse(Active: false));
            }

            return Ok(new IntrospectResponse(
                Active: true,
                UserId: userId,
                Email: email ?? userEmail,
                Exp: jwtToken?.ValidTo != null ? new DateTimeOffset(jwtToken.ValidTo).ToUnixTimeSeconds() : null,
                Issuer: _jwtSettings.Issuer
            ));
        }
        catch (SecurityTokenExpiredException)
        {
            return Ok(new IntrospectResponse(Active: false));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token introspection failed");
            return Ok(new IntrospectResponse(Active: false));
        }
    }
}
