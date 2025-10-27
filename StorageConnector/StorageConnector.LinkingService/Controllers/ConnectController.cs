using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageConnector.Application;
using StorageConnector.Application.Interfaces;
using StorageConnector.Domain;
using StorageConnector.LinkingService.Extensions;
using StorageConnector.Infrastructure.Config;

namespace StorageConnector.LinkingService.Controllers;

[ApiController]
[Route("api/connect")]
[Authorize]
public sealed class ConnectController : ControllerBase
{
    private readonly LinkProviderService _service;
    private readonly LinkScopes _scopes;

    public ConnectController(LinkProviderService service, LinkScopes scopes)
    {
        _service = service;
        _scopes = scopes;
    }

    [HttpGet("{provider}/start")]
    public async Task<IActionResult> Start([FromRoute] ProviderType provider)
    {
        var userId = User.RequireUserId();
        var redirect = new Uri($"{Request.Scheme}://{Request.Host}/api/connect/{provider}/callback");
        var url = await _service.StartAsync(userId, provider, redirect, _scopes.For(provider));
        return Ok(new { redirectUrl = url.ToString() });
    }

    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback([FromRoute] ProviderType provider, [FromQuery] string state, [FromQuery] string code)
    {
        var userId = User.RequireUserId();
        var redirect = new Uri($"{Request.Scheme}://{Request.Host}/api/connect/{provider}/callback");
        await _service.ConnectCallbackAsync(userId, state, code, redirect);
        return Redirect("/connections/success");
    }

    [HttpPost("{provider}/disconnect")]
    public async Task<IActionResult> Disconnect([FromRoute] ProviderType provider, [FromServices] ITokenStore tokens)
    {
        var userId = User.RequireUserId();
        await tokens.DeleteAsync(userId, provider);
        return NoContent();
    }
}
