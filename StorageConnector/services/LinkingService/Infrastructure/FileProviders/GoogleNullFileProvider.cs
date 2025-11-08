using LinkingService.Application.DTOs;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;

namespace LinkingService.Infrastructure.FileProviders;

// Typed adapter that composes a NullFileProvider instance and implements IFileProvider.
// This avoids inheriting from the sealed NullFileProvider while providing a distinct DI type.
public sealed class GoogleNullFileProvider : IFileProvider
{
  private readonly NullFileProvider _inner = new NullFileProvider(ProviderType.Google);

  public ProviderType Provider => _inner.Provider;

  public Task<(IReadOnlyList<FileItem>, string?)> ListAsync(string userId, string? folderId, int pageSize, string? pageToken)
      => _inner.ListAsync(userId, folderId, pageSize, pageToken);

  public Task<FileMetadata> GetMetadataAsync(string userId, string fileId)
      => _inner.GetMetadataAsync(userId, fileId);

  public Task<Uri> GetViewUrlAsync(string userId, string fileId)
      => _inner.GetViewUrlAsync(userId, fileId);
}
