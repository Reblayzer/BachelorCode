using Microsoft.Extensions.Caching.Memory;
using LinkingService.Application.Interfaces;
using LinkingService.Domain;

namespace LinkingService.Infrastructure.Stores;

public sealed class CacheStateStore : IStateStore
{
    private readonly IMemoryCache _cache; public CacheStateStore(IMemoryCache c) => _cache = c;
    public Task SaveAsync(string state, Guid userId, string cv, ProviderType p, TimeSpan ttl) { _cache.Set(state, (userId, cv, p), ttl); return Task.CompletedTask; }
    public Task<(Guid userId, string codeVerifier, ProviderType provider)?> TakeAsync(string state)
    {
        if (_cache.TryGetValue<(Guid, string, ProviderType)>(state, out var v)) { _cache.Remove(state); return Task.FromResult<(Guid, string, ProviderType)?>(v); }
        return Task.FromResult<(Guid, string, ProviderType)?>(null);
    }
}
