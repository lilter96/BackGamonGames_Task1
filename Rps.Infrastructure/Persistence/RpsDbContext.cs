using Microsoft.EntityFrameworkCore;
using Rps.Domain.Entities;

namespace Rps.Infrastructure.Persistence;

public class RpsDbContext(DbContextOptions<RpsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<MatchHistory> MatchHistories => Set<MatchHistory>();
    public DbSet<GameTransaction> GameTransactions => Set<GameTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedOnAdd();
            entity.Property(u => u.Username).IsRequired();
        });

        modelBuilder.Entity<MatchHistory>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(u => u.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<GameTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(u => u.Id).ValueGeneratedOnAdd();
        });
    }
}
