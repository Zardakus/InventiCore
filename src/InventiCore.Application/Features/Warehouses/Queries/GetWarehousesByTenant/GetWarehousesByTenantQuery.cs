using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Warehouses.Queries.GetWarehousesByTenant;

public record GetWarehousesByTenantQuery(Guid TenantId) : IRequest<IEnumerable<WarehouseDto>>;

public class GetWarehousesByTenantQueryHandler : IRequestHandler<GetWarehousesByTenantQuery, IEnumerable<WarehouseDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWarehousesByTenantQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<WarehouseDto>> Handle(GetWarehousesByTenantQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await _unitOfWork.Warehouses.GetByTenantAsync(request.TenantId, cancellationToken);
        return WarehouseMapper.ToDtoList(warehouses);
    }
}
