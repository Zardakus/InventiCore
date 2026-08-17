using System.Text;
using InventiCore.Api.Middleware;
using InventiCore.Api.Services;
using InventiCore.Application;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

// â”€â”€ HttpClient para o Consumer do Discord com Polly (ResiliÃªncia) â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddHttpClient<InventiCore.Api.Workers.StockLowEventConsumer>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError() // Intercepta 5xx, 408 e falhas de rede
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))) // Exponential backoff: 2s, 4s, 8s
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30))); // Abre circuito por 30s apÃ³s 3 falhas consecutivas


// â”€â”€ Mensageria (MassTransit + RabbitMQ) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InventiCore.Api.Workers.StockLowEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // ConexÃ£o com RabbitMQ via docker compose
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
        
        cfg.ConfigureEndpoints(context);
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

app.Run();
