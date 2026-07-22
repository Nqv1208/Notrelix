namespace Notrelix.Infrastructure.Events;

public sealed class DeliveryPolicy : IDeliveryPolicy
{
    private readonly Dictionary<Type, DeliveryDecision> _decisions;

    public DeliveryPolicy(Dictionary<Type, DeliveryDecision> decisions)
    {
        _decisions = decisions;
    }

    public DeliveryDecision GetDecision(Type domainEventType)
    {
        if (_decisions.TryGetValue(domainEventType, out var decision))
            return decision;

        return new DeliveryDecision { Outbox = true };
    }

    public static DeliveryPolicyBuilder CreateBuilder() => new();

    public sealed class DeliveryPolicyBuilder
    {
        private readonly Dictionary<Type, DeliveryDecision> _decisions = new();

        public DeliveryPolicyBuilder Register<T>(DeliveryDecision decision) where T : IDomainEvent
        {
            _decisions[typeof(T)] = decision;
            return this;
        }

        public DeliveryPolicyBuilder Register<T>(bool outbox, bool realtime = false, bool projection = false)
            where T : IDomainEvent
        {
            _decisions[typeof(T)] = new DeliveryDecision
            {
                Outbox = outbox,
                Realtime = realtime,
                Projection = projection,
            };
            return this;
        }

        public DeliveryPolicyBuilder OutboxOnly<T>() where T : IDomainEvent
        {
            _decisions[typeof(T)] = new DeliveryDecision { Outbox = true };
            return this;
        }

        public DeliveryPolicy Build() => new(_decisions);
    }
}
