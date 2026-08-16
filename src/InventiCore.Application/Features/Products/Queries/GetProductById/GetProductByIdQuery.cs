using InventiCore.Application.DTOs;
using InventiCore.Application.Common.Mappings;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Products.Queries.GetProductById;

// ── Query ────────────────────────────────────────────────────
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

// ── Handler ──────────────────────────────────────────────────
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Produto com Id '{request.Id}' não encontrado.");

        return ProductMapper.ToDto(product);
    }
}
