using StorageConnector.Domain;

namespace StorageConnector.Contracts.Connections;

public sealed record ConnectionStatusResponse(ProviderType Provider, bool IsLinked, string[] Scopes);
