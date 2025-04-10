using GameAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameAPI.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Token> Tokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Accounts_Role", "Role IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)");
                    tb.HasCheckConstraint("CK_Accounts_MCoins", "MCoins >= 0");
                    tb.HasCheckConstraint("CK_Accounts_MaxCharCount", "MaxCharCount >= 1 AND MaxCharCount <= 10");
                });

                entity.HasIndex(a => a.Email).IsUnique();
                entity.Property(a => a.Email)
                    .IsRequired()
                    .HasMaxLength(50)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);
                entity.HasIndex(a => a.TgId).IsUnique();

                entity.Property(a => a.Role).HasDefaultValue(1);
                entity.Property(a => a.Banned).HasDefaultValue(false);
                entity.Property(a => a.MCoins).HasDefaultValue(0);
                entity.Property(a => a.MaxCharCount).HasDefaultValue(2);
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(a => a.FailedLoginAttempts).HasDefaultValue(0);
            });

            modelBuilder.Entity<Token>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Tokens_Expiry", "ExpiredAt > GETUTCDATE()");
                });

                entity.HasKey(t => t.TgId);
                entity.Property(t => t.ExpiredAt).IsRequired();
                entity.Property(t => t.TgToken).IsRequired().HasMaxLength(5);
                entity.Property(t => t.IsUsed).HasDefaultValue(false);
            });
        }
    }
}