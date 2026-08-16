namespace InventiCore.Domain.Entities;

/// <summary>
/// Representa a quantidade real de um Produto dentro de um Warehouse específico.
/// Relacionamento N:N materializado entre Product e Warehouse.
/// Usa RowVersion para Concorrência Otimista (Optimistic Concurrency).
/// </summary>
public class StockItem : BaseEntity
{
    public int Quantity { get; set; }

    // Foreign Keys
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }

    // Concorrência Otimista: o EF Core usa essa coluna para detectar
    // se outro usuário alterou o registro entre a leitura e a escrita.
    public uint RowVersion { get; set; }

    // Navigation Properties
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public void AddQuantity(int amount)
    {
        if (amount <= 0) throw new ArgumentException("A quantidade a ser adicionada deve ser maior que zero.", nameof(amount));
        Quantity += amount;
    }

    public void RemoveQuantity(int amount)
    {
        if (amount <= 0) throw new ArgumentException("A quantidade a ser removida deve ser maior que zero.", nameof(amount));
        if (Quantity < amount) throw new InventiCore.Domain.Exceptions.InsufficientStockException(Product?.Name ?? "Desconhecido", amount, Quantity);
        Quantity -= amount;
    }
}
