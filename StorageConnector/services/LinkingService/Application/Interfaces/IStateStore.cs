using LinkingService.Domain;

namespace LinkingService.Application.Interfaces;

public interface IStateStore
{
    Task SaveAsync(string state, Guid userId, string codeVerifier, ProviderType provider, TimeSpan ttl);
    Task<(Guid userId, string codeVerifier, ProviderType provider)?> TakeAsync(string state);
}
