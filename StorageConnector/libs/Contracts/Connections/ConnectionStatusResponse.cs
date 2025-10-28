using Domain;

namespace Contracts.Connections;

public sealed record ConnectionStatusResponse(ProviderType Provider, bool IsLinked, string[] Scopes);
