using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Application;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces;
using LinkingService.Extensions;
using Domain;
using Infrastructure;
using Infrastructure.Config;
using Infrastructure.Data;
using Infrastructure.Email;
using Infrastructure.FileProviders;
using Infrastructure.OAuth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.SignIn.RequireConfirmedEmail = true;
        opts.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

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

// Register linking service infrastructure (IoC wiring) from a shared extension so tests
// and Program can use the same registrations.
builder.Services.AddLinkingServiceInfrastructure(builder.Configuration);

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

// Map domain exceptions to friendly HTTP responses for the SPA.
app.UseMiddleware<LinkingService.Middleware.ExceptionMappingMiddleware>();

app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
