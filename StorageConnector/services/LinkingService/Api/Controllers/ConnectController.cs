using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LinkingService.Application;
using LinkingService.Application.Interfaces;
using LinkingService.Application.Services;
using LinkingService.Domain;
using LinkingService.Infrastructure.Config;
using Microsoft.Extensions.Options;

namespace LinkingService.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/connect")]
[ApiVersion("1.0")]
[Authorize]
public sealed class ConnectController : ControllerBase
{
    private readonly LinkProviderService _service;
    private readonly LinkScopes _scopes;
    private readonly string _frontendBaseUrl;

    public ConnectController(
        LinkProviderService service,
        LinkScopes scopes,
        IOptions<FrontendOptions> frontendOptions)
    {
        _service = service;
        _scopes = scopes;
        var configured = frontendOptions.Value.BaseUrl.TrimEnd('/');
        _frontendBaseUrl = string.IsNullOrWhiteSpace(configured)
            ? "https://localhost:5173"
            : configured;
    }

    [HttpGet("{provider}/start")]
    public async Task<IActionResult> Start([FromRoute] ProviderType provider)
    {
        var userId = User.RequireUserId();
        // Always include the API version segment so the callback hits this controller
        var redirect = new Uri($"{Request.Scheme}://{Request.Host}/api/v1/connect/{provider}/callback");
        var url = await _service.StartAsync(userId, provider, redirect, _scopes.For(provider));
        return Ok(new { redirectUrl = url.ToString() });
    }

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromRoute] ProviderType provider, [FromQuery] string state, [FromQuery] string code)
    {
        // Must match the redirect URI registered with the provider
        var redirect = new Uri($"{Request.Scheme}://{Request.Host}/api/v1/connect/{provider}/callback");
        await _service.ConnectCallbackAsync(state, code, redirect);
        return Redirect($"{_frontendBaseUrl}/connections/success?provider={provider}");
    }

    [HttpPost("{provider}/disconnect")]
    public async Task<IActionResult> Disconnect([FromRoute] ProviderType provider, [FromServices] ITokenStore tokens)
    {
        var userId = User.RequireUserId();
        await tokens.DeleteAsync(userId, provider);
        return NoContent();
    }
}
