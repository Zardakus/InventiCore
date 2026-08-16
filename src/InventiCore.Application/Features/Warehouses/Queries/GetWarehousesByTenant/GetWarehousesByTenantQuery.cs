using InventiCore.Application.Common.Interfaces;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Warehouses.Queries.GetWarehousesByTenant;

public record GetWarehousesByTenantQuery() : IRequest<IEnumerable<WarehouseDto>>;

public class GetWarehousesByTenantQueryHandler : IRequestHandler<GetWarehousesByTenantQuery, IEnumerable<WarehouseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetWarehousesByTenantQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<WarehouseDto>> Handle(GetWarehousesByTenantQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId ?? throw new UnauthorizedAccessException("TenantId obrigatório.");
        var warehouses = await _unitOfWork.Warehouses.GetByTenantAsync(tenantId, cancellationToken);
        return WarehouseMapper.ToDtoList(warehouses);
    }
}
