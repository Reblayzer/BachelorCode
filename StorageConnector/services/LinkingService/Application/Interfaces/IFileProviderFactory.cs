using LinkingService.Domain;

namespace LinkingService.Application.Interfaces;

public interface IFileProviderFactory
{
  /// <summary>
  /// Resolve an <see cref="IFileProvider"/> for the given <see cref="ProviderType"/>.
  /// Throws if a matching provider is not registered.
  /// </summary>
  IFileProvider Get(ProviderType provider);

  /// <summary>
  /// Try to resolve an <see cref="IFileProvider"/> for the given provider.
  /// Returns true and sets <c>provider</c> when found; otherwise false.
  /// </summary>
  bool TryGet(ProviderType provider, out IFileProvider? providerInstance);
}
