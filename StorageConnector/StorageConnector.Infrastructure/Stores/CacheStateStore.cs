using Microsoft.Extensions.Caching.Memory;
using StorageConnector.Application;
using StorageConnector.Domain;

namespace StorageConnector.Infrastructure;

public sealed class CacheStateStore : IStateStore {
    private readonly IMemoryCache _cache; public CacheStateStore(IMemoryCache c) => _cache = c;
    public Task SaveAsync(string state, string cv, ProviderType p, TimeSpan ttl) { _cache.Set(state, (cv, p), ttl); return Task.CompletedTask; }
    public Task<(string codeVerifier, ProviderType provider)?> TakeAsync(string state) {
        if (_cache.TryGetValue<(string, ProviderType)>(state, out var v)) { _cache.Remove(state); return Task.FromResult<(string, ProviderType)?>(v); }
        return Task.FromResult<(string, ProviderType)?>(null);
    }
}
