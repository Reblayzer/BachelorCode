using Domain;

namespace Application.Interfaces;

public interface ITokenStore
{
    Task<ProviderAccount?> GetAsync(Guid userId, ProviderType provider);
    Task<IReadOnlyList<ProviderAccount>> GetAllByUserAsync(Guid userId);
    Task UpsertAsync(ProviderAccount account);
    Task DeleteAsync(Guid userId, ProviderType provider);
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
