using System.Text;
using IdentityService.Infrastructure.Config;
using IdentityService.Infrastructure.Data;
using IdentityService.Infrastructure.Services;
using IdentityService.Infrastructure.Email;
using IdentityService.Application.Interfaces;
using IdentityService.Api.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Database - separate database for IdentityService
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// JWT Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IConfirmationLinkGenerator, ConfirmationLinkGenerator>();
builder.Services.AddHttpContextAccessor();

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

// Email
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("Email:SendGrid"));
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();

builder.Services.AddOptions<SendGridOptions>()
    .Bind(builder.Configuration.GetSection("Email:SendGrid"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey) && !string.IsNullOrWhiteSpace(o.FromEmail),
        "SendGrid ApiKey and FromEmail must be configured");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Security Headers
app.UseMiddleware<IdentityService.Api.Middleware.SecurityHeadersMiddleware>();

// HSTS (HTTP Strict Transport Security) - only in production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseIpRateLimiting();
app.UseMiddleware<IdentityService.Api.Middleware.ExceptionMappingMiddleware>();
app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
