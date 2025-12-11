using Asp.Versioning;
using LinkingService.Api.DTOs;
using LinkingService.Application.DTOs;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;
using LinkingService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkingService.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/files")]
[ApiVersion("1.0")]
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

  /// <summary>
  /// Get the web view URL for a file as JSON (for frontend to open in new tab).
  /// </summary>
  [HttpGet("{provider}/{fileId}/view-url")]
  public async Task<ActionResult<FileViewUrlResponse>> GetFileViewUrlJson(
      ProviderType provider,
      string fileId)
  {
    var userId = User.RequireUserId();

    var viewUrl = await _fileService.GetFileViewUrlAsync(userId, provider, fileId);

    return Ok(new FileViewUrlResponse(viewUrl.ToString()));
  }
}
