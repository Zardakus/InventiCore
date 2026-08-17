using System.Net.Http.Json;
using InventiCore.Application.Common.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventiCore.Api.Workers;

public class StockLowEventConsumer : IConsumer<StockLowEvent>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StockLowEventConsumer> _logger;
    private readonly HttpClient _httpClient;

    public StockLowEventConsumer(IConfiguration configuration, ILogger<StockLowEventConsumer> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task Consume(ConsumeContext<StockLowEvent> context)
    {
        var evt = context.Message;
        _logger.LogWarning("Processando evento de estoque baixo (via MassTransit) para {ProductName} no depósito {WarehouseName}", evt.ProductName, evt.WarehouseName);

        var webhookUrl = _configuration["Discord:WebhookUrl"];
        if (string.IsNullOrEmpty(webhookUrl) || webhookUrl.Contains("your_webhook_id_here"))
        {
            _logger.LogInformation("Discord Webhook não configurado em appsettings.json. Alerta ignorado.");
            return;
        }

        var payload = new
        {
            username = "InventiCore Alerts",
            avatar_url = "https://cdn-icons-png.flaticon.com/512/5968/5968756.png",
            embeds = new[]
            {
                new
                {
                    title = "⚠️ Alerta de Estoque Crítico",
                    description = $"O produto **{evt.ProductName}** atingiu a zona de perigo no depósito **{evt.WarehouseName}**.",
                    color = 16711680, // Vermelho
                    fields = new[]
                    {
                        new { name = "SKU", value = evt.Sku, inline = true },
                        new { name = "Estoque Atual", value = evt.CurrentQuantity.ToString(), inline = true },
                        new { name = "Estoque Mínimo", value = evt.MinimumStock.ToString(), inline = true },
                        new { name = "Tenant ID", value = evt.TenantId.ToString(), inline = false }
                    },
                    timestamp = DateTime.UtcNow.ToString("O")
                }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Falha ao enviar alerta para o Discord: {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogInformation("Alerta do Discord disparado com sucesso para o produto {ProductName}.", evt.ProductName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro de rede ao tentar acionar o Webhook do Discord.");
        }
    }
}
