using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Application.Interfaces;
using Infrastructure.FileProviders;

namespace Tests.Integration
{
  public class LinkingServiceHostTests
  {
    [Fact]
    public void WebApplicationHost_Resolves_FileProviderFactory()
    {
      var builder = WebApplication.CreateBuilder();

      // mimic the relevant parts of LinkingService Program.cs wiring
      builder.Services.AddScoped<IFileProvider, GoogleNullFileProvider>();
      builder.Services.AddScoped<IFileProvider, MicrosoftNullFileProvider>();
      builder.Services.AddScoped<IFileProviderFactory, LinkingService.Factories.FileProviderFactory>();

      var app = builder.Build();

      var factory = app.Services.GetService<IFileProviderFactory>();

      Assert.NotNull(factory);

      var google = factory.Get(Domain.ProviderType.Google);
      Assert.Equal(Domain.ProviderType.Google, google.Provider);

      var ok = factory.TryGet(Domain.ProviderType.Microsoft, out var ms);
      Assert.True(ok);
      Assert.Equal(Domain.ProviderType.Microsoft, ms!.Provider);
    }
  }
}
