using System;

namespace StorageConnector.Api.Contracts.Connections;

public sealed class ConnectionStatusResponse
{
    public string Provider { get; init; } = default!;   // "Google" / "Microsoft"
    public bool Connected { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
}