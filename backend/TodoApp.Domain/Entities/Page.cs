using TodoApp.Domain.Common;

namespace TodoApp.Domain.Entities;

public class Page : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? ParentId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; } = "Untitled";
    public string? IconType { get; private set; }
    public string? IconValue { get; private set; }
    public string? CoverUrl { get; private set; }
    public double Position { get; private set; }
    public short Depth { get; private set; }
    public bool IsTemplate { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    public Workspace Workspace { get; private set; } = null!;
    public Page? Parent { get; private set; }
    private readonly List<Page> _children = [];
    public IReadOnlyCollection<Page> Children => _children.AsReadOnly();
    private readonly List<Block> _blocks = [];
    public IReadOnlyCollection<Block> Blocks => _blocks.AsReadOnly();

    private Page() : base() { }

    public static Page Create(Guid workspaceId, Guid createdByUserId, string title, Guid? parentId = null)
    {
        return new Page
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdByUserId,
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim(),
            ParentId = parentId,
            Position = 0,
            Depth = 0
        };
    }
}
