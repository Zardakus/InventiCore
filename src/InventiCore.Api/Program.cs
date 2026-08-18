using System.Text;
using InventiCore.Api.Middleware;
using InventiCore.Api.Services;
using InventiCore.Application;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Polly;
using Polly.Extensions.Http;
using MassTransit;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Async(a => a.File(
            new CompactJsonFormatter(), 
            "logs/inventicore-api-.json", 
            rollingInterval: RollingInterval.Day));
});

// ── Dependency Injection (Clean Architecture layers) ─────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Context & Auth Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MinhaSuperChaveSecretaMuitoLongaParaOJWTAqui2024!";
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

// â”€â”€ Cache DistribuÃ­do (Redis) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    options.InstanceName = "InventiCore_";
});

// â”€â”€ HttpClient para o Consumer do Discord com Polly (ResiliÃªncia) â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddHttpClient<InventiCore.Api.Workers.StockLowEventConsumer>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError() // Intercepta 5xx, 408 e falhas de rede
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))) // Exponential backoff: 2s, 4s, 8s
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30))); // Abre circuito por 30s apÃ³s 3 falhas consecutivas


// Mensageria (MassTransit + RabbitMQ)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InventiCore.Api.Workers.StockLowEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rmqHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
        var rmqUser = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
        var rmqPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(rmqHost, "/", h => {
            h.Username(rmqUser);
            h.Password(rmqPass);
        });

        cfg.ReceiveEndpoint("stock-low-events", e =>
        {
            e.ConfigureConsumer<InventiCore.Api.Workers.StockLowEventConsumer>(context);
        });
    });
});

// ── Controllers ──────────────────────────────────────────
builder.Services.AddControllers();

// ── CORS ─────────────────────────────────────────────────
builder.Services.AddCors();

// ── Swagger ──────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InventiCore API",
        Version = "v1",
        Description = "SaaS B2B de Gestão de Estoque Distribuído"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta forma: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// ── Global Exception Handler (deve ser o primeiro middleware) ──
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// ── Pipeline HTTP ────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var currentUser = httpContext.RequestServices.GetService<ICurrentUserService>();
        if (currentUser != null && currentUser.TenantId != Guid.Empty)
        {
            diagnosticContext.Set("TenantId", currentUser.TenantId);
        }
    };
});
app.UseHttpsRedirection();

// Tenant Context Middleware para enriquecer logs internos (handlers/domain)
app.Use(async (context, next) =>
{
    var currentUser = context.RequestServices.GetService<ICurrentUserService>();
    if (currentUser != null && currentUser.TenantId != Guid.Empty)
    {
        using (Serilog.Context.LogContext.PushProperty("TenantId", currentUser.TenantId))
        {
            await next(context);
        }
    }
    else
    {
        await next(context);
    }
});

// Use CORS before Auth
app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventiCore.Infrastructure.Data.InventiCoreDbContext>();
    dbContext.Database.Migrate();

    // Seeding de teste para o Tenant fixo do Frontend (simulação SaaS)
    var defaultTenantId = Guid.Parse("7eca4967-f455-464c-bb32-925522806364");
    if (!dbContext.Tenants.Any(t => t.Id == defaultTenantId))
    {
        dbContext.Tenants.Add(new InventiCore.Domain.Entities.Tenant
        {
            Id = defaultTenantId,
            Name = "Empresa Demonstração",
            Document = "00.000.000/0001-00",
            IsActive = true
        });
        dbContext.SaveChanges();
    }
}

app.Run();
