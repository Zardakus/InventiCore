namespace InventiCore.Application.DTOs;

public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }
    public int LowStockItems { get; set; }
    public List<WarehouseStockSummaryDto> StockPerWarehouse { get; set; } = new();
}

public class WarehouseStockSummaryDto
{
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
}
