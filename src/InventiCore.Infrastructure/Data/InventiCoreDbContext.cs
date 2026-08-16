using InventiCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventiCore.Infrastructure.Data;

public class InventiCoreDbContext : DbContext
{
    public InventiCoreDbContext(DbContextOptions<InventiCoreDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas as configurações de entidade (IEntityTypeConfiguration)
        // encontradas no assembly atual automaticamente.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventiCoreDbContext).Assembly);
    }

    /// <summary>
    /// Override para preencher automaticamente CreatedAt/UpdatedAt em todas as entidades.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
