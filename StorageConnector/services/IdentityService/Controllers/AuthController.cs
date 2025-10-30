using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Infrastructure.Data;
using Infrastructure.Email;
using Application.Interfaces;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IEmailSender _email;
    private readonly Application.Interfaces.IConfirmationLinkGenerator _linkGenerator;
    private readonly Microsoft.Extensions.Logging.ILogger<AuthController> _logger;

    public AuthController(IUserService users, IEmailSender email, Application.Interfaces.IConfirmationLinkGenerator linkGenerator, Microsoft.Extensions.Logging.ILogger<AuthController> logger)
    {
        _users = users;
        _email = email;
        _linkGenerator = linkGenerator;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var (succeeded, errors, userId) = await _users.CreateAsync(dto.Email, dto.Password);
        if (!succeeded) return BadRequest(errors);

        var token = await _users.GenerateEmailConfirmationTokenAsync(userId!);
        if (string.IsNullOrEmpty(token))
        {
            // Token generation failed unexpectedly — surface an error to the caller.
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to generate email confirmation token." });
        }

        var url = _linkGenerator.GenerateEmailConfirmationLink(userId!, token, Request.Scheme, Request.Host.ToString());

        try
        {
            await _email.SendAsync(dto.Email, "Confirm your StorageConnector account",
                $"Click to confirm: <a href=\"{url}\">{url}</a>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to send confirmation email." });
        }

        return Accepted(new { message = "Registration successful. Check your email to confirm." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var (id, _) = await _users.FindByIdAsync(userId);
        if (id is null) return NotFound();

        var ok = await _users.ConfirmEmailAsync(userId, token);
        if (!ok) return BadRequest();

        return Redirect("/auth/confirmed");
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> Resend([FromBody] LoginDto dto)
    {
        var (userId, email) = await _users.FindByEmailAsync(dto.Email);
        if (userId is null) return Ok();

        var token = await _users.GenerateEmailConfirmationTokenAsync(userId!);
        if (string.IsNullOrEmpty(token))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to generate email confirmation token." });
        }

        var url = _linkGenerator.GenerateEmailConfirmationLink(userId!, token, Request.Scheme, Request.Host.ToString());

        try
        {
            await _email.SendAsync(email!, "Confirm your StorageConnector account",
                $"Click to confirm: <a href=\"{url}\">{url}</a>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend confirmation email to {Email}", email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to send confirmation email." });
        }

        return Accepted(new { message = "Confirmation email sent." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (userId, _) = await _users.FindByEmailAsync(dto.Email);
        if (userId is null) return Unauthorized();

        if (!await _users.IsEmailConfirmedAsync(userId))
            return Forbid();

        var ok = await _users.PasswordSignInAsync(dto.Email, dto.Password, true, true);
        return ok ? Ok() : Unauthorized();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _users.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();
        var (id, email) = await _users.FindByIdAsync(userId);
        if (id is null)
            return Unauthorized();

        return Ok(new { email = email ?? "" });
    }
}
