using LinkingService.Application.DTOs;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;
using LinkingService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkingService.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public sealed class FilesController : ControllerBase
{
  private readonly IFileService _fileService;
  private readonly ILogger<FilesController> _logger;

  public FilesController(IFileService fileService, ILogger<FilesController> logger)
  {
    _fileService = fileService;
    _logger = logger;
  }

  /// <summary>
  /// Get files from all linked providers (unified dashboard view).
  /// </summary>
  [HttpGet]
  public async Task<ActionResult<IReadOnlyList<ProviderFileItem>>> GetAllFiles(
      [FromQuery] int pageSize = 50)
  {
    var userId = User.RequireUserId();

    var files = await _fileService.GetFilesFromAllProvidersAsync(userId, pageSize);

    _logger.LogInformation("Returned {Count} files for user {UserId}", files.Count, userId);

    return Ok(files);
  }

  /// <summary>
  /// Get files from a specific provider with pagination support.
  /// </summary>
  [HttpGet("{provider}")]
  public async Task<ActionResult<FileListResponse>> GetFilesByProvider(
      ProviderType provider,
      [FromQuery] string? folderId = null,
      [FromQuery] int pageSize = 50,
      [FromQuery] string? pageToken = null)
  {
    var userId = User.RequireUserId();

    var (items, nextPageToken) = await _fileService.GetFilesByProviderAsync(
        userId, provider, folderId, pageSize, pageToken);

    return Ok(new FileListResponse(items, nextPageToken));
  }

  /// <summary>
  /// Get detailed metadata for a specific file.
  /// </summary>
  [HttpGet("{provider}/{fileId}/metadata")]
  public async Task<ActionResult<FileMetadata>> GetFileMetadata(
      ProviderType provider,
      string fileId)
  {
    var userId = User.RequireUserId();

    var metadata = await _fileService.GetFileMetadataAsync(userId, provider, fileId);

    return Ok(metadata);
  }

  /// <summary>
  /// Get the web view URL for a file (redirects to provider's web interface).
  /// </summary>
  [HttpGet("{provider}/{fileId}/view")]
  public async Task<ActionResult> GetFileViewUrl(
      ProviderType provider,
      string fileId)
  {
    var userId = User.RequireUserId();

    var viewUrl = await _fileService.GetFileViewUrlAsync(userId, provider, fileId);

    return Redirect(viewUrl.ToString());
  }
}

/// <summary>
/// Response DTO for paginated file lists.
/// </summary>
/// <param name="Items">The list of files.</param>
/// <param name="NextPageToken">Token for retrieving the next page of results.</param>
public sealed record FileListResponse(
    IReadOnlyList<FileItem> Items,
    string? NextPageToken
);
