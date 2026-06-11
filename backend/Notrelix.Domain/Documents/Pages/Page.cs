using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Documents.Pages;

public class Page : SoftDeletableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Icon { get; private set; } = "📄";
    public string? CoverImage { get; private set; }
    public PageStatus Status { get; private set; }
    public PageVisibility Visibility { get; private set; }

    private Page() : base() { }

    public static Page Create(Guid workspaceId, string title, Guid createdBy, Guid? parentId = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);

        var page = new Page
        {
            WorkspaceId = workspaceId,
            ParentId = parentId,
            Title = title.Trim(),
            Status = PageStatus.Active,
            Visibility = PageVisibility.Workspace
        };

        page.SetAuditOnCreate(createdBy);
        page.AddDomainEvent(new PageCreatedEvent(workspaceId, page.Id, page.Title, createdBy));

        return page;
    }

    public void Rename(string newTitle, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newTitle);

        var oldTitle = Title;
        if (Title == newTitle.Trim()) return;

        Title = newTitle.Trim();
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new PageRenamedEvent(Id, oldTitle, Title, updatedBy));
    }

    public void Move(Guid? newParentId, Guid updatedBy, Func<Guid, Guid?> getParentId)
    {
        EnsureNotDeleted();
        if (ParentId == newParentId) return;

        if (newParentId.HasValue)
        {
            Rules.PageTreeRules.EnsureNoCycle(Id, newParentId.Value, getParentId);
        }

        var oldParentId = ParentId;
        ParentId = newParentId;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new PageMovedEvent(Id, oldParentId, ParentId, updatedBy));
    }

    public void Archive(Guid archivedBy)
    {
        EnsureNotDeleted();
        if (Status == PageStatus.Archived) return;

        Status = PageStatus.Archived;
        SetAuditOnUpdate(archivedBy);
        AddDomainEvent(new PageArchivedEvent(Id, archivedBy));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = PageStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new PageSoftDeletedEvent(Id, deletedBy));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = PageStatus.Active;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new PageRestoredEvent(Id, restoredBy));
    }
}
