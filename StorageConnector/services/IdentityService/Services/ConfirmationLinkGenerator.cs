using System;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace IdentityService.Services
{
  // Use LinkGenerator to produce route-aware absolute URIs. This lives in the IdentityService
  // so the shared interface stays framework-agnostic while the runtime implementation can use
  // ASP.NET Core routing services.
  public class ConfirmationLinkGenerator : IConfirmationLinkGenerator
  {
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ConfirmationLinkGenerator(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
    {
      _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
      _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string GenerateEmailConfirmationLink(string userId, string token, string scheme, string host)
    {
      // Defensive: if token is null, return empty string (caller should handle this case).
      if (string.IsNullOrEmpty(token)) return string.Empty;

      if (string.IsNullOrEmpty(scheme)) scheme = "https";
      if (string.IsNullOrEmpty(host)) host = _httpContextAccessor.HttpContext?.Request?.Host.ToString() ?? "localhost";

      // Use LinkGenerator to build an absolute URI that respects routing configuration.
      var uri = _linkGenerator.GetUriByAction(action: "ConfirmEmail", controller: "Auth", values: new { userId, token }, scheme: scheme, host: new HostString(host));
      return uri ?? string.Empty;
    }

    public string GeneratePasswordResetLink(string email, string token, string scheme, string host)
    {
      if (string.IsNullOrEmpty(token)) return string.Empty;

      // Build a URL that points to the frontend reset password page
      var encodedToken = Uri.EscapeDataString(token);
      var encodedEmail = Uri.EscapeDataString(email);

      // Always point to the frontend URL for password reset (development)
      return $"http://localhost:5173/reset-password?token={encodedToken}&email={encodedEmail}";
    }
  }
}
