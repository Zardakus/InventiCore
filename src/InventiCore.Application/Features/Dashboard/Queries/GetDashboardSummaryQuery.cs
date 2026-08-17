using System.Text.Json;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace InventiCore.Application.Features.Dashboard.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDistributedCache _cache;

    public GetDashboardSummaryQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedAccessException("TenantId não encontrado.");

        var cacheKey = $"DashboardSummary_{tenantId}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<DashboardSummaryDto>(cachedData)!;
        }

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

        var result = new DashboardSummaryDto
        {
            TotalProducts = products.Count(),
            LowStockItems = lowStock.Select(s => s.ProductId).Distinct().Count(),
            StockPerWarehouse = stockPerWarehouse
        };

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions, cancellationToken);

        return result;
    }
}
