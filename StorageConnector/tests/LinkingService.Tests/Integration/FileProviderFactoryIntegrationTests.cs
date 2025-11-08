using Microsoft.Extensions.DependencyInjection;
using LinkingService.Application.Interfaces;
using LinkingService.Infrastructure.Factories;
using LinkingService.Infrastructure.FileProviders;
using LinkingService.Domain;

namespace LinkingService.Tests.Integration
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
      services.AddScoped<IFileProviderFactory, FileProviderFactory>();

      var sp = services.BuildServiceProvider();

      var factory = sp.GetRequiredService<IFileProviderFactory>();

      Assert.NotNull(factory);

      var got = factory.Get(ProviderType.Google);
      Assert.Equal(ProviderType.Google, got.Provider);

      var ok = factory.TryGet(ProviderType.Microsoft, out var ms);
      Assert.True(ok);
      Assert.Equal(ProviderType.Microsoft, ms!.Provider);
    }
  }
}
