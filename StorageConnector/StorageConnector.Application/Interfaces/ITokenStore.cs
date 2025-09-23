using StorageConnector.Domain;

namespace StorageConnector.Application.Interfaces;

public interface ITokenStore {
    Task<ProviderAccount?> GetAsync(string userId, ProviderType provider);
    Task UpsertAsync(ProviderAccount account);
    Task DeleteAsync(string userId, ProviderType provider);
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}