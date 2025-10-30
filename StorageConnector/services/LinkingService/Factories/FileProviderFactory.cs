using System;
using System.Collections.Generic;
using System.Linq;
using Application.Interfaces;
using Domain;

namespace LinkingService.Factories;

public sealed class FileProviderFactory : IFileProviderFactory
{
  private readonly IEnumerable<IFileProvider> _providers;

  public FileProviderFactory(IEnumerable<IFileProvider> providers)
  {
    _providers = providers ?? throw new ArgumentNullException(nameof(providers));
  }

  public IFileProvider Get(ProviderType provider)
  {
    var p = _providers.FirstOrDefault(x => x.Provider == provider);
    if (p is null)
      throw new Application.Exceptions.ProviderNotRegisteredException($"No IFileProvider registered for provider '{provider}'. Ensure a provider is registered in DI.");
    return p;
  }

  public bool TryGet(ProviderType provider, out IFileProvider? providerInstance)
  {
    providerInstance = _providers.FirstOrDefault(x => x.Provider == provider);
    return providerInstance is not null;
  }
}
