using Microsoft.EntityFrameworkCore;
using IdentityService.Domain;

namespace IdentityService.Infrastructure.Data;

public sealed class IdentityDbContext : DbContext
{
  public DbSet<User> Users => Set<User>();
  public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();
  public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

  public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<User>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.Email).IsRequired().HasMaxLength(256);
      entity.HasIndex(x => x.Email).IsUnique();
      entity.Property(x => x.PasswordHash).IsRequired();
      entity.Property(x => x.EmailConfirmed).IsRequired();
      entity.Property(x => x.CreatedAt).IsRequired();
    });

    builder.Entity<EmailConfirmationToken>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.UserId).IsRequired();
      entity.Property(x => x.Token).IsRequired().HasMaxLength(500);
      entity.Property(x => x.ExpiresAt).IsRequired();
      entity.Property(x => x.IsUsed).IsRequired();
      entity.HasIndex(x => x.Token);
    });

    builder.Entity<PasswordResetToken>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.UserId).IsRequired();
      entity.Property(x => x.Token).IsRequired().HasMaxLength(500);
      entity.Property(x => x.ExpiresAt).IsRequired();
      entity.Property(x => x.IsUsed).IsRequired();
      entity.HasIndex(x => x.Token);
    });
  }
}
