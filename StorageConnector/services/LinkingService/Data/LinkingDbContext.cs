using Microsoft.EntityFrameworkCore;
using Domain;

namespace LinkingService.Data;

public sealed class LinkingDbContext : DbContext
{
  public DbSet<ProviderAccount> ProviderAccounts => Set<ProviderAccount>();

  public LinkingDbContext(DbContextOptions<LinkingDbContext> options) : base(options) { }

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<ProviderAccount>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.UserId).IsRequired();
      entity.Property(x => x.EncryptedRefreshToken).IsRequired();
      entity.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
    });
  }
}
