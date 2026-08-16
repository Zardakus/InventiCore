namespace InventiCore.Application.DTOs;

public record StockItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record StockMovementDto
{
    public Guid Id { get; init; }
    public Guid StockItemId { get; init; }
    public int Type { get; init; } // 1=Entry, 2=Exit, 3=Transfer, 4=Adjustment
    public int Quantity { get; init; }
    public string? Reason { get; init; }
    public string? PerformedBy { get; init; }
    public Guid? SourceWarehouseId { get; init; }
    public Guid? DestinationWarehouseId { get; init; }
    public DateTime CreatedAt { get; init; }
}
