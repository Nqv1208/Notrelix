namespace Notrelix.Domain.Tests.Freeze;

public enum MutationScenario
{
    Valid,
    NoOp,
    Invalid,
    FailureAtomicity,
    Audit,
    Version,
    Event,
    Scope,
    Lifecycle
}
