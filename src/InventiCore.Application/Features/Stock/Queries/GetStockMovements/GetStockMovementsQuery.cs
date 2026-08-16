using InventiCore.Application.Common.Interfaces;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Stock.Queries.GetStockMovements;

public record GetStockMovementsQuery() : IRequest<IEnumerable<StockMovementHistoryDto>>;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, IEnumerable<StockMovementHistoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetStockMovementsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<StockMovementHistoryDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId ?? throw new UnauthorizedAccessException("TenantId obrigatório.");
        var movements = await _unitOfWork.StockMovements.GetMovementsByTenantAsync(tenantId, cancellationToken);

        return movements.Select(sm => new StockMovementHistoryDto
        {
            CreatedAt = sm.CreatedAt,
            MovementType = sm.Type.ToString(),
            Quantity = sm.Quantity,
            ProductName = sm.StockItem.Product.Name,
            WarehouseName = sm.StockItem.Warehouse.Name,
            Reason = sm.Reason,
            SourceWarehouseName = sm.SourceWarehouse?.Name,
            DestinationWarehouseName = sm.DestinationWarehouse?.Name
        });
    }
}
