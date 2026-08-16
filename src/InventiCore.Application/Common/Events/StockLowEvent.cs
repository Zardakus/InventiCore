namespace InventiCore.Application.Common.Events;

/// <summary>
/// Evento de domínio disparado quando o estoque de um produto atinge
/// ou cai abaixo do MinimumStock definido.
/// </summary>
public record StockLowEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public int CurrentQuantity { get; init; }
    public int MinimumStock { get; init; }
    public Guid TenantId { get; init; }
    public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;
}
