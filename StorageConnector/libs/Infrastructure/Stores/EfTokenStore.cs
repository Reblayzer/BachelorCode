using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Application;
using Application.Interfaces;
using Domain;

namespace Infrastructure;

public sealed class EfTokenStore : ITokenStore
{
    private readonly DbContext _db; private readonly IDataProtector _p;
    public EfTokenStore(DbContext db, IDataProtectionProvider dp) { _db = db; _p = dp.CreateProtector("tokens.v1"); }

    public Task<ProviderAccount?> GetAsync(Guid userId, ProviderType provider) =>
        _db.Set<ProviderAccount>().FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider)!;

    public async Task<IReadOnlyList<ProviderAccount>> GetAllByUserAsync(Guid userId) =>
        await _db.Set<ProviderAccount>().Where(x => x.UserId == userId).ToListAsync();

    public async Task UpsertAsync(ProviderAccount account)
    {
        var set = _db.Set<ProviderAccount>();

        // Try to find an existing account for the same user+provider.
        var existing = await set.FirstOrDefaultAsync(x => x.UserId == account.UserId && x.Provider == account.Provider);
        if (existing is null)
        {
            // New account -> add
            await set.AddAsync(account);
        }
        else
        {
            // Existing account -> copy values from the provided instance onto the tracked entity.
            // Using CurrentValues.SetValues updates the tracked entity's values without attempting
            // to issue an UPDATE that targets a non-existent primary key (which can cause
            // DbUpdateConcurrencyException when caller passed a new Guid id).
            _db.Entry(existing).CurrentValues.SetValues(account);
        }

        await _db.SaveChangesAsync();
    }
    public async Task DeleteAsync(Guid userId, ProviderType provider)
    {
        var a = await GetAsync(userId, provider); if (a != null) { _db.Remove(a); await _db.SaveChangesAsync(); }
    }
    public string Encrypt(string plaintext) => _p.Protect(plaintext);
    public string Decrypt(string ciphertext) => _p.Unprotect(ciphertext);
}
