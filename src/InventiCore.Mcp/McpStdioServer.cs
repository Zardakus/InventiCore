using System.Text.Json;
using System.Text.Json.Nodes;
using InventiCore.Application.Features.Products.Commands.CreateProduct;
using InventiCore.Application.Features.Stock.Commands.TransferStock;
using InventiCore.Application.Features.Stock.Queries.AnalyzeLowStock;
using InventiCore.Application.Features.Warehouses.Commands.DeleteWarehouse;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventiCore.Mcp;

public class McpStdioServer : BackgroundService
{
    private readonly IMediator _mediator;
    private readonly ILogger<McpStdioServer> _logger;

    public McpStdioServer(IMediator mediator, ILogger<McpStdioServer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP Server iniciado. Aguardando mensagens JSON-RPC no stdin...");
        
        using var reader = new StreamReader(Console.OpenStandardInput());
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(stoppingToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var request = JsonNode.Parse(line);
                if (request == null) continue;

                var id = request["id"]?.GetValue<string>();
                var method = request["method"]?.GetValue<string>();

                if (method == "initialize")
                {
                    Respond(id, new
                    {
                        protocolVersion = "2024-11-05",
                        serverInfo = new { name = "InventiCore.Mcp", version = "1.0.0" },
                        capabilities = new { tools = new { } }
                    });
                }
                else if (method == "tools/list")
                {
                    Respond(id, new
                    {
                        tools = new object[]
                        {
                            new {
                                name = "analyze_low_stock",
                                description = "Analisa produtos com estoque baixo e identifica depósitos com saldo positivo para transferência.",
                                inputSchema = new { type = "object", properties = new { } }
                            },
                            new {
                                name = "execute_stock_transfer",
                                description = "Executa a transferência de estoque entre dois depósitos.",
                                inputSchema = new {
                                    type = "object",
                                    properties = new {
                                        productId = new { type = "string", description = "ID (GUID) do produto a ser transferido." },
                                        sourceWarehouseId = new { type = "string", description = "ID (GUID) do depósito de origem." },
                                        destinationWarehouseId = new { type = "string", description = "ID (GUID) do depósito de destino." },
                                        quantity = new { type = "number", description = "Quantidade de unidades a transferir." }
                                    },
                                    required = new[] { "productId", "sourceWarehouseId", "destinationWarehouseId", "quantity" }
                                }
                            },
                            new {
                                name = "create_product",
                                description = "Cadastra um novo produto no sistema, vinculado automaticamente ao Tenant ativo. O SKU deve ser único por Tenant.",
                                inputSchema = new {
                                    type = "object",
                                    properties = new {
                                        name = new { type = "string", description = "Nome do produto (ex: 'Notebook Dell Inspiron 15')." },
                                        sku = new { type = "string", description = "Código SKU único do produto dentro do Tenant (ex: 'NB-DELL-015')." },
                                        description = new { type = "string", description = "Descrição opcional do produto." },
                                        category = new { type = "string", description = "Categoria opcional (ex: 'Eletrônicos', 'Periféricos')." },
                                        costPrice = new { type = "number", description = "Preço de custo unitário." },
                                        sellingPrice = new { type = "number", description = "Preço de venda unitário." },
                                        minimumStock = new { type = "number", description = "Quantidade mínima de segurança que dispara alerta de estoque baixo." }
                                    },
                                    required = new[] { "name", "sku", "minimumStock" }
                                }
                            },
                            new {
                                name = "delete_warehouse",
                                description = "Desativa (soft delete) um depósito do sistema. O depósito não será mais listado, mas seus dados históricos são preservados para auditoria.",
                                inputSchema = new {
                                    type = "object",
                                    properties = new {
                                        warehouseId = new { type = "string", description = "ID (GUID) do depósito a ser desativado." }
                                    },
                                    required = new[] { "warehouseId" }
                                }
                            }
                        }
                    });
                }
                else if (method == "tools/call")
                {
                    var toolName = request["params"]?["name"]?.GetValue<string>();
                    var toolArgs = request["params"]?["arguments"];

                    if (toolName == "analyze_low_stock")
                    {
                        var result = await _mediator.Send(new AnalyzeLowStockQuery(), stoppingToken);
                        Respond(id, new { content = new object[] { new { type = "text", text = JsonSerializer.Serialize(result) } } });
                    }
                    else if (toolName == "execute_stock_transfer")
                    {
                        var command = new TransferStockCommand(
                            Guid.Parse(toolArgs?["productId"]?.GetValue<string>()!),
                            Guid.Parse(toolArgs?["sourceWarehouseId"]?.GetValue<string>()!),
                            Guid.Parse(toolArgs?["destinationWarehouseId"]?.GetValue<string>()!),
                            (int)toolArgs?["quantity"]?.GetValue<decimal>()!,
                            "Automação MCP",
                            "Agente MCP"
                        );

                        var result = await _mediator.Send(command, stoppingToken);
                        Respond(id, new { content = new object[] { new { type = "text", text = $"Transferência executada com sucesso. Movimento ID: {result.Id}" } } });
                    }
                    else if (toolName == "create_product")
                    {
                        var command = new CreateProductCommand
                        {
                            Name = toolArgs?["name"]?.GetValue<string>() ?? throw new ArgumentException("Nome do produto é obrigatório."),
                            Sku = toolArgs?["sku"]?.GetValue<string>() ?? throw new ArgumentException("SKU do produto é obrigatório."),
                            Description = toolArgs?["description"]?.GetValue<string>(),
                            Category = toolArgs?["category"]?.GetValue<string>(),
                            CostPrice = toolArgs?["costPrice"]?.GetValue<decimal>() ?? 0,
                            SellingPrice = toolArgs?["sellingPrice"]?.GetValue<decimal>() ?? 0,
                            MinimumStock = (int)(toolArgs?["minimumStock"]?.GetValue<decimal>() ?? 10)
                        };

                        var result = await _mediator.Send(command, stoppingToken);
                        Respond(id, new { content = new object[] { new { type = "text", text = $"Produto '{result.Name}' (SKU: {result.Sku}) cadastrado com sucesso. ID: {result.Id}" } } });
                    }
                    else if (toolName == "delete_warehouse")
                    {
                        var warehouseId = Guid.Parse(toolArgs?["warehouseId"]?.GetValue<string>()
                            ?? throw new ArgumentException("warehouseId é obrigatório."));

                        await _mediator.Send(new DeleteWarehouseCommand(warehouseId), stoppingToken);
                        Respond(id, new { content = new object[] { new { type = "text", text = $"Depósito {warehouseId} desativado com sucesso (soft delete). Dados históricos preservados." } } });
                    }
                    else
                    {
                        RespondError(id, -32601, "Ferramenta não encontrada");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem MCP");
                var requestNode = JsonNode.Parse("{}");
                try { requestNode = JsonNode.Parse(line); } catch { }
                var errorId = requestNode?["id"]?.GetValue<string>();
                RespondError(errorId, -32603, $"Erro interno: {ex.Message}");
            }
        }
    }

    private void Respond(string? id, object result)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = id,
            result = result
        };
        Console.WriteLine(JsonSerializer.Serialize(response));
    }

    private void RespondError(string? id, int code, string message)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = id,
            error = new { code, message }
        };
        Console.WriteLine(JsonSerializer.Serialize(response));
    }
}
