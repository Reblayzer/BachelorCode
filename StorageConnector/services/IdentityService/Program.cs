using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Email;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.SignIn.RequireConfirmedEmail = true;
        opts.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<Application.Interfaces.IUserService, IdentityService.Services.UserService>();
builder.Services.AddScoped<Application.Interfaces.IConfirmationLinkGenerator, IdentityService.Services.ConfirmationLinkGenerator>();

// Make LinkGenerator's IHttpContextAccessor available to the ConfirmationLinkGenerator
builder.Services.AddHttpContextAccessor();

var keysPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data", "dp-keys"));
Directory.CreateDirectory(keysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("StorageConnector");

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

// Always use SendGrid for sending emails
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();

// Validate SendGrid options
builder.Services.AddOptions<Infrastructure.Email.SendGridOptions>()
    .Bind(builder.Configuration.GetSection("Email:SendGrid"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey) && !string.IsNullOrWhiteSpace(o.FromEmail), "SendGrid ApiKey and FromEmail must be configured");

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
// Map domain exceptions to friendly HTTP responses
app.UseMiddleware<IdentityService.Middleware.ExceptionMappingMiddleware>();
app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
