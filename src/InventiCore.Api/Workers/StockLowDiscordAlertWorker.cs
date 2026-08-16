using System.Text;
using System.Text.Json;
using InventiCore.Application.Common.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventiCore.Api.Workers;

/// <summary>
/// Worker de background que consome mensagens do RabbitMQ e dispara um Webhook.
/// </summary>
public class StockLowDiscordAlertWorker : BackgroundService
{
    private readonly ILogger<StockLowDiscordAlertWorker> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly HttpClient _httpClient;
    private const string QueueName = "stock.low.discord.alerts";

    public StockLowDiscordAlertWorker(ILogger<StockLowDiscordAlertWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        
        InitRabbitMq();
    }

    private void InitRabbitMq()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMQ:UserName"] ?? "inventicore",
            Password = _configuration["RabbitMQ:Password"] ?? "inventicore_dev_2024",
            DispatchConsumersAsync = true // Importante para usar AsyncEventingBasicConsumer
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Garante que a exchange existe
            _channel.ExchangeDeclare(exchange: "inventicore.events", type: ExchangeType.Topic, durable: true);
            
            // Declara a fila e faz o bind
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: QueueName, exchange: "inventicore.events", routingKey: "stock.low.alert");

            _logger.LogInformation("StockLowDiscordAlertWorker conectado ao RabbitMQ.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao conectar no RabbitMQ para consumir alertas.");
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null) return Task.CompletedTask;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var stockLowEvent = JsonSerializer.Deserialize<StockLowEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (stockLowEvent != null)
                {
                    await SendDiscordWebhookAsync(stockLowEvent, stoppingToken);
                }
                
                // Ack da mensagem para removê-la da fila
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do RabbitMQ: {Message}", message);
                // Nack para reencaminhar (em ambiente produtivo, enviar para DLQ)
                _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task SendDiscordWebhookAsync(StockLowEvent evt, CancellationToken stoppingToken)
    {
        var webhookUrl = _configuration["DiscordWebhookUrl"];
        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("DiscordWebhookUrl não configurado no appsettings.json. Ignorando envio de webhook.");
            return;
        }

        var payload = new
        {
            content = $"⚠️ **ALERTA DE ESTOQUE BAIXO** ⚠️\n" +
                      $"Produto: `{evt.ProductName}` (SKU: `{evt.Sku}`)\n" +
                      $"Depósito: `{evt.WarehouseName}`\n" +
                      $"Estoque Atual: **{evt.CurrentQuantity}** (Mínimo: {evt.MinimumStock})"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(webhookUrl, content, stoppingToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Webhook do Discord enviado com sucesso para Produto: {ProductId}", evt.ProductId);
        }
        else
        {
            _logger.LogError("Falha ao enviar webhook do Discord. StatusCode: {StatusCode}", response.StatusCode);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _httpClient.Dispose();
        base.Dispose();
    }
}
