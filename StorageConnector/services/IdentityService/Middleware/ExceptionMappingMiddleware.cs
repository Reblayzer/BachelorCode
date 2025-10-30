using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentityService.Middleware;

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

      if (ex is InvalidOperationException && ex.Message?.IndexOf("state expired", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        context.Response.StatusCode = StatusCodes.Status410Gone;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "State expired" }));
        return;
      }

      if (ex is InvalidOperationException)
      {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        return;
      }

      context.Response.StatusCode = StatusCodes.Status500InternalServerError;
      await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
    }
  }
}
