using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StorageConnector.Contracts.Auth;
using StorageConnector.Infrastructure.Data;
using StorageConnector.Infrastructure.Email;

namespace StorageConnector.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IEmailSender _email;

    public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IEmailSender email)
    {
        _users = users;
        _signIn = signIn;
        _email = email;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var url = Url.Action(
            action: nameof(ConfirmEmail),
            controller: "Auth",
            values: new { userId = user.Id, token },
            protocol: Request.Scheme,
            host: Request.Host.ToString());

        await _email.SendAsync(user.Email!, "Confirm your StorageConnector account",
            $"Click to confirm: <a href=\"{url}\">{url}</a>");

        return Accepted(new { message = "Registration successful. Check your email to confirm." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var res = await _users.ConfirmEmailAsync(user, token);
        if (!res.Succeeded) return BadRequest(res.Errors);

        return Redirect("/auth/confirmed");
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> Resend([FromBody] LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null) return Ok();

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var url = Url.Action(nameof(ConfirmEmail), "Auth",
            new { userId = user.Id, token }, Request.Scheme, Request.Host.ToString());

        await _email.SendAsync(user.Email!, "Confirm your StorageConnector account",
            $"Click to confirm: <a href=\"{url}\">{url}</a>");

        return Accepted(new { message = "Confirmation email sent." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized();

        if (!await _users.IsEmailConfirmedAsync(user))
            return Forbid();

        var res = await _signIn.PasswordSignInAsync(user, dto.Password, isPersistent: true, lockoutOnFailure: true);
        return res.Succeeded ? Ok() : Unauthorized();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return NoContent();
    }
}
