namespace Notrelix.Application.Features.Operators.Public.Audit;

/// <summary>What an operator action changed: the operated record's identity, the reason, and the values before and after.</summary>
public sealed record OperatorAuditRecord(
    string Action,
    string TargetKind,
    Guid TargetId,
    Guid? AccountId,
    string Reason,
    object? Before,
    object? After);

/// <summary>
/// Records an operator action in the same unit of work as the change it describes: the entry is added to the
/// request's context and committed by the caller's single save, so a change never exists without its audit
/// row and an audit row never describes a change that did not happen. The actor is the current operator.
/// </summary>
public interface IOperatorAuditTrail
{
    /// <summary>Validates the reason first (required, at most 500 characters) and throws before anything is added.</summary>
    void Record(OperatorAuditRecord record);
}
