namespace Notrelix.Application.Common.Security;

/// <summary>
/// Resolves resource references across bounded contexts.
/// Replaces direct DbContext access for checking existence/workspace of resources
/// owned by other contexts (Pages, BoardItems, Comments, etc.).
/// </summary>
public interface IResourceReferenceResolver
{
    /// <summary>
    /// Returns the WorkspaceId that owns the given resource, or null if not found.
    /// </summary>
    Task<Guid?> GetWorkspaceIdAsync(Guid resourceId, string resourceType, CancellationToken ct);

    /// <summary>
    /// Checks if the given resource exists and is not deleted.
    /// </summary>
    Task<bool> ExistsAsync(Guid resourceId, string resourceType, CancellationToken ct);

    /// <summary>
    /// Returns AccountId and WorkspaceId for a resource, or null if not found.
    /// Used when a handler needs both identifiers to create domain objects (e.g. AutomationExecution).
    /// </summary>
    Task<AccountContextSnapshot?> GetAccountContextAsync(Guid resourceId, string resourceType, CancellationToken ct);
}

public sealed record AccountContextSnapshot(Guid AccountId, Guid WorkspaceId);

public static class ResourceTypes
{
    public const string Page = "Page";
    public const string Block = "Block";
    public const string BoardItem = "BoardItem";
    public const string Board = "Board";
    public const string Comment = "Comment";
    public const string Attachment = "Attachment";
}