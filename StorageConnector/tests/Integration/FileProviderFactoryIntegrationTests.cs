using System;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces;
using Infrastructure.FileProviders;
using Xunit;

namespace Tests.Integration
{
  public class FileProviderFactoryIntegrationTests
  {
    [Fact]
    public void ResolveFactory_FromServiceCollection_WiresProviders()
    {
      var services = new ServiceCollection();

      // register the same small set of providers as in Program.cs
      services.AddScoped<IFileProvider, GoogleNullFileProvider>();
      services.AddScoped<IFileProvider, MicrosoftNullFileProvider>();
      services.AddScoped<IFileProviderFactory, LinkingService.Factories.FileProviderFactory>();

      var sp = services.BuildServiceProvider();

      var factory = sp.GetRequiredService<IFileProviderFactory>();

      Assert.NotNull(factory);

      var got = factory.Get(Domain.ProviderType.Google);
      Assert.Equal(Domain.ProviderType.Google, got.Provider);

      var ok = factory.TryGet(Domain.ProviderType.Microsoft, out var ms);
      Assert.True(ok);
      Assert.Equal(Domain.ProviderType.Microsoft, ms!.Provider);
    }
  }
}
