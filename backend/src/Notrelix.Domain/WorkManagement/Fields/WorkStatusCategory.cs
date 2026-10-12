namespace Notrelix.Domain.WorkManagement.Fields;

/// <summary>
/// The lifecycle bucket a Status option belongs to. Only <see cref="Done"/> marks
/// an Item as done for progress and completion. Work-status semantics are local to
/// Work Management; this is not a shared cross-context status.
/// </summary>
public enum WorkStatusCategory
{
    Todo,
    InProgress,
    Review,
    Done
}
