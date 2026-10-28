namespace Notrelix.Application.Features.WorkManagement.Public.ItemActivity;

/// <summary>
/// Work Management-published facts that the Item history needs to word a change: the name and value mode of a
/// field, the names of its options, and the names of a Board or group. Collaboration records the history
/// row; it never reads Work persistence.
/// </summary>
public interface IWorkItemActivityFacts
{
    /// <summary>The field's facts, or null when it no longer exists in the Workspace.</summary>
    Task<WorkFieldActivityFacts?> FindFieldAsync(Guid workspaceId, Guid fieldId, CancellationToken cancellationToken);

    /// <summary>The Board's title, or null when it no longer exists in the Workspace.</summary>
    Task<string?> FindBoardNameAsync(Guid workspaceId, Guid boardId, CancellationToken cancellationToken);

    /// <summary>The group's title, or null when it no longer exists in the Workspace.</summary>
    Task<string?> FindGroupNameAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken);
}

/// <summary>How a stored value of a field may be shown in the history.</summary>
public enum WorkFieldValueMode
{
    /// <summary>Never copy values: sensitive fields, long text, people, links and computed fields.</summary>
    Hidden,

    /// <summary>A short scalar: text, number, date, checkbox.</summary>
    Plain,

    /// <summary>One option held by id: Select and Status.</summary>
    Option,

    /// <summary>Several options held by id: MultiSelect.</summary>
    OptionList,
}

/// <param name="Name">The field's name now.</param>
/// <param name="IsStatus">True when this is the Board's Status field.</param>
/// <param name="ValueMode">How the field's values may be shown.</param>
/// <param name="OptionNames">The names of a Select/Status/MultiSelect field's options by id.</param>
public sealed record WorkFieldActivityFacts(
    string Name,
    bool IsStatus,
    WorkFieldValueMode ValueMode,
    IReadOnlyDictionary<Guid, string> OptionNames);
