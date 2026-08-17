using InventiCore.Domain.Interfaces;
using InventiCore.Infrastructure.Data;
using InventiCore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventiCore.Infrastructure;

/// <summary>
/// Extension method para registrar todos os serviços da camada de Infrastructure no DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── EF Core (PostgreSQL) ─────────────────────────────────
        services.AddDbContext<InventiCoreDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // ── Repositories ─────────────────────────────────────────
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IStockItemRepository, StockItemRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();

        // ── Unit of Work ─────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
