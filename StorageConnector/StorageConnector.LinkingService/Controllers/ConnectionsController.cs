using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageConnector.Contracts.Connections;
using StorageConnector.LinkingService.Extensions;
using StorageConnector.Infrastructure.Data;

namespace StorageConnector.LinkingService.Controllers;

[ApiController]
[Route("api/connections")]
[Authorize]
public sealed class ConnectionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConnectionsController(AppDbContext db) => _db = db;

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
