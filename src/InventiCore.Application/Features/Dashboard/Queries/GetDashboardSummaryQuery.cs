using InventiCore.Application.Common.Interfaces;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Dashboard.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardSummaryQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedAccessException("TenantId não encontrado.");

        var products = await _unitOfWork.Products.GetByTenantAsync(tenantId, cancellationToken);
        var lowStock = await _unitOfWork.StockItems.GetLowStockItemsAsync(tenantId, cancellationToken);
        var warehouses = await _unitOfWork.Warehouses.GetByTenantAsync(tenantId, cancellationToken);

        var stockPerWarehouse = new List<WarehouseStockSummaryDto>();

        foreach (var warehouse in warehouses)
        {
            var items = await _unitOfWork.StockItems.GetByWarehouseAsync(warehouse.Id, cancellationToken);
            stockPerWarehouse.Add(new WarehouseStockSummaryDto
            {
                WarehouseName = warehouse.Name,
                TotalQuantity = items.Sum(i => i.Quantity)
            });
        }

        return new DashboardSummaryDto
        {
            TotalProducts = products.Count(),
            LowStockItems = lowStock.Select(s => s.ProductId).Distinct().Count(),
            StockPerWarehouse = stockPerWarehouse
        };
    }
}
