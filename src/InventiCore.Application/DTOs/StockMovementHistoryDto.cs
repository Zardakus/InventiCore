namespace InventiCore.Application.DTOs;

public class StockMovementHistoryDto
{
    public DateTime CreatedAt { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalValue { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? SourceWarehouseName { get; set; }
    public string? DestinationWarehouseName { get; set; }
}
