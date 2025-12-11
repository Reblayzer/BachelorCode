namespace LinkingService.Api.DTOs;

/// <summary>
/// Response DTO for file view URL.
/// </summary>
/// <param name="Url">The URL to view the file in the provider's web interface.</param>
public sealed record FileViewUrlResponse(string Url);
