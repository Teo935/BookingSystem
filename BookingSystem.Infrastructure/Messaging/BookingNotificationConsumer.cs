using System.Text;
using System.Text.Json;
using BookingSystem.Application.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingSystem.Infrastructure.Messaging;

// Consumer dell'architettura Event-Driven: un BackgroundService, cioè un processo che
// gira in sottofondo per tutta la vita dell'applicazione (stesso processo dell'API, non
// un progetto worker separato — scelta di semplicità). Riceve i messaggi pubblicati da
// RabbitMqEventPublisher e li smista a BookingNotificationHandler.
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

        // Dichiarare exchange/coda/binding qui (e non altrove) li rende idempotenti:
        // ad ogni avvio dell'app la topologia RabbitMQ viene creata se manca, o
        // verificata se esiste già — nessuna configurazione manuale del broker richiesta.
        await _channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(_settings.NotificationsQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(_settings.NotificationsQueueName, _settings.ExchangeName, "booking.created", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(_settings.NotificationsQueueName, _settings.ExchangeName, "booking.cancelled", cancellationToken: stoppingToken);

        // prefetchCount: 1 = il consumer riceve un solo messaggio alla volta e non ne
        // riceve un altro finché non ha fatto ack/nack del precedente: evita che un
        // singolo consumer si accumuli messaggi in memoria più velocemente di quanto
        // riesca a elaborarli.
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        // autoAck: false = ack manuale (vedi OnMessageReceivedAsync): un messaggio è
        // rimosso dalla coda solo dopo essere stato elaborato con successo, non appena
        // ricevuto — se il processo crasha a metà elaborazione, RabbitMQ lo riconsegna.
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
            // requeue: false — un messaggio che fallisce la deserializzazione (es.
            // formato cambiato) fallirebbe di nuovo all'infinito se rimesso in coda,
            // bloccando l'elaborazione di tutti i messaggi successivi. Meglio scartarlo
            // con un log di errore che restare bloccati in un retry-loop infinito.
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
