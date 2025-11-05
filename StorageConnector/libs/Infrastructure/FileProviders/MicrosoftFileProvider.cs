using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs;
using Application.Interfaces;
using Domain;
using Microsoft.Extensions.Logging;

namespace Infrastructure.FileProviders;

public sealed class MicrosoftFileProvider : IFileProvider
{
  private const string GraphApiBaseUrl = "https://graph.microsoft.com/v1.0";
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private readonly HttpClient _http;
  private readonly ITokenStore _tokenStore;
  private readonly IEnumerable<IOAuthClient> _oauthClients;
  private readonly ILogger<MicrosoftFileProvider> _logger;

  public MicrosoftFileProvider(
      HttpClient http,
      ITokenStore tokenStore,
      IEnumerable<IOAuthClient> oauthClients,
      ILogger<MicrosoftFileProvider> logger)
  {
    _http = http;
    _tokenStore = tokenStore;
    _oauthClients = oauthClients;
    _logger = logger;
  }

  public ProviderType Provider => ProviderType.Microsoft;

  public async Task<(IReadOnlyList<FileItem> items, string? nextPageToken)> ListAsync(
      string userId, string? folderId, int pageSize, string? pageToken)
  {
    var accessToken = await GetAccessTokenAsync(userId);

    // Build URL: list items in folder or root
    var requestUrl = string.IsNullOrWhiteSpace(folderId)
        ? $"{GraphApiBaseUrl}/me/drive/root/children?$top={pageSize}&$orderby=lastModifiedDateTime desc"
        : $"{GraphApiBaseUrl}/me/drive/items/{folderId}/children?$top={pageSize}&$orderby=lastModifiedDateTime desc";

    // Microsoft Graph uses @odata.nextLink for pagination instead of pageToken
    if (!string.IsNullOrWhiteSpace(pageToken))
    {
      requestUrl = pageToken; // pageToken is the full nextLink URL
    }

    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await _http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync();
      _logger.LogError("Microsoft Graph API error: {StatusCode} {Body}", response.StatusCode, errorBody);
      throw new HttpRequestException($"Microsoft Graph API returned {response.StatusCode}");
    }

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<OneDriveItemListResponse>(json, JsonOptions);

    var items = data?.Value?.Select(item => new FileItem(
        Id: item.Id,
        Name: item.Name,
        MimeType: item.File?.MimeType, // null for folders
        SizeBytes: item.Size,
        ModifiedUtc: item.LastModifiedDateTime
    )).ToList() ?? new List<FileItem>();

    return (items, data?.ODataNextLink);
  }

  public async Task<FileMetadata> GetMetadataAsync(string userId, string fileId)
  {
    var accessToken = await GetAccessTokenAsync(userId);

    var requestUrl = $"{GraphApiBaseUrl}/me/drive/items/{fileId}";

    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await _http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync();
      _logger.LogError("Microsoft Graph API error: {StatusCode} {Body}", response.StatusCode, errorBody);
      throw new HttpRequestException($"Microsoft Graph API returned {response.StatusCode}");
    }

    var json = await response.Content.ReadAsStringAsync();
    var item = JsonSerializer.Deserialize<OneDriveItem>(json, JsonOptions);

    if (item is null)
    {
      throw new InvalidOperationException("Failed to deserialize OneDrive item metadata");
    }

    return new FileMetadata(
        Id: item.Id,
        Name: item.Name,
        MimeType: item.File?.MimeType,
        SizeBytes: item.Size,
        ModifiedUtc: item.LastModifiedDateTime,
        ProviderSpecificJson: json
    );
  }

  public async Task<Uri> GetViewUrlAsync(string userId, string fileId)
  {
    var accessToken = await GetAccessTokenAsync(userId);

    var requestUrl = $"{GraphApiBaseUrl}/me/drive/items/{fileId}";

    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await _http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync();
      _logger.LogError("Microsoft Graph API error: {StatusCode} {Body}", response.StatusCode, errorBody);
      throw new HttpRequestException($"Microsoft Graph API returned {response.StatusCode}");
    }

    var json = await response.Content.ReadAsStringAsync();
    var item = JsonSerializer.Deserialize<OneDriveItem>(json, JsonOptions);

    if (item?.WebUrl is null)
    {
      throw new InvalidOperationException("Failed to get web URL from OneDrive item");
    }

    return new Uri(item.WebUrl);
  }

  private async Task<string> GetAccessTokenAsync(string userId)
  {
    var account = await _tokenStore.GetAsync(userId, ProviderType.Microsoft);
    if (account is null)
    {
      throw new InvalidOperationException($"No Microsoft account linked for user {userId}");
    }

    var oauthClient = _oauthClients.FirstOrDefault(c => c.Provider == ProviderType.Microsoft)
        ?? throw new InvalidOperationException("Microsoft OAuth client not registered");

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

  // DTOs for Microsoft Graph API responses
  private sealed record OneDriveItemListResponse(
      [property: JsonPropertyName("value")] List<OneDriveItem>? Value,
      [property: JsonPropertyName("@odata.nextLink")] string? ODataNextLink
  );

  private sealed record OneDriveItem(
      [property: JsonPropertyName("id")] string Id,
      [property: JsonPropertyName("name")] string Name,
      [property: JsonPropertyName("size")] long? Size,
      [property: JsonPropertyName("lastModifiedDateTime")] DateTimeOffset LastModifiedDateTime,
      [property: JsonPropertyName("webUrl")] string? WebUrl,
      [property: JsonPropertyName("file")] OneDriveFile? File
  );

  private sealed record OneDriveFile(
      [property: JsonPropertyName("mimeType")] string? MimeType
  );
}
