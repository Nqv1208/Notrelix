namespace Notrelix.Application.Common.Data;

/// <summary>
/// Recognizes a persistence failure caused by a named ordering-uniqueness guard
/// (SPEC WM-V3-DEC-015, WMREQ156). Infrastructure identifies the guard by its
/// constraint name; callers that commit outside <see cref="IRequestDataSession"/>,
/// such as the automation consumer, use it to surface the stable ordering
/// conflict instead of treating the collision as a generic technical failure.
/// </summary>
public interface IOrderingConflictClassifier
{
    /// <summary>Stable public error code of an ordering conflict (HTTP 409).</summary>
    const string ConflictCode = "work.ordering.conflict";

    bool IsOrderingConflict(Exception exception);
}
