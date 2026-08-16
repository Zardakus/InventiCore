namespace InventiCore.Domain.Entities;

/// <summary>
/// Produto base do catálogo. Representa o "o quê" (ex: "Notebook Dell Inspiron 15").
/// A quantidade real é controlada pela entidade StockItem.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty; // Código único do produto
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int MinimumStock { get; set; } // Ponto de reposição
    public bool IsActive { get; set; } = true;

    // Foreign Keys
    public Guid TenantId { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
