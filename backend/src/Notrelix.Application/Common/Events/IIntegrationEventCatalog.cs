namespace Notrelix.Application.Common.Events;

/// <summary>
/// Runtime type resolution for public integration events (IAREQ131).
/// Resolution is keyed by the compound contract identity (Name, Version):
/// same logical name may coexist as multiple versions; unknown names and
/// unsupported versions fail deterministically with no fallback.
/// </summary>
public interface IIntegrationEventCatalog
{
    Type Resolve(EventContractKey key);
    bool TryResolve(EventContractKey key, out Type type);
}
