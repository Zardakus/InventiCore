namespace InventiCore.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern - garante que múltiplas operações de repositório
/// sejam commitadas numa única transação atômica.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ITenantRepository Tenants { get; }
    IProductRepository Products { get; }
    IWarehouseRepository Warehouses { get; }
    IStockItemRepository StockItems { get; }
    IStockMovementRepository StockMovements { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
