using Notrelix.Domain.Workspaces.Rules;
using Notrelix.Domain.Workspaces.Spaces.Events;
namespace Notrelix.Domain.Workspaces.Spaces;

public class Space : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public SpaceVisibility Visibility { get; private set; }
    public SpaceStatus Status { get; private set; }
    public SpaceType SpaceType { get; private set; } = SpaceType.Folder;

    /// <summary>Parent Space in the same Workspace; null for a root Space (WSP-021).</summary>
    public Guid? ParentSpaceId { get; private set; }

    /// <summary>Order among the non-deleted siblings under the same parent.</summary>
    public FractionalIndex Position { get; private set; } = FractionalIndex.Initial();

    private readonly List<SpaceMember> _members = new();

    /// <summary>User and Team entries with a Space role (WSP-024).</summary>
    public IReadOnlyCollection<SpaceMember> Members => _members.AsReadOnly();

    private Space() : base() { }

    public static Space Create(
        Guid accountId,
        Guid workspaceId,
        string name,
        SpaceVisibility visibility,
        Guid createdBy,
        DateTimeOffset createdAt,
        SpaceType spaceType = SpaceType.Folder,
        string? description = null,
        SpacePlacement? placement = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 160);
        Guard.NotEmpty(createdBy);

        var normalizedName = name.Trim();
        var space = new Space
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = normalizedName,
            Description = description?.Trim(),
            Visibility = visibility,
            Status = SpaceStatus.Active,
            SpaceType = spaceType
        };

        if (placement is not null)
        {
            if (placement.Parent is { } parent)
                SpaceRules.EnsureValidParent(space.Id, workspaceId, parent);
            SpaceRules.EnsureUniqueAmongSiblings(normalizedName, placement.SiblingNames);
            space.ParentSpaceId = placement.Parent?.SpaceId;
            space.Position = placement.Position;
        }

        space.SetAuditOnCreate(createdBy, createdAt);

        // WSP-024: the creator is the first Space Admin.
        space._members.Add(SpaceMember.Create(
            accountId, workspaceId, space.Id, SpaceMemberSubjectType.User, createdBy, SpaceRole.Admin, createdBy, createdAt));

        space.RaiseDomainEvent(new SpaceCreatedDomainEvent(space.Id, accountId, workspaceId, space.Name, createdBy, createdAt));
        space.RaiseDomainEvent(new SpaceMemberAddedDomainEvent(
            accountId, workspaceId, space.Id, SpaceMemberSubjectType.User, createdBy, SpaceRole.Admin, createdBy, createdAt));

        return space;
    }

    /// <param name="siblingNames">
    /// Names of the other non-deleted Spaces under the same parent, loaded by
    /// Application under the hierarchy lock (WSP-021 sibling uniqueness).
    /// </param>
    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt, IReadOnlyCollection<string>? siblingNames = null)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);
        Guard.MaxLength(newName, 160);
        Guard.NotEmpty(updatedBy);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotRenameArchived, "Cannot rename an archived space.");

        var normalizedName = newName.Trim();
        if (Name == normalizedName) return;

        if (siblingNames is not null)
            SpaceRules.EnsureUniqueAmongSiblings(normalizedName, siblingNames);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldName = Name;
        Name = normalizedName;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceRenamedDomainEvent(AccountId, WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void UpdateDescription(string? newDescription, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotUpdateDescriptionArchived, "Cannot update description of an archived space.");

        var normalized = string.IsNullOrWhiteSpace(newDescription)
            ? null
            : newDescription.Trim();

        if (normalized is not null)
            Guard.MaxLength(normalized, 1024);

        if (Description == normalized) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldDescription = Description;
        Description = normalized;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceDescriptionUpdatedDomainEvent(
            AccountId, WorkspaceId, Id, oldDescription, Description, updatedBy, updatedAt));
    }

    public void ChangeVisibility(SpaceVisibility newVisibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotChangeVisibilityArchived, "Cannot change visibility of an archived space.");

        if (Visibility == newVisibility) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldVisibility = Visibility;
        Visibility = newVisibility;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceVisibilityChangedDomainEvent(
            AccountId, WorkspaceId, Id, oldVisibility, newVisibility, updatedBy, updatedAt));
    }

    public void ChangeType(SpaceType newType, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotChangeTypeArchived, "Cannot change type of an archived space.");

        if (SpaceType == newType) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldType = SpaceType;
        SpaceType = newType;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceTypeChangedDomainEvent(
            AccountId, WorkspaceId, Id, oldType, newType, updatedBy, updatedAt));
    }

    /// <param name="content">Child Spaces and placed resources (WSP-022: no cascade).</param>
    public void Archive(Guid archivedBy, DateTimeOffset archivedAt, SpaceContentFact content)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        Guard.NotNull(content);

        if (Status == SpaceStatus.Archived) return;

        SpaceRules.EnsureCanArchive(content);

        var audit = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = SpaceStatus.Archived;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    /// <param name="parentIsActive">Whether the parent (if any) is active; true for a root Space.</param>
    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt, bool parentIsActive)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);

        if (Status == SpaceStatus.Active) return;

        if (Status != SpaceStatus.Archived)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_CannotUnarchiveNonArchived,
                "Only an archived space can be unarchived.");

        SpaceRules.EnsureParentActiveForReactivation(parentIsActive);

        var audit = PrepareAuditUpdate(unarchivedBy, unarchivedAt);
        Status = SpaceStatus.Active;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceUnarchivedDomainEvent(
            AccountId, WorkspaceId, Id, unarchivedBy, unarchivedAt));
    }

    /// <param name="content">Child Spaces and placed resources (WSP-022: no cascade).</param>
    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, SpaceContentFact content, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        Guard.NotNull(content);
        if (IsDeleted) return;

        SpaceRules.EnsureCanDelete(content);

        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new SpaceDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, Status, deletedAt));
    }

    /// <param name="placement">
    /// The Space's current parent (as an Application-supplied fact; null for a
    /// root Space), the sibling position it returns to (its previous key when
    /// still free, otherwise a free key) and the names of the non-deleted siblings.
    /// </param>
    public void Restore(Guid restoredBy, DateTimeOffset restoredAt, SpacePlacement placement)
    {
        Guard.NotEmpty(restoredBy);
        Guard.NotNull(placement);
        if (!IsDeleted) return;

        if (placement.Parent?.SpaceId != ParentSpaceId)
            throw new ArgumentException("The restore placement must describe the space's current parent.", nameof(placement));

        if (placement.Parent is { } parent)
        {
            SpaceRules.EnsureValidParent(Id, WorkspaceId, parent);
        }

        SpaceRules.EnsureUniqueAmongSiblings(Name, placement.SiblingNames);
        Position = placement.Position;

        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new SpaceRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, Status, restoredAt));
    }

    /// <summary>
    /// Moves this Space (with its whole subtree) under another parent or to the
    /// Workspace root, at the given sibling position (WSP-021). A move to the
    /// current parent and position is a semantic no-op.
    /// </summary>
    public void Move(SpacePlacement placement, Guid movedBy, DateTimeOffset movedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(placement);
        Guard.NotEmpty(movedBy);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotMoveArchived, "Cannot move an archived space.");

        if (placement.Parent is { } parent)
            SpaceRules.EnsureValidParent(Id, WorkspaceId, parent);

        var newParentId = placement.Parent?.SpaceId;
        if (newParentId == ParentSpaceId && placement.Position.Equals(Position)) return;

        if (newParentId != ParentSpaceId)
            SpaceRules.EnsureUniqueAmongSiblings(Name, placement.SiblingNames);

        var audit = PrepareAuditUpdate(movedBy, movedAt);
        var oldParentId = ParentSpaceId;
        ParentSpaceId = newParentId;
        Position = placement.Position;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceMovedDomainEvent(
            AccountId, WorkspaceId, Id, oldParentId, newParentId, Position.Value, movedBy, movedAt));
    }

    /// <summary>
    /// Adds a User or Team entry. The subject's eligibility (active member /
    /// active Team of this Workspace) is validated by Application before the
    /// call. Re-adding the same subject with the same role is a no-op; with a
    /// different role it is rejected (use <see cref="ChangeMemberRole"/>).
    /// </summary>
    public void AddMember(SpaceMemberSubjectType subjectType, Guid subjectId, SpaceRole role, Guid addedBy, DateTimeOffset addedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(subjectId);
        Guard.NotEmpty(addedBy);
        EnsureMembersEditable();

        var existing = FindMember(subjectType, subjectId);
        if (existing is not null)
        {
            if (existing.Role == role) return;
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_MemberAlreadyExists,
                "This member already belongs to the space with another role. Change the role instead.");
        }

        var audit = PrepareAuditUpdate(addedBy, addedAt);
        _members.Add(SpaceMember.Create(AccountId, WorkspaceId, Id, subjectType, subjectId, role, addedBy, addedAt));
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceMemberAddedDomainEvent(AccountId, WorkspaceId, Id, subjectType, subjectId, role, addedBy, addedAt));
    }

    /// <summary>Removes an entry; removing an absent subject is a no-op.</summary>
    public void RemoveMember(SpaceMemberSubjectType subjectType, Guid subjectId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(subjectId);
        Guard.NotEmpty(removedBy);
        EnsureMembersEditable();

        var member = FindMember(subjectType, subjectId);
        if (member is null) return;

        SpaceAdminRules.EnsureCanRemove(member.Role, AdminCount());

        var audit = PrepareAuditUpdate(removedBy, removedAt);
        _members.Remove(member);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceMemberRemovedDomainEvent(AccountId, WorkspaceId, Id, subjectType, subjectId, removedBy, removedAt));
    }

    public void ChangeMemberRole(SpaceMemberSubjectType subjectType, Guid subjectId, SpaceRole newRole, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(subjectId);
        Guard.NotEmpty(updatedBy);
        EnsureMembersEditable();

        var member = FindMember(subjectType, subjectId)
            ?? throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_MemberNotFound, "This subject is not a member of the space.");

        if (member.Role == newRole) return;

        SpaceAdminRules.EnsureCanChangeRole(member.Role, newRole, AdminCount());

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldRole = member.Role;
        member.ChangeRole(newRole, updatedBy, updatedAt);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, subjectType, subjectId, oldRole, newRole, updatedBy, updatedAt));
    }

    private SpaceMember? FindMember(SpaceMemberSubjectType subjectType, Guid subjectId) =>
        _members.FirstOrDefault(m => m.Addresses(subjectType, subjectId));

    private int AdminCount() => _members.Count(m => m.Role == SpaceRole.Admin);

    private void EnsureMembersEditable()
    {
        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_CannotChangeMembersArchived,
                "Cannot change the members of an archived space.");
    }
}
