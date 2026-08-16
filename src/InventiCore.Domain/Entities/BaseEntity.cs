namespace InventiCore.Domain.Entities;

/// <summary>
/// Classe base abstrata com propriedades comuns de auditoria.
/// Todas as entidades herdarão dessa classe.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
