using System.Text;
using IdentityService.Configuration;
using IdentityService.Data;
using IdentityService.Services;
using Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<Application.Interfaces.IConfirmationLinkGenerator, IdentityService.Services.ConfirmationLinkGenerator>();
builder.Services.AddHttpContextAccessor();

// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "https://localhost:5173" };

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
app.UseMiddleware<IdentityService.Middleware.ExceptionMappingMiddleware>();
app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
