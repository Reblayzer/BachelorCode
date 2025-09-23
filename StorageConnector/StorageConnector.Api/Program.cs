using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StorageConnector.Application;
using StorageConnector.Application.Interfaces;
using StorageConnector.Domain;
using StorageConnector.Infrastructure;
using StorageConnector.Infrastructure.Data;
using StorageConnector.Infrastructure.Email;
using StorageConnector.Infrastructure.FileProviders;
using StorageConnector.Infrastructure.OAuth;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
{
    opts.SignIn.RequireConfirmedEmail = true;
    opts.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();
builder.Services.AddDataProtection();

// Application services
builder.Services.AddScoped<LinkProviderService>();
builder.Services.AddScoped<ITokenStore, EfTokenStore>();
builder.Services.AddSingleton<IStateStore, CacheStateStore>();
builder.Services.AddSingleton<LinkScopes>();

// Email sender (env-based)
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("Email:SendGrid"));
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();

// OAuth stubs (replace with real implementations later)
builder.Services.AddScoped<IOAuthClient, GoogleOAuthClientStub>();
builder.Services.AddScoped<IOAuthClient, MicrosoftOAuthClientStub>();

// Temporary file providers
builder.Services.AddScoped<IFileProvider>(_ => new NullFileProvider(ProviderType.Google));
builder.Services.AddScoped<IFileProvider>(_ => new NullFileProvider(ProviderType.Microsoft));

// Web
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
