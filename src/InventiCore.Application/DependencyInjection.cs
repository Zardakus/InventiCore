using FluentValidation;
using InventiCore.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InventiCore.Application;

/// <summary>
/// Extension method para registrar todos os serviços da camada Application no DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // ── MediatR ──────────────────────────────────────────
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // ── FluentValidation ─────────────────────────────────
        services.AddValidatorsFromAssembly(assembly);

        // ── Pipeline Behaviors ───────────────────────────────
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
