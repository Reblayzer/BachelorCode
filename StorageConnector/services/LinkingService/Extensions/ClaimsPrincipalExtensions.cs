using System.Security.Claims;

namespace LinkingService.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid RequireUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("No user id claim.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Invalid user id format.");

        return userId;
    }
}
