using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageConnector.Api.Contracts.Connections;
using StorageConnector.Api.Extensions;
using StorageConnector.Infrastructure.Data;

namespace StorageConnector.Api.Controllers;

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
            .Select(x => new ConnectionStatusResponse {
                Provider = x.Provider.ToString(),
                Connected = true,
                ExpiresAtUtc = x.ExpiresAtUtc
            })
            .ToListAsync();

        return Ok(list);
    }
}