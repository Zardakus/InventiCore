using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;

namespace InventiCore.Application.Common.Mappings;

public static class WarehouseMapper
{
    public static WarehouseDto ToDto(Warehouse entity)
    {
        return new WarehouseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Location = entity.Location,
            IsActive = entity.IsActive,
            TenantId = entity.TenantId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static IEnumerable<WarehouseDto> ToDtoList(IEnumerable<Warehouse> entities)
    {
        return entities.Select(ToDto);
    }
}
