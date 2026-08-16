using FluentValidation;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Enums;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Stock.Commands.AddStock;

public record AddStockCommand(Guid ProductId, Guid WarehouseId, int Quantity, string? Reason, string? PerformedBy) : IRequest<StockMovementDto>;

public class AddStockCommandValidator : AbstractValidator<AddStockCommand>
{
    public AddStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId é obrigatório.");
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("WarehouseId é obrigatório.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("A quantidade para entrada deve ser maior que zero.");
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.PerformedBy).MaximumLength(200);
    }
}

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, StockMovementDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddStockCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StockMovementDto> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Produto {request.ProductId} não encontrado.");

        var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse {request.WarehouseId} não encontrado.");

        if (product.TenantId != warehouse.TenantId)
            throw new InvalidOperationException("Produto e Warehouse não pertencem ao mesmo Tenant.");

        var stockItem = await _unitOfWork.StockItems.GetByProductAndWarehouseAsync(request.ProductId, request.WarehouseId, cancellationToken);
        
        // Se o produto ainda não tem controle de estoque neste depósito, cria o StockItem
        if (stockItem is null)
        {
            stockItem = new StockItem
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                Quantity = 0
            };
            await _unitOfWork.StockItems.AddAsync(stockItem, cancellationToken);
        }

        // Transação: Atualiza quantidade e grava o log imutável de movimentação
        stockItem.Quantity += request.Quantity;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            StockItemId = stockItem.Id,
            Type = MovementType.Entry,
            Quantity = request.Quantity,
            Reason = request.Reason,
            PerformedBy = request.PerformedBy,
            DestinationWarehouseId = request.WarehouseId
        };
        await _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StockMapper.ToDto(movement);
    }
}
