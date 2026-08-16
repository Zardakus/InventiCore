using InventiCore.Domain.Interfaces;
using MediatR;
using InventiCore.Application.Common.Interfaces;

namespace InventiCore.Application.Features.Stock.Queries.AnalyzeLowStock;

public record LowStockAnalysisResult
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int MinimumStock { get; init; }
    public int CurrentTotalStock { get; init; }
    public List<WarehouseAvailability> AvailableInOtherWarehouses { get; init; } = new();
}

public record WarehouseAvailability(Guid WarehouseId, string WarehouseName, int AvailableQuantity);

public record AnalyzeLowStockQuery() : IRequest<List<LowStockAnalysisResult>>;

public class AnalyzeLowStockQueryHandler : IRequestHandler<AnalyzeLowStockQuery, List<LowStockAnalysisResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AnalyzeLowStockQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<LowStockAnalysisResult>> Handle(AnalyzeLowStockQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedAccessException("TenantId não encontrado.");

        var lowStockItems = await _unitOfWork.StockItems.GetLowStockItemsAsync(tenantId, cancellationToken);
        var warehouses = await _unitOfWork.Warehouses.GetByTenantAsync(tenantId, cancellationToken);
        
        // Obter todos os estoques desse tenant para cruzar dados
        var allProductsIds = lowStockItems.Select(s => s.ProductId).Distinct().ToList();
        var results = new List<LowStockAnalysisResult>();

        foreach (var productId in allProductsIds)
        {
            var productLowStockItem = lowStockItems.First(s => s.ProductId == productId);
            var product = productLowStockItem.Product;

            // Busca onde mais tem esse produto usando os repositórios adequados ou LINQ se tivéssemos.
            // Para simplificar a POC, vamos analisar pelo repositório base.
            // Aqui eu vou simular a verificação em outros warehouses.
            var availableElsewhere = new List<WarehouseAvailability>();

            results.Add(new LowStockAnalysisResult
            {
                ProductId = productId,
                ProductName = product?.Name ?? "Desconhecido",
                Sku = product?.Sku ?? "Desconhecido",
                MinimumStock = product?.MinimumStock ?? 0,
                CurrentTotalStock = productLowStockItem.Quantity,
                AvailableInOtherWarehouses = availableElsewhere
            });
        }

        return results;
    }
}
