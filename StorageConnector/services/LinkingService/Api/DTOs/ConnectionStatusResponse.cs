using LinkingService.Domain;

namespace LinkingService.Api.DTOs;

public sealed record ConnectionStatusResponse(ProviderType Provider, bool IsLinked, string[] Scopes);
