namespace InventiCore.Domain.Entities;

/// <summary>
/// Depósito/galpão físico onde os produtos são armazenados.
/// Um Tenant pode ter múltiplos Warehouses.
/// </summary>
public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // Endereço ou cidade
    public bool IsActive { get; set; } = true;

    // Foreign Keys
    public Guid TenantId { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
