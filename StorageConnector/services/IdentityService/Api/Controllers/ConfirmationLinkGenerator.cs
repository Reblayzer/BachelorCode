using IdentityService.Application.Interfaces;

namespace IdentityService.Api.Controllers;

public class ConfirmationLinkGenerator : IConfirmationLinkGenerator
{
  private readonly LinkGenerator _linkGenerator;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public ConfirmationLinkGenerator(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
  {
    _linkGenerator = linkGenerator;
    _httpContextAccessor = httpContextAccessor;
  }

  public string GenerateEmailConfirmationLink(string userId, string token, string scheme, string host)
  {
    if (string.IsNullOrEmpty(token)) return string.Empty;
    if (string.IsNullOrEmpty(scheme)) scheme = "https";
    if (string.IsNullOrEmpty(host)) host = _httpContextAccessor.HttpContext?.Request.Host.ToString() ?? "localhost";
    var uri = _linkGenerator.GetUriByAction(action: "ConfirmEmail", controller: "Auth", values: new { userId, token }, scheme: scheme, host: new HostString(host));
    return uri ?? string.Empty;
  }

  public string GeneratePasswordResetLink(string email, string token, string scheme, string host)
  {
    if (string.IsNullOrEmpty(token)) return string.Empty;
    var encodedToken = Uri.EscapeDataString(token);
    var encodedEmail = Uri.EscapeDataString(email);
    return $"http://localhost:5173/reset-password?token={encodedToken}&email={encodedEmail}";
  }
}
