namespace Notrelix.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

// ci proof: backend-only change, reverted after the run
