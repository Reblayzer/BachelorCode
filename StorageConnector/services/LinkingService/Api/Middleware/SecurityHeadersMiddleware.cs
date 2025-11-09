namespace LinkingService.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// </summary>
public sealed class SecurityHeadersMiddleware
{
  private readonly RequestDelegate _next;

  public SecurityHeadersMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    // X-Content-Type-Options: Prevents MIME-type sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // X-Frame-Options: Prevents clickjacking attacks
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // X-XSS-Protection: Enables XSS filter in older browsers
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // Referrer-Policy: Controls how much referrer information is included
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content-Security-Policy: Helps prevent XSS attacks
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'");

    // Permissions-Policy: Disables unnecessary browser features
    context.Response.Headers.Append("Permissions-Policy",
        "geolocation=(), microphone=(), camera=()");

    await _next(context);
  }
}
