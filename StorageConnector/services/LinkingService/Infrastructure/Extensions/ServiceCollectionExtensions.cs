using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LinkingService.Application.Services;
using LinkingService.Infrastructure.Config;
using LinkingService.Infrastructure.FileProviders;
using LinkingService.Infrastructure.OAuth;
using LinkingService.Infrastructure.Http;
using LinkingService.Application.Interfaces;
using LinkingService.Infrastructure.Stores;

namespace LinkingService.Infrastructure.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddLinkingServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
      // Data protection, keys and other app-level wiring are left in Program.cs since they touch environment

      services.AddScoped<LinkProviderService>();
      services.AddScoped<IFileService, FileService>();
      services.AddScoped<ITokenStore, EfTokenStore>();
      services.AddSingleton<IStateStore, CacheStateStore>();
      services.AddSingleton<LinkScopes>();

      services.Configure<GoogleOAuthOptions>(configuration.GetSection("OAuth:Google"));
      services.Configure<MicrosoftOAuthOptions>(configuration.GetSection("OAuth:Microsoft"));
      services.Configure<FrontendOptions>(configuration.GetSection("Frontend"));

      // Validate frontend base URL so redirects are reliable
      services.AddOptions<FrontendOptions>()
        .Bind(configuration.GetSection("Frontend"))
        .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl) && Uri.IsWellFormedUriString(o.BaseUrl, UriKind.Absolute), "Frontend BaseUrl must be a valid absolute URL");

      services.AddOptions<GoogleOAuthOptions>()
          .Bind(configuration.GetSection("OAuth:Google"))
          .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId) && !string.IsNullOrWhiteSpace(o.ClientSecret), "Google OAuth client id/secret must be configured");

      services.AddOptions<MicrosoftOAuthOptions>()
          .Bind(configuration.GetSection("OAuth:Microsoft"))
          .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId) && !string.IsNullOrWhiteSpace(o.ClientSecret), "Microsoft OAuth client id/secret must be configured");

      services.AddTransient<RetryHandler>();

      services.AddHttpClient<GoogleOAuthClient>(client => { client.Timeout = TimeSpan.FromSeconds(15); })
          .AddHttpMessageHandler<RetryHandler>();

      services.AddHttpClient<MicrosoftOAuthClient>(client => { client.Timeout = TimeSpan.FromSeconds(15); })
          .AddHttpMessageHandler<RetryHandler>();

      // Configure HttpClients for file provider API calls
      services.AddHttpClient<GoogleFileProvider>(client => { client.Timeout = TimeSpan.FromSeconds(30); })
          .AddHttpMessageHandler<RetryHandler>();

      services.AddHttpClient<MicrosoftFileProvider>(client => { client.Timeout = TimeSpan.FromSeconds(30); })
          .AddHttpMessageHandler<RetryHandler>();

      services.AddScoped<IOAuthClient, GoogleOAuthClient>();
      services.AddScoped<IOAuthClient, MicrosoftOAuthClient>();

      // Register actual file providers (replacing Null implementations)
      services.AddScoped<IFileProvider, GoogleFileProvider>();
      services.AddScoped<IFileProvider, MicrosoftFileProvider>();

      services.AddScoped<IFileProviderFactory, Factories.FileProviderFactory>();

      return services;
    }
  }
}
