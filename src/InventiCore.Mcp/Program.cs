using InventiCore.Application;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Infrastructure;
using InventiCore.Mcp;
using InventiCore.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Recupera o TenantId obrigatório
var tenantArgIndex = Array.IndexOf(args, "--tenant-id");
if (tenantArgIndex == -1 || tenantArgIndex + 1 >= args.Length)
{
    Console.Error.WriteLine("ERRO: Argumento --tenant-id é obrigatório para garantir isolamento de contexto.");
    Environment.Exit(1);
}

if (!Guid.TryParse(args[tenantArgIndex + 1], out var tenantId))
{
    Console.Error.WriteLine("ERRO: --tenant-id inválido.");
    Environment.Exit(1);
}

// Configura o banco de dados (reaproveita appsettings do Api se quiser, ou sobrescreve local)
// Vamos adicionar as configurações do appsettings.json da API para facilitar o dev local
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("../InventiCore.Api/appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// ── Serilog no MCP ──────────────────────────────────────────────────────────
// CUIDADO: Não escreva no Console! O MCP opera via STDIO.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("TenantId", tenantId)
    .WriteTo.Async(a => a.File(
        new CompactJsonFormatter(), 
        "logs/inventicore-mcp-.json", 
        rollingInterval: RollingInterval.Day))
    .CreateLogger();

try
{
    var builder = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            // Removemos a adição de Debug/Console nativos para garantir silêncio no STDOUT
            services.AddLogging(logging => logging.ClearProviders());

            services.AddApplication();
            services.AddInfrastructure(configuration);

            // Injetar o contexto seguro MCP com o TenantId
            services.AddSingleton<ICurrentUserService>(new McpCurrentUserService { TenantId = tenantId });

            services.AddHostedService<McpStdioServer>();
        });


    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha catastrofica no servidor MCP");
}
finally
{
    Log.CloseAndFlush();
}
