using LinkingService.Application.Exceptions;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;
using LinkingService.Infrastructure.Factories;
using Moq;

namespace LinkingService.Tests.Unit;

public class FileProviderFactoryTests
{
  [Fact]
  public void Get_ReturnsMatchingProvider()
  {
    var google = new Mock<IFileProvider>();
    google.Setup(g => g.Provider).Returns(ProviderType.Google);

    var ms = new Mock<IFileProvider>();
    ms.Setup(m => m.Provider).Returns(ProviderType.Microsoft);

    var factory = new FileProviderFactory([google.Object, ms.Object]);

    var p = factory.Get(ProviderType.Google);

    Assert.Same(google.Object, p);
  }

  [Fact]
  public void Get_ThrowsWhenNotFound()
  {
    var factory = new FileProviderFactory(Array.Empty<IFileProvider>());

    var ex = Assert.Throws<ProviderNotRegisteredException>(() => factory.Get(ProviderType.Google));
    Assert.Contains("No IFileProvider registered for provider", ex.Message);
  }

  [Fact]
  public void TryGet_ReturnsTrueWhenFound()
  {
    var google = new Mock<IFileProvider>();
    google.Setup(g => g.Provider).Returns(ProviderType.Google);
    var factory = new FileProviderFactory([google.Object]);

    var ok = factory.TryGet(ProviderType.Google, out var provider);

    Assert.True(ok);
    Assert.Same(google.Object, provider);
  }

  [Fact]
  public void TryGet_ReturnsFalseWhenMissing()
  {
    var factory = new FileProviderFactory([]);

    var ok = factory.TryGet(ProviderType.Google, out var provider);

    Assert.False(ok);
    Assert.Null(provider);
  }
}