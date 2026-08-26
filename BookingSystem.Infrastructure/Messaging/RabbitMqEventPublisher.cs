using System.Text.Json;
using BookingSystem.Application.Events;
using BookingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace BookingSystem.Infrastructure.Messaging;

// Producer dell'architettura Event-Driven: implementa IEventPublisher pubblicando su
// RabbitMQ. Adapter Pattern, in un certo senso — traduce il contratto generico
// "PublishAsync<TEvent>" nelle chiamate specifiche della libreria RabbitMQ.Client
// (creazione canale, dichiarazione exchange, publish).
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IConnection connection, IOptions<RabbitMqSettings> settings, ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        try
        {
            var routingKey = GetRoutingKey<TEvent>();

            await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(@event);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                Type = typeof(TEvent).Name,
                DeliveryMode = DeliveryModes.Persistent
            };

            await channel.BasicPublishAsync(_settings.ExchangeName, routingKey, mandatory: false, properties, body, cancellationToken);
        }
        catch (Exception ex)
        {
            // Punto chiave del design: un errore di pubblicazione viene solo loggato,
            // mai rilanciato. Creare/cancellare una prenotazione (chi chiama questo
            // metodo) non deve MAI fallire solo perché il broker RabbitMQ è
            // temporaneamente irraggiungibile — nel peggiore dei casi si perde solo la
            // notifica simulata, non l'operazione di business.
            _logger.LogWarning(ex, "Failed to publish event {EventType} to RabbitMQ.", typeof(TEvent).Name);
        }
    }

    private static string GetRoutingKey<TEvent>() => typeof(TEvent).Name switch
    {
        nameof(BookingCreatedEvent) => "booking.created",
        nameof(BookingCancelledEvent) => "booking.cancelled",
        _ => throw new InvalidOperationException($"No routing key mapped for event type '{typeof(TEvent).Name}'.")
    };
}
