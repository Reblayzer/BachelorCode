using LinkingService.Application.DTOs;

namespace LinkingService.Api.Controllers;

// Response DTO for a page of files and an optional next page token.
public sealed record FileListResponse(
    IReadOnlyList<FileItem> Items,
    string? NextPageToken
);
