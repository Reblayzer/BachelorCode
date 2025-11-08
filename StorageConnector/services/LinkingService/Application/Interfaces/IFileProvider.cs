using LinkingService.Application.DTOs;
using LinkingService.Domain;

namespace LinkingService.Application.Interfaces;

public interface IFileProvider
{
    ProviderType Provider { get; }

    // simple “page” result you can replace later
    Task<(IReadOnlyList<FileItem> items, string? nextPageToken)> ListAsync(
        string userId, string? folderId, int pageSize, string? pageToken);

    Task<FileMetadata> GetMetadataAsync(string userId, string fileId);
    Task<Uri> GetViewUrlAsync(string userId, string fileId);
}
