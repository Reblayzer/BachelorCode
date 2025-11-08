using System.Security.Claims;

namespace LinkingService.Application;

public static class ClaimsPrincipalExtensions
{
    public static Guid RequireUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("No user id claim.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Invalid user id format.");

        return userId;
    }
}
