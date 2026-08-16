using InventiCore.Application.Common.Interfaces;

namespace InventiCore.Mcp.Services;

public class McpCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Empty;
    public Guid? TenantId { get; set; }
    public bool IsAuthenticated => TenantId.HasValue;
}
