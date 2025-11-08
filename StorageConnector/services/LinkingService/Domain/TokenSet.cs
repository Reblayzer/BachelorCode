namespace LinkingService.Domain;

public sealed record TokenSet(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc, string[] Scopes);
