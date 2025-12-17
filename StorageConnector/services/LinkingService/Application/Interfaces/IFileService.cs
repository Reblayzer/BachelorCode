using LinkingService.Application.DTOs;
using LinkingService.Domain;

namespace LinkingService.Application.Interfaces;

// Orchestrates file operations across providers and aggregates linked accounts for a unified view.
public interface IFileService
{
  // Get files from all linked providers for the user.
  Task<IReadOnlyList<ProviderFileItem>> GetFilesFromAllProvidersAsync(
      Guid userId, int pageSize = 50);

  // Get files from a specific provider, with paging and optional folder scope.
  Task<(IReadOnlyList<FileItem> items, string? nextPageToken)> GetFilesByProviderAsync(
      Guid userId, ProviderType provider, string? folderId = null,
      int pageSize = 50, string? pageToken = null);

  // Get detailed metadata for a file from a given provider.
  Task<FileMetadata> GetFileMetadataAsync(Guid userId, ProviderType provider, string fileId);

  // Get the web view URL for a file from a given provider.
  Task<Uri> GetFileViewUrlAsync(Guid userId, ProviderType provider, string fileId);
}
