using System.Text.Json;

namespace IdentityService.Api.Middleware;

public sealed class ExceptionMappingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<ExceptionMappingMiddleware> _logger;

  public ExceptionMappingMiddleware(RequestDelegate next, ILogger<ExceptionMappingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unhandled exception while processing request");

      context.Response.ContentType = "application/json";

      switch (ex)
      {
        case InvalidOperationException when ex.Message.Contains("state expired", StringComparison.OrdinalIgnoreCase):
          context.Response.StatusCode = StatusCodes.Status410Gone;
          await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "State expired" }));
          return;
        case InvalidOperationException:
          context.Response.StatusCode = StatusCodes.Status400BadRequest;
          await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
          return;
        default:
          context.Response.StatusCode = StatusCodes.Status500InternalServerError;
          await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
          break;
      }
    }
  }
}
