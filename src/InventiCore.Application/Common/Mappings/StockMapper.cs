using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;

namespace InventiCore.Application.Common.Mappings;

public static class StockMapper
{
    public static StockItemDto ToDto(StockItem entity)
    {
        return new StockItemDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            WarehouseId = entity.WarehouseId,
            Quantity = entity.Quantity,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static StockMovementDto ToDto(StockMovement entity)
    {
        return new StockMovementDto
        {
            Id = entity.Id,
            StockItemId = entity.StockItemId,
            Type = (int)entity.Type,
            Quantity = entity.Quantity,
            Reason = entity.Reason,
            PerformedBy = entity.PerformedBy,
            SourceWarehouseId = entity.SourceWarehouseId,
            DestinationWarehouseId = entity.DestinationWarehouseId,
            CreatedAt = entity.CreatedAt
        };
    }
}
