using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using LinkingService.Application.Interfaces;
using LinkingService.Infrastructure.Factories;
using LinkingService.Infrastructure.FileProviders;
using LinkingService.Domain;

namespace LinkingService.Tests.Integration;

public class LinkingServiceHostTests
{
  [Fact]
  public void WebApplicationHost_Resolves_FileProviderFactory()
  {
    var builder = WebApplication.CreateBuilder();

    // mimic the relevant parts of LinkingService Program.cs wiring
    builder.Services.AddScoped<IFileProvider, GoogleNullFileProvider>();
    builder.Services.AddScoped<IFileProvider, MicrosoftNullFileProvider>();
    builder.Services.AddScoped<IFileProviderFactory, FileProviderFactory>();

    var app = builder.Build();

    var factory = app.Services.GetService<IFileProviderFactory>();

    Assert.NotNull(factory);

    var google = factory.Get(ProviderType.Google);
    Assert.Equal(ProviderType.Google, google.Provider);

    var ok = factory.TryGet(ProviderType.Microsoft, out var ms);
    Assert.True(ok);
    Assert.Equal(ProviderType.Microsoft, ms!.Provider);
  }
}