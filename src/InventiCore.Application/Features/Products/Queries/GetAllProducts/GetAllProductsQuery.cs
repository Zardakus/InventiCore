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
public record GetAllProductsQuery(Guid TenantId) : IRequest<IEnumerable<ProductDto>>;

// ── Handler ──────────────────────────────────────────────────
public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllProductsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _unitOfWork.Products
            .GetByTenantAsync(request.TenantId, cancellationToken);

        return ProductMapper.ToDtoList(products);
    }
}
