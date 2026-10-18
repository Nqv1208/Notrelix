namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>Constraint-name based ordering conflict classification (WM-V3-ORDER-005).</summary>
public sealed class PostgresOrderingConflictClassifier : IOrderingConflictClassifier
{
    public bool IsOrderingConflict(Exception exception) => WorkOrderingConstraints.IsOrderingViolation(exception);
}
