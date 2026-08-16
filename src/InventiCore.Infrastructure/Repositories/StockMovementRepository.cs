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
            .AsNoTracking()
            .Where(sm => sm.StockItemId == stockItemId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
