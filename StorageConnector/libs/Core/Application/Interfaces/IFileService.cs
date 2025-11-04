using Application.DTOs;
using Domain;

namespace Application.Interfaces;

/// <summary>
/// Service that orchestrates file operations across multiple cloud storage providers.
/// Aggregates files from all linked providers for a unified view.
/// </summary>
public interface IFileService
{
  /// <summary>
  /// Get files from all linked providers for the authenticated user.
  /// </summary>
  /// <param name="userId">The user ID</param>
  /// <param name="pageSize">Number of items per provider (default 50)</param>
  /// <returns>List of files with provider information</returns>
  Task<IReadOnlyList<ProviderFileItem>> GetFilesFromAllProvidersAsync(
      string userId, int pageSize = 50);

  /// <summary>
  /// Get files from a specific provider.
  /// </summary>
  /// <param name="userId">The user ID</param>
  /// <param name="provider">The provider type</param>
  /// <param name="folderId">Optional folder ID to list files from</param>
  /// <param name="pageSize">Number of items per page</param>
  /// <param name="pageToken">Pagination token from previous request</param>
  /// <returns>List of files and next page token</returns>
  Task<(IReadOnlyList<FileItem> items, string? nextPageToken)> GetFilesByProviderAsync(
      string userId, ProviderType provider, string? folderId = null,
      int pageSize = 50, string? pageToken = null);

  /// <summary>
  /// Get detailed metadata for a specific file.
  /// </summary>
  /// <param name="userId">The user ID</param>
  /// <param name="provider">The provider type</param>
  /// <param name="fileId">The file ID</param>
  /// <returns>File metadata</returns>
  Task<FileMetadata> GetFileMetadataAsync(string userId, ProviderType provider, string fileId);

  /// <summary>
  /// Get the web view URL for a file.
  /// </summary>
  /// <param name="userId">The user ID</param>
  /// <param name="provider">The provider type</param>
  /// <param name="fileId">The file ID</param>
  /// <returns>Web view URI</returns>
  Task<Uri> GetFileViewUrlAsync(string userId, ProviderType provider, string fileId);
}
