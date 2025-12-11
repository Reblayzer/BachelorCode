using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Http;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication - validate tokens but don't issue them
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "StorageConnector.IdentityService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "StorageConnector";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
      };
    });

builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["https://localhost:5173"];

builder.Services.AddCors(options =>
{
  options.AddPolicy("Spa", policy =>
      policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials());
});

// Resilience policies reused for proxy http clients
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var resiliencePolicy = Policy.WrapAsync<HttpResponseMessage>(retryPolicy, circuitBreakerPolicy);

// Health checks for the gateway itself
builder.Services.AddHealthChecks();

// YARP Reverse Proxy with resilience policies
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddSingleton<IForwarderHttpClientFactory>(_ =>
    new ResilientForwarderHttpClientFactory(resiliencePolicy));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Correlation ID middleware
app.Use(async (context, next) =>
{
  var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
      ?? Guid.NewGuid().ToString();

  context.Items["CorrelationId"] = correlationId;
  context.Response.Headers["X-Correlation-ID"] = correlationId;

  await next();
});

app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();

// Gateway health check
app.MapHealthChecks("/health");

// Reverse proxy
app.MapReverseProxy();

app.Run();

// Make Program accessible for testing
public partial class Program { }

internal sealed class ResilientForwarderHttpClientFactory : ForwarderHttpClientFactory
{
  private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

  public ResilientForwarderHttpClientFactory(IAsyncPolicy<HttpResponseMessage> resiliencePolicy)
  {
    _resiliencePolicy = resiliencePolicy ?? throw new ArgumentNullException(nameof(resiliencePolicy));
  }

  protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
  {
    var wrappedHandler = new PolicyHttpMessageHandler(_resiliencePolicy)
    {
      InnerHandler = handler
    };

    return base.WrapHandler(context, wrappedHandler);
  }
}
