using Domain;

namespace Application.Interfaces;

public interface ITokenStore
{
    Task<ProviderAccount?> GetAsync(string userId, ProviderType provider);
    Task<IReadOnlyList<ProviderAccount>> GetAllByUserAsync(string userId);
    Task UpsertAsync(ProviderAccount account);
    Task DeleteAsync(string userId, ProviderType provider);
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
