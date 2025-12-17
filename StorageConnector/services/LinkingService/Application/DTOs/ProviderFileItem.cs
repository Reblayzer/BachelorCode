using LinkingService.Domain;

namespace LinkingService.Application.DTOs;

// File item with provider information used in aggregated views.
public sealed record ProviderFileItem(
    string Id,
    string Name,
    string? MimeType,
    long? SizeBytes,
    DateTimeOffset ModifiedUtc,
    ProviderType Provider
);
