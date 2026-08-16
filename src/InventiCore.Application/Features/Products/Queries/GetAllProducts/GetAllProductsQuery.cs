using InventiCore.Application.DTOs;
using InventiCore.Application.Common.Mappings;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Products.Queries.GetAllProducts;

// ── Query ────────────────────────────────────────────────────
/// <summary>
/// Retorna todos os produtos de um Tenant específico.
/// Regra de negócio: queries devem sempre filtrar por TenantId para garantir isolamento.
/// </summary>
public record GetAllProductsQuery() : IRequest<IEnumerable<ProductDto>>;

// ── Handler ──────────────────────────────────────────────────
public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventiCore.Application.Common.Interfaces.ICurrentUserService _currentUserService;

    public GetAllProductsQueryHandler(IUnitOfWork unitOfWork, InventiCore.Application.Common.Interfaces.ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant não identificado no token.");

        var products = await _unitOfWork.Products
            .GetByTenantAsync(tenantId, cancellationToken);

        return ProductMapper.ToDtoList(products);
    }
}
