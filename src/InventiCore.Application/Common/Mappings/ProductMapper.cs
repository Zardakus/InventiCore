using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;

namespace InventiCore.Application.Common.Mappings;

/// <summary>
/// Mapper manual Entity ↔ DTO para Product.
/// Decisão: sem AutoMapper para manter controle explícito e evitar dependência extra.
/// </summary>
public static class ProductMapper
{
    public static ProductDto ToDto(Product entity)
    {
        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Sku = entity.Sku,
            Description = entity.Description,
            Category = entity.Category,
            CostPrice = entity.CostPrice,
            SellingPrice = entity.SellingPrice,
            MinimumStock = entity.MinimumStock,
            IsActive = entity.IsActive,
            TenantId = entity.TenantId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static IEnumerable<ProductDto> ToDtoList(IEnumerable<Product> entities)
    {
        return entities.Select(ToDto);
    }
}
