using Application.DTOs;
using Application.Interfaces;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class FileService : IFileService
{
  private readonly ITokenStore _tokenStore;
  private readonly IFileProviderFactory _fileProviderFactory;
  private readonly ILogger<FileService> _logger;

  public FileService(
      ITokenStore tokenStore,
      IFileProviderFactory fileProviderFactory,
      ILogger<FileService> logger)
  {
    _tokenStore = tokenStore;
    _fileProviderFactory = fileProviderFactory;
    _logger = logger;
  }

  public async Task<IReadOnlyList<ProviderFileItem>> GetFilesFromAllProvidersAsync(
      string userId, int pageSize = 50)
  {
    // Get all linked accounts for the user
    var linkedAccounts = await _tokenStore.GetAllByUserAsync(userId);

    if (!linkedAccounts.Any())
    {
      _logger.LogInformation("User {UserId} has no linked accounts", userId);
      return Array.Empty<ProviderFileItem>();
    }

    // Fetch files from each provider in parallel
    var tasks = linkedAccounts.Select(async account =>
    {
      try
      {
        if (!_fileProviderFactory.TryGet(account.Provider, out var provider) || provider is null)
        {
          _logger.LogWarning("No file provider found for {Provider}", account.Provider);
          return Enumerable.Empty<ProviderFileItem>();
        }

        var (items, _) = await provider.ListAsync(userId, folderId: null, pageSize, pageToken: null);

        // Map to ProviderFileItem to include provider info
        return items.Select(item => new ProviderFileItem(
                Id: item.Id,
                Name: item.Name,
                MimeType: item.MimeType,
                SizeBytes: item.SizeBytes,
                ModifiedUtc: item.ModifiedUtc,
                Provider: account.Provider
            ));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to fetch files from {Provider} for user {UserId}",
                account.Provider, userId);
        // Return empty on error to avoid breaking the entire aggregation
        return Enumerable.Empty<ProviderFileItem>();
      }
    });

    var results = await Task.WhenAll(tasks);

    // Flatten and sort by modified date (newest first)
    var allFiles = results
        .SelectMany(files => files)
        .OrderByDescending(f => f.ModifiedUtc)
        .ToList();

    _logger.LogInformation("Aggregated {Count} files from {ProviderCount} providers for user {UserId}",
        allFiles.Count, linkedAccounts.Count, userId);

    return allFiles;
  }

  public async Task<(IReadOnlyList<FileItem> items, string? nextPageToken)> GetFilesByProviderAsync(
      string userId, ProviderType provider, string? folderId = null,
      int pageSize = 50, string? pageToken = null)
  {
    var fileProvider = _fileProviderFactory.Get(provider);
    return await fileProvider.ListAsync(userId, folderId, pageSize, pageToken);
  }

  public async Task<FileMetadata> GetFileMetadataAsync(string userId, ProviderType provider, string fileId)
  {
    var fileProvider = _fileProviderFactory.Get(provider);
    return await fileProvider.GetMetadataAsync(userId, fileId);
  }

  public async Task<Uri> GetFileViewUrlAsync(string userId, ProviderType provider, string fileId)
  {
    var fileProvider = _fileProviderFactory.Get(provider);
    return await fileProvider.GetViewUrlAsync(userId, fileId);
  }
}
