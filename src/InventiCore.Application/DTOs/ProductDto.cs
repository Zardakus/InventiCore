namespace InventiCore.Application.DTOs;

/// <summary>
/// DTO de resposta para a entidade Product.
/// Nunca exponha entidades de domínio diretamente na API.
/// </summary>
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public int MinimumStock { get; init; }
    public bool IsActive { get; init; }
    public Guid TenantId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
