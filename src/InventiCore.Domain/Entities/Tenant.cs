namespace InventiCore.Domain.Entities;

/// <summary>
/// A empresa cliente (tenant) que usa o sistema.
/// Cada tenant tem seus próprios depósitos, produtos e movimentações isolados.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty; // CNPJ
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
