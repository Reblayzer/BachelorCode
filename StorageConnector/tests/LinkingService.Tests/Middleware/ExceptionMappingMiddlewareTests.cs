using System.Text.Json;
using LinkingService.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace LinkingService.Tests.Middleware;

public sealed class ExceptionMappingMiddlewareTests
{
  private sealed class NoOpLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
  }

  [Fact]
  public async Task InvokeAsync_StateExpiredException_Produces_410()
  {
    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    RequestDelegate next = _ => Task.FromException(new InvalidOperationException("State expired"));
    var middleware = new ExceptionMappingMiddleware(next, new NoOpLogger<ExceptionMappingMiddleware>());

    await middleware.InvokeAsync(context);

    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var json = await new StreamReader(context.Response.Body).ReadToEndAsync();

    Assert.Equal(410, context.Response.StatusCode);
    var doc = JsonDocument.Parse(json);
    Assert.Equal("State expired", doc.RootElement.GetProperty("error").GetString());
  }

  [Fact]
  public async Task InvokeAsync_InvalidOperationException_Produces_400()
  {
    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();

    RequestDelegate next = _ => Task.FromException(new InvalidOperationException("Some bad input"));
    var middleware = new ExceptionMappingMiddleware(next, new NoOpLogger<ExceptionMappingMiddleware>());

    await middleware.InvokeAsync(context);

    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var json = await new StreamReader(context.Response.Body).ReadToEndAsync();

    Assert.Equal(400, context.Response.StatusCode);
    var doc = JsonDocument.Parse(json);
    Assert.Equal("Some bad input", doc.RootElement.GetProperty("error").GetString());
  }
}
