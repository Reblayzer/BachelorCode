using StorageConnector.Domain;

namespace StorageConnector.Application;

public interface IStateStore {
    Task SaveAsync(string state, string codeVerifier, ProviderType provider, TimeSpan ttl);
    Task<(string codeVerifier, ProviderType provider)?> TakeAsync(string state);
}
