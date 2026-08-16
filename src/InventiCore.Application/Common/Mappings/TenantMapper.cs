using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;

namespace InventiCore.Application.Common.Mappings;

public static class TenantMapper
{
    public static TenantDto ToDto(Tenant entity)
    {
        return new TenantDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Document = entity.Document,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static IEnumerable<TenantDto> ToDtoList(IEnumerable<Tenant> entities)
    {
        return entities.Select(ToDto);
    }
}
