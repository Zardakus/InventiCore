using InventiCore.Domain.Entities;

namespace InventiCore.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySkuAsync(string sku, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<IEnumerable<Warehouse>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IStockItemRepository : IRepository<StockItem>
{
    Task<StockItem?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockItem>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockItem>> GetLowStockItemsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<IEnumerable<StockMovement>> GetByStockItemAsync(Guid stockItemId, CancellationToken cancellationToken = default);
}
