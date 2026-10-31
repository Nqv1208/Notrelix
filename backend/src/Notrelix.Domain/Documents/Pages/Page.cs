using Notrelix.Domain.Documents.Pages.Events;
namespace Notrelix.Domain.Documents.Pages;

public class Page : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Workspaces Space this page is placed in (null = Workspace root). A nested
    /// page follows the Space of its root page (docs/product/workspaces.md §79).
    /// </summary>
    public Guid? SpaceId { get; private set; }
    public string Title { get; private set; } = null!;
    public const string DefaultIcon = "📄";
    public string Icon { get; private set; } = DefaultIcon;
    public string? CoverImage { get; private set; }
    public PageStatus Status { get; private set; }
    public PageVisibility Visibility { get; private set; }

    private Page() : base() { }

    /// <param name="icon">The icon the page starts with (a page made from a template takes the template's); blank keeps the default.</param>
    /// <param name="spaceId">
    /// Space placement; for a nested page Application passes the parent's Space
    /// (a nested page follows its root page, docs/product/workspaces.md §79).
    /// </param>
    public static Page Create(Guid accountId, Guid workspaceId, string title, Guid createdBy, DateTimeOffset createdAt, Guid? parentId = null, Guid? spaceId = null, string? icon = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 500);
        if (!string.IsNullOrWhiteSpace(icon)) Guard.MaxLength(icon.Trim(), 50);

        var page = new Page
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ParentId = parentId,
            SpaceId = spaceId,
            Title = title.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? DefaultIcon : icon.Trim(),
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

    public void Move(Guid? newParentId, Guid updatedBy, DateTimeOffset updatedAt, Func<Guid, Guid?> getParentId) =>
        Move(newParentId, SpaceId, updatedBy, updatedAt, getParentId);

    /// <summary>
    /// Puts the page under another parent (null = a root page). A nested page follows the Space of its root
    /// page, so Application passes the Space the new parent is in (the page's own Space when it becomes a root);
    /// the descendants follow through <see cref="FollowRootSpace"/>. One change, one version bump. The same
    /// place is a semantic no-op.
    /// </summary>
    public void Move(Guid? newParentId, Guid? newSpaceId, Guid updatedBy, DateTimeOffset updatedAt, Func<Guid, Guid?> getParentId)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == PageStatus.Archived)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotMoveArchived, "Cannot move an archived page.");
        if (ParentId == newParentId && SpaceId == newSpaceId) return;

        if (newParentId.HasValue)
        {
            Rules.PageTreeRules.EnsureNoCycle(Id, newParentId.Value, getParentId);
        }
        if (newSpaceId is { } space) Guard.NotEmpty(space);

        var oldParentId = ParentId;
        var oldSpaceId = SpaceId;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentId = newParentId;
        SpaceId = newSpaceId;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new PageMovedDomainEvent(AccountId, WorkspaceId, Id, oldParentId, ParentId, updatedBy, updatedAt));
        if (oldSpaceId != SpaceId)
            RaiseDomainEvent(new PageSpaceChangedDomainEvent(AccountId, WorkspaceId, Id, oldSpaceId, SpaceId, updatedBy, updatedAt));
    }

    /// <summary>
    /// Places a root page in a Workspaces Space, or back at the Workspace root
    /// (null). A nested page cannot be placed on its own: it follows its root
    /// page, whose move re-places the whole subtree through
    /// <see cref="FollowRootSpace"/>. Same Space = no-op.
    /// </summary>
    public void PlaceInSpace(Guid? spaceId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (ParentId is not null)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_NestedPageFollowsRootSpace, "A nested page follows the space of its root page; move the root page instead.");
        if (Status == PageStatus.Archived)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotChangeSpaceArchived, "Cannot move an archived page to another space.");

        ChangeSpace(spaceId, updatedBy, updatedAt);
    }

    /// <summary>Re-places a descendant page with its root page's Space (subtree move).</summary>
    public void FollowRootSpace(Guid? spaceId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        ChangeSpace(spaceId, updatedBy, updatedAt);
    }

    private void ChangeSpace(Guid? spaceId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (spaceId is { } id) Guard.NotEmpty(id);
        if (SpaceId == spaceId) return;

        var oldSpaceId = SpaceId;
        // A page in the trash is dated by the time it went there: a restore and a delete find the pages trashed
        // together by that time, so a Space move that carries the page along must not change it.
        if (Status == PageStatus.Archived)
        {
            SpaceId = spaceId;
        }
        else
        {
            var pending = PrepareAuditUpdate(updatedBy, updatedAt);
            SpaceId = spaceId;
            ApplyAuditUpdate(pending);
        }

        IncrementVersion();
        RaiseDomainEvent(new PageSpaceChangedDomainEvent(AccountId, WorkspaceId, Id, oldSpaceId, SpaceId, updatedBy, updatedAt));
    }

    /// <summary>
    /// Announces one successful save of this page's blocks (and the mentions it added) for the reactions that
    /// follow the commit. It changes no page state: the page's version counts changes of the page itself, not
    /// edits of its blocks.
    /// </summary>
    public void RecordBlocksSaved(Guid savedBy, DateTimeOffset savedAt, IReadOnlyList<BlockMentionAdded> mentions)
    {
        EnsureNotDeleted();
        Rules.PageRules.EnsureCanEdit(Status);
        Guard.NotEmpty(savedBy);
        Guard.NotNull(mentions);
        RaiseDomainEvent(new PageBlocksSavedDomainEvent(AccountId, WorkspaceId, Id, Title, savedBy, mentions, savedAt));
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

    /// <summary>
    /// Takes an archived (trashed) page back out of the trash. An active page is
    /// a semantic no-op (no version bump, no second event).
    /// </summary>
    /// <param name="detachFromParent">
    /// The parent is no longer an active page (trashed or deleted): the page comes back at the root of its
    /// Space instead of under it. Application decides this from the parent's current state.
    /// </param>
    public void Unarchive(Guid restoredBy, DateTimeOffset restoredAt, bool detachFromParent = false)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(restoredBy);
        if (Status == PageStatus.Active) return;

        var oldParentId = detachFromParent ? ParentId : null;
        var pending = PrepareAuditUpdate(restoredBy, restoredAt);
        Status = PageStatus.Active;
        if (oldParentId is not null) ParentId = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new PageRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
        if (oldParentId is not null)
            RaiseDomainEvent(new PageMovedDomainEvent(AccountId, WorkspaceId, Id, oldParentId, null, restoredBy, restoredAt));
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
