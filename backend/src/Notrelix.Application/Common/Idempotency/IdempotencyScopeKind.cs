namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Classifies the tenant scope of an idempotent request for partition construction.
/// </summary>
public enum IdempotencyScopeKind
{
    System,
    Account,
    AccountWorkspace,
    AccountUser,
}
