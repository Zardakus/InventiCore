using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Tenants.Queries.GetAllTenants;

public record GetAllTenantsQuery() : IRequest<IEnumerable<TenantDto>>;

public class GetAllTenantsQueryHandler : IRequestHandler<GetAllTenantsQuery, IEnumerable<TenantDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllTenantsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TenantDto>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
        return TenantMapper.ToDtoList(tenants);
    }
}
