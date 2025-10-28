using System.Security.Claims;

namespace LinkingService.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string RequireUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No user id claim.");
}
