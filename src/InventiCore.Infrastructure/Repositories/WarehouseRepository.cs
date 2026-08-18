using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using InventiCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventiCore.Infrastructure.Repositories;

public class WarehouseRepository : Repository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(InventiCoreDbContext context) : base(context) { }

    public async Task<IEnumerable<Warehouse>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }
}
