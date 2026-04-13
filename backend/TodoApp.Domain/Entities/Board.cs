using TodoApp.Domain.Common;

namespace TodoApp.Domain.Entities;

public class Board : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Background { get; private set; } = "{}";
    public string Visibility { get; private set; } = "workspace";
    public bool IsArchived { get; private set; }

    public Workspace Workspace { get; private set; } = null!;

    private Board() : base() { }

    public static Board Create(Guid workspaceId, Guid createdBy, string title)
    {
        return new Board
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdBy,
            Title = title.Trim()
        };
    }
}
