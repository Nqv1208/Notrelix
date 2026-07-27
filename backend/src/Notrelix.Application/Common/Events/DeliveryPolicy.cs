namespace Notrelix.Application.Common.Events;

public sealed record DeliveryDecision
{
    public bool Outbox { get; init; }
    public bool Realtime { get; init; }
    public bool Projection { get; init; }
}

public interface IDeliveryPolicy
{
    DeliveryDecision GetDecision(Type domainEventType);
}
