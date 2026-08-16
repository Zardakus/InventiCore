using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using InventiCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventiCore.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(InventiCoreDbContext context) : base(context) { }

    public async Task<Product?> GetBySkuAsync(string sku, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku && p.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
