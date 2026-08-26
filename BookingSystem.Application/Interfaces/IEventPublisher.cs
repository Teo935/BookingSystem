namespace BookingSystem.Application.Interfaces;

// Astrazione Event-Driven: pubblica un evento di dominio (es. BookingCreatedEvent) senza
// che il chiamante (BookingService) sappia che dietro c'è RabbitMQ. L'implementazione
// (RabbitMqEventPublisher) non deve mai far fallire l'operazione che genera l'evento se
// il broker è irraggiungibile — creare/cancellare una prenotazione non può dipendere
// dalla disponibilità della messaggistica.
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
