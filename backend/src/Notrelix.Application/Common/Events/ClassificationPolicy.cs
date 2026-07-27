namespace Notrelix.Application.Common.Events;

public sealed record Classification
{
    public EventClassification Value { get; init; }
    public bool Audit { get; init; } = true;
}

public interface IClassificationPolicy
{
    Classification GetClassification(Type domainEventType);
}
