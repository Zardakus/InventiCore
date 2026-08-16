using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using InventiCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventiCore.Infrastructure.Repositories;

public class StockMovementRepository : Repository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(InventiCoreDbContext context) : base(context) { }

    public async Task<IEnumerable<StockMovement>> GetByStockItemAsync(Guid stockItemId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sm => sm.StockItemId == stockItemId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> GetMovementsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(sm => sm.StockItem)
                .ThenInclude(si => si.Product)
            .Include(sm => sm.StockItem)
                .ThenInclude(si => si.Warehouse)
            .Include(sm => sm.SourceWarehouse)
            .Include(sm => sm.DestinationWarehouse)
            .Where(sm => sm.StockItem.Product.TenantId == tenantId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
