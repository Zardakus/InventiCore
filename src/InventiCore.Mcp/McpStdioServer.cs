using System.Text.Json;
using System.Text.Json.Nodes;
using InventiCore.Application.Features.Stock.Commands.TransferStock;
using InventiCore.Application.Features.Stock.Queries.AnalyzeLowStock;
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
                                        productId = new { type = "string" },
                                        sourceWarehouseId = new { type = "string" },
                                        destinationWarehouseId = new { type = "string" },
                                        quantity = new { type = "number" }
                                    },
                                    required = new[] { "productId", "sourceWarehouseId", "destinationWarehouseId", "quantity" }
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

                        await _mediator.Send(command, stoppingToken);
                        Respond(id, new { content = new object[] { new { type = "text", text = "Transferência executada com sucesso." } } });
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
                // Em JSON-RPC, falhas críticas poderiam retornar Internal Error
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
