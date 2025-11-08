namespace LinkingService.Application.DTOs;

public sealed record FileMetadata(string Id, string Name, string? MimeType, long? SizeBytes,
    DateTimeOffset? ModifiedUtc, string? ProviderSpecificJson);
