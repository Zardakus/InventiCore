using InventiCore.Domain.Enums;

namespace InventiCore.Domain.Entities;

/// <summary>
/// Registro de auditoria imutável. Cada entrada, saída, transferência
/// ou ajuste gera um StockMovement para rastreabilidade completa.
/// Essa tabela NUNCA sofre UPDATE ou DELETE — é append-only.
/// </summary>
public class StockMovement : BaseEntity
{
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; } // Ex: "Venda #12345", "Avaria no transporte"
    public string? PerformedBy { get; set; } // Usuário que executou a ação

    // Foreign Keys
    public Guid StockItemId { get; set; }

    // Para transferências: de onde veio e para onde foi
    public Guid? SourceWarehouseId { get; set; }
    public Guid? DestinationWarehouseId { get; set; }

    // Navigation Properties
    public StockItem StockItem { get; set; } = null!;
    public Warehouse? SourceWarehouse { get; set; }
    public Warehouse? DestinationWarehouse { get; set; }
}
