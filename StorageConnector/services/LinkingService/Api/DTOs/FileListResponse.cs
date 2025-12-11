using LinkingService.Application.DTOs;

namespace LinkingService.Api.Controllers;

/// <summary>
/// Response DTO for paginated file lists.
/// </summary>
/// <param name="Items">The list of files.</param>
/// <param name="NextPageToken">Token for retrieving the next page of results.</param>
public sealed record FileListResponse(
    IReadOnlyList<FileItem> Items,
    string? NextPageToken
);
