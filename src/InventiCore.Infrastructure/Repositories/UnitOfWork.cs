using InventiCore.Domain.Interfaces;
using InventiCore.Infrastructure.Data;

namespace InventiCore.Infrastructure.Repositories;

/// <summary>
/// Unit of Work — coordena múltiplos repositórios sob uma única transação.
/// Garante atomicidade: ou todas as operações são salvas, ou nenhuma é.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly InventiCoreDbContext _context;

    private ITenantRepository? _tenants;
    private IProductRepository? _products;
    private IWarehouseRepository? _warehouses;
    private IStockItemRepository? _stockItems;
    private IStockMovementRepository? _stockMovements;

    public UnitOfWork(InventiCoreDbContext context)
    {
        _context = context;
    }

    // Lazy initialization — repositórios são criados sob demanda
    public ITenantRepository Tenants =>
        _tenants ??= new TenantRepository(_context);

    public IProductRepository Products =>
        _products ??= new ProductRepository(_context);

    public IWarehouseRepository Warehouses =>
        _warehouses ??= new WarehouseRepository(_context);

    public IStockItemRepository StockItems =>
        _stockItems ??= new StockItemRepository(_context);

    public IStockMovementRepository StockMovements =>
        _stockMovements ??= new StockMovementRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
