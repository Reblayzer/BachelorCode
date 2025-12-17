using LinkingService.Domain;

namespace LinkingService.Application.Interfaces;

public interface IFileProviderFactory
{
  // Resolve a file provider for the given provider type; throws if none registered.
  IFileProvider Get(ProviderType provider);

  // Try to resolve a file provider for the given provider type, returning true when found.
  bool TryGet(ProviderType provider, out IFileProvider? providerInstance);
}
