using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Application;
using Application.Interfaces;
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

builder.Services.AddScoped<LinkProviderService>();
builder.Services.AddScoped<ITokenStore, EfTokenStore>();
builder.Services.AddSingleton<IStateStore, CacheStateStore>();
builder.Services.AddSingleton<LinkScopes>();

builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection("OAuth:Google"));
builder.Services.Configure<MicrosoftOAuthOptions>(builder.Configuration.GetSection("OAuth:Microsoft"));
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection("Frontend"));
builder.Services.AddHttpClient<GoogleOAuthClient>();
builder.Services.AddHttpClient<MicrosoftOAuthClient>();
builder.Services.AddScoped<IOAuthClient>(sp => sp.GetRequiredService<GoogleOAuthClient>());
builder.Services.AddScoped<IOAuthClient>(sp => sp.GetRequiredService<MicrosoftOAuthClient>());

builder.Services.AddScoped<IFileProvider>(_ => new NullFileProvider(ProviderType.Google));
builder.Services.AddScoped<IFileProvider>(_ => new NullFileProvider(ProviderType.Microsoft));

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
app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
