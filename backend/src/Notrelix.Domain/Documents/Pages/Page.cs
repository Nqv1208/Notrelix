using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Documents.Pages;

public class Page : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Icon { get; private set; } = "📄";
    public string? CoverImage { get; private set; }
    public PageStatus Status { get; private set; }
    public PageVisibility Visibility { get; private set; }

    private Page() : base() { }

    public static Page Create(Guid workspaceId, string title, Guid createdBy, DateTimeOffset createdAt, Guid? parentId = null)
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

        page.SetAuditOnCreate(createdBy, createdAt);
        page.AddDomainEvent(new PageCreatedDomainEvent(workspaceId, page.Id, page.Title, createdBy, createdAt));

        return page;
    }

    public void Rename(string newTitle, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == PageStatus.Archived)
            throw new BusinessRuleException("Cannot rename an archived page.");
        Guard.NotNullOrWhiteSpace(newTitle);

        var oldTitle = Title;
        if (Title == newTitle.Trim()) return;

        Title = newTitle.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new PageRenamedDomainEvent(WorkspaceId, Id, oldTitle, Title, updatedBy, updatedAt));
    }

    public void Move(Guid? newParentId, Guid updatedBy, DateTimeOffset updatedAt, Func<Guid, Guid?> getParentId)
    {
        EnsureNotDeleted();
        if (Status == PageStatus.Archived)
            throw new BusinessRuleException("Cannot move an archived page.");
        if (ParentId == newParentId) return;

        if (newParentId.HasValue)
        {
            Rules.PageTreeRules.EnsureNoCycle(Id, newParentId.Value, getParentId);
        }

        var oldParentId = ParentId;
        ParentId = newParentId;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new PageMovedDomainEvent(WorkspaceId, Id, oldParentId, ParentId, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (Status == PageStatus.Archived) return;

        Status = PageStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        AddDomainEvent(new PageArchivedDomainEvent(WorkspaceId, Id, archivedBy, archivedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = PageStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new PageSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = PageStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new PageRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
