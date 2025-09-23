namespace StorageConnector.Application.DTOs;

public sealed record FileItem(string Id, string Name, string? MimeType, DateTimeOffset ModifiedUtc);