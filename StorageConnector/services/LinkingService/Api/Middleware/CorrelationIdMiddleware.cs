namespace LinkingService.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<CorrelationIdMiddleware> _logger;
  private const string CorrelationIdHeader = "X-Correlation-ID";

  public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers[CorrelationIdHeader] = correlationId;

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      ["CorrelationId"] = correlationId
    }))
    {
      await _next(context);
    }
  }
}
