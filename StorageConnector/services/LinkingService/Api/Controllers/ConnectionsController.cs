using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinkingService.Api.DTOs;
using LinkingService.Application;
using LinkingService.Infrastructure.Data;

namespace LinkingService.Api.Controllers;

[ApiController]
[Route("api/connections")]
[Authorize]
public sealed class ConnectionsController : ControllerBase
{
    private readonly LinkingDbContext _db;

    public ConnectionsController(LinkingDbContext db) => _db = db;

    [HttpGet("status")]
    public async Task<ActionResult<IReadOnlyList<ConnectionStatusResponse>>> Status()
    {
        var userId = User.RequireUserId();

        var list = await _db.ProviderAccounts
            .Where(x => x.UserId == userId)
            .Select(x => new ConnectionStatusResponse(
                x.Provider,
                true,
                x.ScopeCsv.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .ToListAsync();

        return Ok(list);
    }
}
