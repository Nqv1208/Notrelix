namespace Notrelix.Domain.Common;

/// <summary>
/// Base class for aggregate roots with version tracking but NO soft-delete.
/// Use SoftDeletableAggregateRoot for aggregates that need deletion lifecycle.
/// </summary>
public abstract class AggregateRoot : AuditableEntity
{
    public long Version { get; private set; } = 1;

    protected AggregateRoot() : base() { }
    protected AggregateRoot(Guid id) : base(id) { }

    protected void IncrementVersion()
    {
        checked
        {
            Version++;
        }
    }
}
