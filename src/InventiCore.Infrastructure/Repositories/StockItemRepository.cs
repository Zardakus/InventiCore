using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using InventiCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventiCore.Infrastructure.Repositories;

public class StockItemRepository : Repository<StockItem>, IStockItemRepository
{
    public StockItemRepository(InventiCoreDbContext context) : base(context) { }

    public async Task<StockItem?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(si => si.ProductId == productId && si.WarehouseId == warehouseId, cancellationToken);
    }

    public async Task<IEnumerable<StockItem>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(si => si.Product)
            .Where(si => si.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockItem>> GetLowStockItemsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(si => si.Product)
            .Include(si => si.Warehouse)
            .Where(si => si.Product.TenantId == tenantId && si.Quantity <= si.Product.MinimumStock)
            .ToListAsync(cancellationToken);
    }
}
