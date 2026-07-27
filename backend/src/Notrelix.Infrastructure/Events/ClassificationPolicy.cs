namespace Notrelix.Infrastructure.Events;

public sealed class ClassificationPolicy : IClassificationPolicy
{
    private readonly Dictionary<Type, Classification> _classifications;

    public ClassificationPolicy(Dictionary<Type, Classification> classifications)
    {
        _classifications = classifications;
    }

    public Classification GetClassification(Type domainEventType)
    {
        if (_classifications.TryGetValue(domainEventType, out var classification))
            return classification;

        return new Classification { Value = EventClassification.Business };
    }

    public static ClassificationPolicyBuilder CreateBuilder() => new();

    public sealed class ClassificationPolicyBuilder
    {
        private readonly Dictionary<Type, Classification> _classifications = new();

        public ClassificationPolicyBuilder Register<T>(Classification classification) where T : IDomainEvent
        {
            _classifications[typeof(T)] = classification;
            return this;
        }

        public ClassificationPolicyBuilder Register<T>(EventClassification classification) where T : IDomainEvent
        {
            _classifications[typeof(T)] = new Classification { Value = classification };
            return this;
        }

        public ClassificationPolicyBuilder RegisterAudit<T>() where T : IDomainEvent
        {
            _classifications[typeof(T)] = new Classification { Value = EventClassification.Audit, Audit = true };
            return this;
        }

        public ClassificationPolicyBuilder RegisterScope<T>(
            EventClassification classification,
            bool audit) where T : IDomainEvent
        {
            _classifications[typeof(T)] = new Classification { Value = classification, Audit = audit };
            return this;
        }

        public ClassificationPolicy Build() => new(_classifications);
    }
}
