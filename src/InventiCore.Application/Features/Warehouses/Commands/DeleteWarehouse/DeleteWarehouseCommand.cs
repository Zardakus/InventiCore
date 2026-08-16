using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Warehouses.Commands.DeleteWarehouse;

public record DeleteWarehouseCommand(Guid Id) : IRequest<Unit>;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWarehouseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse com Id '{request.Id}' não encontrado.");

        // Soft delete
        warehouse.IsActive = false;

        _unitOfWork.Warehouses.Update(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
