using System.Text;
using System.Text.Json;
using BookingSystem.Application.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingSystem.Infrastructure.Messaging;

public class BookingNotificationConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly RabbitMqSettings _settings;
    private readonly BookingNotificationHandler _handler;
    private readonly ILogger<BookingNotificationConsumer> _logger;
    private IChannel? _channel;

    public BookingNotificationConsumer(
        IConnection connection,
        IOptions<RabbitMqSettings> settings,
        BookingNotificationHandler handler,
        ILogger<BookingNotificationConsumer> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(_settings.NotificationsQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(_settings.NotificationsQueueName, _settings.ExchangeName, "booking.created", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(_settings.NotificationsQueueName, _settings.ExchangeName, "booking.cancelled", cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(_settings.NotificationsQueueName, autoAck: false, consumer, cancellationToken: stoppingToken);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);

            switch (ea.BasicProperties.Type)
            {
                case nameof(BookingCreatedEvent):
                    var created = JsonSerializer.Deserialize<BookingCreatedEvent>(json)
                        ?? throw new JsonException("Il messaggio BookingCreatedEvent deserializzato è nullo.");
                    await _handler.HandleBookingCreatedAsync(created);
                    break;

                case nameof(BookingCancelledEvent):
                    var cancelled = JsonSerializer.Deserialize<BookingCancelledEvent>(json)
                        ?? throw new JsonException("Il messaggio BookingCancelledEvent deserializzato è nullo.");
                    await _handler.HandleBookingCancelledAsync(cancelled);
                    break;

                default:
                    _logger.LogWarning("Ricevuto messaggio con tipo sconosciuto '{Type}', scartato.", ea.BasicProperties.Type);
                    break;
            }

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'elaborazione del messaggio di notifica prenotazione, messaggio scartato.");
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
