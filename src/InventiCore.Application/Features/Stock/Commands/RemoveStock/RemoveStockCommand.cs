using FluentValidation;
using InventiCore.Application.Common.Events;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Enums;
using InventiCore.Domain.Exceptions;
using InventiCore.Domain.Interfaces;
using MassTransit;
using MediatR;

namespace InventiCore.Application.Features.Stock.Commands.RemoveStock;

public record RemoveStockCommand(Guid ProductId, Guid WarehouseId, int Quantity, string? Reason, string? PerformedBy) : IRequest<StockMovementDto>;

public class RemoveStockCommandValidator : AbstractValidator<RemoveStockCommand>
{
    public RemoveStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId é obrigatório.");
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("WarehouseId é obrigatório.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("A quantidade para saída deve ser maior que zero.");
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.PerformedBy).MaximumLength(200);
    }
}

public class RemoveStockCommandHandler : IRequestHandler<RemoveStockCommand, StockMovementDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public RemoveStockCommandHandler(IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<StockMovementDto> Handle(RemoveStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Produto {request.ProductId} não encontrado.");

        var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse {request.WarehouseId} não encontrado.");

        var stockItem = await _unitOfWork.StockItems.GetByProductAndWarehouseAsync(request.ProductId, request.WarehouseId, cancellationToken);
        
        if (stockItem is null || stockItem.Quantity < request.Quantity)
        {
            var available = stockItem?.Quantity ?? 0;
            throw new InsufficientStockException(product.Name, request.Quantity, available);
        }

        // Transação: Reduz quantidade e grava o log
        stockItem.RemoveQuantity(request.Quantity);
        _unitOfWork.StockItems.Update(stockItem);

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            StockItemId = stockItem.Id,
            Type = MovementType.Exit,
            Quantity = request.Quantity,
            Reason = request.Reason,
            PerformedBy = request.PerformedBy,
            SourceWarehouseId = request.WarehouseId
        };
        await _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);

        // O SaveChangesAsync usa a RowVersion automaticamente para Concorrência Otimista
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publica evento de estoque baixo no RabbitMQ se atingir o limite de segurança
        if (stockItem.Quantity <= product.MinimumStock)
        {
            var lowEvent = new StockLowEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Sku = product.Sku,
                WarehouseId = warehouse.Id,
                WarehouseName = warehouse.Name,
                CurrentQuantity = stockItem.Quantity,
                MinimumStock = product.MinimumStock,
                TenantId = product.TenantId
            };
            
            await _publishEndpoint.Publish(lowEvent, cancellationToken);
        }

        return StockMapper.ToDto(movement);
    }
}
