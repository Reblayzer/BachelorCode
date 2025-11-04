using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs;
using Application.Interfaces;
using Domain;
using Microsoft.Extensions.Logging;

namespace Infrastructure.FileProviders;

public sealed class GoogleFileProvider : IFileProvider
{
  private const string DriveApiBaseUrl = "https://www.googleapis.com/drive/v3";
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private readonly HttpClient _http;
  private readonly ITokenStore _tokenStore;
  private readonly IEnumerable<IOAuthClient> _oauthClients;
  private readonly ILogger<GoogleFileProvider> _logger;

  public GoogleFileProvider(
      HttpClient http,
      ITokenStore tokenStore,
      IEnumerable<IOAuthClient> oauthClients,
      ILogger<GoogleFileProvider> logger)
  {
    _http = http;
    _tokenStore = tokenStore;
    _oauthClients = oauthClients;
    _logger = logger;
  }

  public ProviderType Provider => ProviderType.Google;

  public async Task<(IReadOnlyList<FileItem> items, string? nextPageToken)> ListAsync(
      string userId, string? folderId, int pageSize, string? pageToken)
  {
    var accessToken = await GetAccessTokenAsync(userId);

    // Build query: list files in folder or root
    var query = string.IsNullOrWhiteSpace(folderId)
        ? "trashed = false"
        : $"'{folderId}' in parents and trashed = false";

    var requestUrl = $"{DriveApiBaseUrl}/files?" +
        $"q={Uri.EscapeDataString(query)}&" +
        $"fields=nextPageToken,files(id,name,mimeType,modifiedTime)&" +
        $"pageSize={pageSize}&" +
        $"orderBy=modifiedTime desc";

    if (!string.IsNullOrWhiteSpace(pageToken))
    {
      requestUrl += $"&pageToken={Uri.EscapeDataString(pageToken)}";
    }

    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await _http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync();
      _logger.LogError("Google Drive API error: {StatusCode} {Body}", response.StatusCode, errorBody);
      throw new HttpRequestException($"Google Drive API returned {response.StatusCode}");
    }

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<GoogleFileListResponse>(json, JsonOptions);

    var items = data?.Files?.Select(f => new FileItem(
        Id: f.Id,
        Name: f.Name,
        MimeType: f.MimeType,
        ModifiedUtc: f.ModifiedTime
    )).ToList() ?? new List<FileItem>();

    return (items, data?.NextPageToken);
  }

  public async Task<FileMetadata> GetMetadataAsync(string userId, string fileId)
  {
    var accessToken = await GetAccessTokenAsync(userId);

    var requestUrl = $"{DriveApiBaseUrl}/files/{fileId}?" +
        "fields=id,name,mimeType,size,modifiedTime,webViewLink";

    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await _http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync();
      _logger.LogError("Google Drive API error: {StatusCode} {Body}", response.StatusCode, errorBody);
      throw new HttpRequestException($"Google Drive API returned {response.StatusCode}");
    }

    var json = await response.Content.ReadAsStringAsync();
    var file = JsonSerializer.Deserialize<GoogleFile>(json, JsonOptions);

    if (file is null)
    {
      throw new InvalidOperationException("Failed to deserialize Google Drive file metadata");
    }

    return new FileMetadata(
        Id: file.Id,
        Name: file.Name,
        MimeType: file.MimeType,
        SizeBytes: file.Size,
        ModifiedUtc: file.ModifiedTime,
        ProviderSpecificJson: json
    );
  }

  public async Task<Uri> GetViewUrlAsync(string userId, string fileId)
  {
    var accessToken = await GetAccessTokenAsync(userId);

    var requestUrl = $"{DriveApiBaseUrl}/files/{fileId}?fields=webViewLink";

    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await _http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync();
      _logger.LogError("Google Drive API error: {StatusCode} {Body}", response.StatusCode, errorBody);
      throw new HttpRequestException($"Google Drive API returned {response.StatusCode}");
    }

    var json = await response.Content.ReadAsStringAsync();
    var file = JsonSerializer.Deserialize<GoogleFile>(json, JsonOptions);

    if (file?.WebViewLink is null)
    {
      throw new InvalidOperationException("Failed to get web view link from Google Drive");
    }

    return new Uri(file.WebViewLink);
  }

  private async Task<string> GetAccessTokenAsync(string userId)
  {
    var account = await _tokenStore.GetAsync(userId, ProviderType.Google);
    if (account is null)
    {
      throw new InvalidOperationException($"No Google account linked for user {userId}");
    }

    var oauthClient = _oauthClients.FirstOrDefault(c => c.Provider == ProviderType.Google)
        ?? throw new InvalidOperationException("Google OAuth client not registered");

    // Check if token is expired (with 5 min buffer)
    if (account.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(5))
    {
      _logger.LogInformation("Access token expired, refreshing for user {UserId}", userId);

      // Decrypt refresh token and get new access token
      var refreshToken = _tokenStore.Decrypt(account.EncryptedRefreshToken);
      var refreshed = await oauthClient.RefreshAsync(refreshToken);

      // Update the stored account with new token data
      account.UpdateFrom(refreshed, _tokenStore.Encrypt);
      await _tokenStore.UpsertAsync(account);

      return refreshed.AccessToken;
    }

    // Token not expired - still need to refresh to get access token since we don't store it
    var decryptedRefreshToken = _tokenStore.Decrypt(account.EncryptedRefreshToken);
    var tokenResponse = await oauthClient.RefreshAsync(decryptedRefreshToken);

    return tokenResponse.AccessToken;
  }

  // DTOs for Google Drive API responses
  private sealed record GoogleFileListResponse(
      [property: JsonPropertyName("nextPageToken")] string? NextPageToken,
      [property: JsonPropertyName("files")] List<GoogleFile>? Files
  );

  private sealed record GoogleFile(
      [property: JsonPropertyName("id")] string Id,
      [property: JsonPropertyName("name")] string Name,
      [property: JsonPropertyName("mimeType")] string? MimeType,
      [property: JsonPropertyName("size")] long? Size,
      [property: JsonPropertyName("modifiedTime")] DateTimeOffset ModifiedTime,
      [property: JsonPropertyName("webViewLink")] string? WebViewLink
  );
}
