namespace InventiCore.Application.Common.Interfaces;

/// <summary>
/// Abstração para publicação de mensagens/eventos de domínio (ex: RabbitMQ).
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default) where T : class;
}
