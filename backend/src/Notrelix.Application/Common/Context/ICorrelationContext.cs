namespace Notrelix.Application.Common.Context;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }
    Guid? CausationId { get; }

    void Set(Guid correlationId, Guid? causationId = null);
    void Clear();
}
