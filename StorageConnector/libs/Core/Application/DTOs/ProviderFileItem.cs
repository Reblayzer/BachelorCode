using Domain;

namespace Application.DTOs;

/// <summary>
/// File item with provider information for aggregated views.
/// </summary>
public sealed record ProviderFileItem(
    string Id,
    string Name,
    string? MimeType,
    long? SizeBytes,
    DateTimeOffset ModifiedUtc,
    ProviderType Provider
);
