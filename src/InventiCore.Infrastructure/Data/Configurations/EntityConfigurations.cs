using InventiCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventiCore.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Document).IsRequired().HasMaxLength(20);
        builder.HasIndex(t => t.Document).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.SellingPrice).HasColumnType("decimal(18,2)");

        // SKU deve ser único por Tenant
        builder.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();

        builder.HasOne(p => p.Tenant)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Location).HasMaxLength(500);

        builder.HasOne(w => w.Tenant)
            .WithMany(t => t.Warehouses)
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.HasKey(si => si.Id);

        // Concorrência Otimista com xmin (PostgreSQL system column)
        builder.Property(si => si.RowVersion)
            .IsRowVersion();

        // Um produto só pode existir uma vez por warehouse
        builder.HasIndex(si => new { si.ProductId, si.WarehouseId }).IsUnique();

        builder.HasOne(si => si.Product)
            .WithMany(p => p.StockItems)
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Warehouse)
            .WithMany(w => w.StockItems)
            .HasForeignKey(si => si.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(sm => sm.Id);
        builder.Property(sm => sm.Reason).HasMaxLength(500);
        builder.Property(sm => sm.PerformedBy).HasMaxLength(200);

        builder.HasOne(sm => sm.StockItem)
            .WithMany(si => si.StockMovements)
            .HasForeignKey(sm => sm.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.SourceWarehouse)
            .WithMany()
            .HasForeignKey(sm => sm.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(sm => sm.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
