using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageConnector.Api.Extensions;
using StorageConnector.Application;
using StorageConnector.Application.Interfaces;
using StorageConnector.Domain;
using StorageConnector.Infrastructure;

namespace StorageConnector.Api.Controllers;

[ApiController]
[Route("api/connect")]
[Authorize] // must be logged in to start/finish linking
public sealed class ConnectController : ControllerBase
{
    private readonly LinkProviderService _svc;
    private readonly LinkScopes _scopes;

    public ConnectController(LinkProviderService svc, LinkScopes scopes)
    {
        _svc = svc; _scopes = scopes;
    }

    // 1) Start OAuth: returns a provider authorize URL for the SPA to redirect to
    [HttpGet("{provider}/start")]
    public async Task<IActionResult> Start([FromRoute] ProviderType provider)
    {
        var userId = User.RequireUserId();
        var redirect = new Uri($"{Request.Scheme}://{Request.Host}/api/connect/{provider}/callback");
        var url = await _svc.StartAsync(userId, provider, redirect, _scopes.For(provider));
        return Ok(new { redirectUrl = url.ToString() });
    }

    // 2) OAuth callback: provider redirects here with state+code
    // Keep [Authorize] if your SPA preserves the auth cookie during the round-trip.
    // If not yet, temporarily use [AllowAnonymous] and resolve user differently.
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback([FromRoute] ProviderType provider, [FromQuery] string state, [FromQuery] string code)
    {
        var userId = User.RequireUserId();
        var redirect = new Uri($"{Request.Scheme}://{Request.Host}/api/connect/{provider}/callback");
        await _svc.ConnectCallbackAsync(userId, state, code, redirect);

        // Redirect to a SPA route or return 204 for XHR-based flows
        return Redirect("/connections/success");
    }

    // 3) Optional: disconnect a provider
    [HttpPost("{provider}/disconnect")]
    public async Task<IActionResult> Disconnect([FromRoute] ProviderType provider, [FromServices] ITokenStore tokens)
    {
        var userId = User.RequireUserId();
        await tokens.DeleteAsync(userId, provider);
        return NoContent();
    }
}
