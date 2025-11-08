namespace LinkingService.Application.DTOs;

public sealed record FileItem(string Id, string Name, string? MimeType, long? SizeBytes, DateTimeOffset ModifiedUtc);
