using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using StorageConnector.Application;
using StorageConnector.Application.Interfaces;
using StorageConnector.Domain;
using StorageConnector.Infrastructure.Data;

namespace StorageConnector.Infrastructure;

public sealed class EfTokenStore : ITokenStore {
    private readonly AppDbContext _db; private readonly IDataProtector _p;
    public EfTokenStore(AppDbContext db, IDataProtectionProvider dp) { _db = db; _p = dp.CreateProtector("tokens.v1"); }

    public Task<ProviderAccount?> GetAsync(string userId, ProviderType provider) =>
        _db.Set<ProviderAccount>().FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider)!;

    public async Task UpsertAsync(ProviderAccount account) { _db.Update(account); await _db.SaveChangesAsync(); }
    public async Task DeleteAsync(string userId, ProviderType provider) {
        var a = await GetAsync(userId, provider); if (a != null) { _db.Remove(a); await _db.SaveChangesAsync(); }
    }
    public string Encrypt(string plaintext) => _p.Protect(plaintext);
    public string Decrypt(string ciphertext) => _p.Unprotect(ciphertext);
}