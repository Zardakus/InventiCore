using InventiCore.Api.Middleware;
using InventiCore.Application;
using InventiCore.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

// ── Dependency Injection (Clean Architecture layers) ─────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register Background Worker
builder.Services.AddHostedService<InventiCore.Api.Workers.StockLowDiscordAlertWorker>();

// ── Controllers ──────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger ──────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "InventiCore API",
        Version = "v1",
        Description = "SaaS B2B de Gestão de Estoque Distribuído"
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

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
