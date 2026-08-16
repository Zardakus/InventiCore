using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Products.Commands.DeleteProduct;

// ── Command ──────────────────────────────────────────────────
public record DeleteProductCommand(Guid Id) : IRequest<Unit>;

// ── Handler ──────────────────────────────────────────────────
/// <summary>
/// Soft Delete: desativa o produto ao invés de excluí-lo fisicamente.
/// Regra de negócio: entidades com IsActive devem ser desativadas.
/// </summary>
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Produto com Id '{request.Id}' não encontrado.");

        // Soft delete — desativa ao invés de excluir
        product.IsActive = false;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
