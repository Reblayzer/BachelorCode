using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StorageConnector.Domain;

namespace StorageConnector.Infrastructure.Data;

public sealed class ApplicationUser : IdentityUser { }

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<ProviderAccount> ProviderAccounts => Set<ProviderAccount>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ProviderAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.EncryptedRefreshToken).IsRequired();
            e.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
        });
    }
}