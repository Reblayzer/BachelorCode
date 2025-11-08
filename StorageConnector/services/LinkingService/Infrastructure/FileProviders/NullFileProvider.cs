using LinkingService.Application.DTOs;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;

namespace LinkingService.Infrastructure.FileProviders;

public sealed class NullFileProvider : IFileProvider
{
    public ProviderType Provider { get; }
    public NullFileProvider(ProviderType provider) => Provider = provider;

    public Task<(IReadOnlyList<FileItem>, string?)> ListAsync(
        string userId, string? folderId, int pageSize, string? pageToken)
        => Task.FromResult<(IReadOnlyList<FileItem>, string?)>((Array.Empty<FileItem>(), null));

    public Task<FileMetadata> GetMetadataAsync(string userId, string fileId)
        => Task.FromResult(new FileMetadata(fileId, "N/A", null, null, null, null));

    public Task<Uri> GetViewUrlAsync(string userId, string fileId)
        => Task.FromResult(new Uri("about:blank"));
}
