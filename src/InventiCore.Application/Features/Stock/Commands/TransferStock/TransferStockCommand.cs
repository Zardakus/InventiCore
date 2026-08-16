using FluentValidation;
using InventiCore.Application.Common.Events;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Enums;
using InventiCore.Domain.Exceptions;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Stock.Commands.TransferStock;

public record TransferStockCommand(Guid ProductId, Guid SourceWarehouseId, Guid DestinationWarehouseId, int Quantity, string? Reason, string? PerformedBy) : IRequest<StockMovementDto>;

public class TransferStockCommandValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId)
            .NotEqual(x => x.SourceWarehouseId)
            .WithMessage("O depósito de destino deve ser diferente do depósito de origem.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("A quantidade para transferência deve ser maior que zero.");
    }
}

public class TransferStockCommandHandler : IRequestHandler<TransferStockCommand, StockMovementDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _publisher;

    public TransferStockCommandHandler(IUnitOfWork unitOfWork, IMessagePublisher publisher)
    {
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<StockMovementDto> Handle(TransferStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Produto {request.ProductId} não encontrado.");

        var sourceWarehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.SourceWarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source Warehouse não encontrado.");
            
        var destWarehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.DestinationWarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Destination Warehouse não encontrado.");

        if (sourceWarehouse.TenantId != destWarehouse.TenantId)
            throw new InvalidOperationException("Não é permitido transferir estoque entre Tenants diferentes.");

        var sourceStock = await _unitOfWork.StockItems.GetByProductAndWarehouseAsync(request.ProductId, request.SourceWarehouseId, cancellationToken);
        
        if (sourceStock is null || sourceStock.Quantity < request.Quantity)
        {
            var available = sourceStock?.Quantity ?? 0;
            throw new InsufficientStockException(product.Name, request.Quantity, available);
        }

        var destStock = await _unitOfWork.StockItems.GetByProductAndWarehouseAsync(request.ProductId, request.DestinationWarehouseId, cancellationToken);
        if (destStock is null)
        {
            destStock = new StockItem
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                WarehouseId = request.DestinationWarehouseId,
                Quantity = 0
            };
            await _unitOfWork.StockItems.AddAsync(destStock, cancellationToken);
        }

        // Realiza a transferência
        sourceStock.RemoveQuantity(request.Quantity);
        destStock.AddQuantity(request.Quantity);

        _unitOfWork.StockItems.Update(sourceStock);
        _unitOfWork.StockItems.Update(destStock);

        // Grava apenas 1 log de transferência refletindo os dois lados
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            StockItemId = sourceStock.Id, // Atrelado ao item de origem primariamente
            Type = MovementType.Transfer,
            Quantity = request.Quantity,
            Reason = request.Reason ?? "Transferência entre depósitos",
            PerformedBy = request.PerformedBy,
            SourceWarehouseId = request.SourceWarehouseId,
            DestinationWarehouseId = request.DestinationWarehouseId
        };
        await _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Evento se o depósito de origem ficar baixo
        if (sourceStock.Quantity <= product.MinimumStock)
        {
            var lowEvent = new StockLowEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Sku = product.Sku,
                WarehouseId = sourceWarehouse.Id,
                WarehouseName = sourceWarehouse.Name,
                CurrentQuantity = sourceStock.Quantity,
                MinimumStock = product.MinimumStock,
                TenantId = product.TenantId
            };
            await _publisher.PublishAsync(lowEvent, "stock.low.alert", cancellationToken);
        }

        return StockMapper.ToDto(movement);
    }
}
