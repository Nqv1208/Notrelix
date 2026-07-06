namespace Notrelix.Infrastructure.Services;

public sealed class CurrentCorrelationContext : ICorrelationContext
{
    public Guid CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }

    public void Set(Guid correlationId, Guid? causationId = null)
    {
        CorrelationId = correlationId == Guid.Empty
            ? throw new ArgumentException("CorrelationId cannot be empty.", nameof(correlationId))
            : correlationId;
        CausationId = causationId;
    }

    public void Clear()
    {
        CorrelationId = Guid.Empty;
        CausationId = null;
    }
}
