using Notrelix.Domain.Documents.Pages.Events;
namespace Notrelix.Domain.Documents.Pages;

public class Page : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Icon { get; private set; } = "📄";
    public string? CoverImage { get; private set; }
    public PageStatus Status { get; private set; }
    public PageVisibility Visibility { get; private set; }

    private Page() : base() { }

    public static Page Create(Guid accountId, Guid workspaceId, string title, Guid createdBy, DateTimeOffset createdAt, Guid? parentId = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 500);

        var page = new Page
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ParentId = parentId,
            Title = title.Trim(),
            Status = PageStatus.Active,
            Visibility = PageVisibility.Workspace
        };

        page.SetAuditOnCreate(createdBy, createdAt);
        page.RaiseDomainEvent(new PageCreatedDomainEvent(accountId, workspaceId, page.Id, page.Title, createdBy, createdAt));

        return page;
    }

    public void Rename(string newTitle, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == PageStatus.Archived)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotRenameArchived, "Cannot rename an archived page.");
        Guard.NotNullOrWhiteSpace(newTitle);
        Guard.MaxLength(newTitle, 500);

        if (Title == newTitle.Trim()) return;

        var oldTitle = Title;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Title = newTitle.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new PageRenamedDomainEvent(AccountId, WorkspaceId, Id, oldTitle, Title, updatedBy, updatedAt));
    }

    public void Move(Guid? newParentId, Guid updatedBy, DateTimeOffset updatedAt, Func<Guid, Guid?> getParentId)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == PageStatus.Archived)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotMoveArchived, "Cannot move an archived page.");
        if (ParentId == newParentId) return;

        if (newParentId.HasValue)
        {
            Rules.PageTreeRules.EnsureNoCycle(Id, newParentId.Value, getParentId);
        }

        var oldParentId = ParentId;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentId = newParentId;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new PageMovedDomainEvent(AccountId, WorkspaceId, Id, oldParentId, ParentId, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == PageStatus.Archived) return;

        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = PageStatus.Archived;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new PageArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new PageDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new PageRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
