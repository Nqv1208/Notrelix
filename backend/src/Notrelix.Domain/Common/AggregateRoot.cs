namespace Notrelix.Domain.Common;

/// <summary>
/// Marker base class for aggregate roots. Inherits soft-delete behavior.
/// All top-level business entities should extend this.
/// </summary>
public abstract class AggregateRoot : SoftDeletableEntity
{
    public long Version { get; private set; } = 1;

    protected AggregateRoot() : base() { }
    protected AggregateRoot(Guid id) : base(id) { }

    protected void IncrementVersion()
    {
        Version++;
    }
}
