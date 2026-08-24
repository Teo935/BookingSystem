namespace BookingSystem.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string ExchangeName { get; set; } = "booking.events";
    public string NotificationsQueueName { get; set; } = "booking.notifications";
}
