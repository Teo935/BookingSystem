namespace BookingSystem.Infrastructure.Messaging;

// Topologia RabbitMQ del progetto: un solo exchange di tipo "topic" (booking.events)
// con due routing key (booking.created, booking.cancelled) entrambe legate alla
// stessa coda (booking.notifications) — vedi BookingNotificationConsumer per il binding.
public class RabbitMqSettings
{
    public string ExchangeName { get; set; } = "booking.events";
    public string NotificationsQueueName { get; set; } = "booking.notifications";
}
