using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Domain.Workspaces.Rules;
namespace Notrelix.Domain.Workspaces.Members;

public class WorkspaceMember : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public WorkspaceMemberStatus Status { get; private set; }

    /// <summary>
    /// The person's title inside this Workspace (WSP member profile). Workspace-scoped and
    /// optional: null means none is set. Editing is a profile change, not a membership or
    /// authorization change.
    /// </summary>
    public string? JobTitle { get; private set; }

    public const int MaxJobTitleLength = 100;

    /// <summary>
    /// True when the member takes part as an internal colleague: participation is active and the
    /// role is not Guest. Guests are external participants, so colleague-only facts such as another
    /// person's opted-in phone are neither shown to a guest nor shown for a guest.
    /// </summary>
    public bool IsInternalParticipant => Status == WorkspaceMemberStatus.Active && Role != WorkspaceRole.Guest;

    private WorkspaceMember() : base() { }

    public static WorkspaceMember Create(Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role, Guid addedBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(addedBy);

        var member = new WorkspaceMember
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            Status = WorkspaceMemberStatus.Active
        };

        member.SetAuditOnCreate(addedBy, createdAt);
        member.RaiseDomainEvent(new WorkspaceMemberAddedDomainEvent(accountId, workspaceId, member.Id, userId, role, addedBy, createdAt));
        return member;
    }

    public void ChangeRole(
        WorkspaceRole newRole,
        Guid updatedBy,
        int activeOwnerCount,
        DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);

        if (newRole == WorkspaceRole.Owner)
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Member_CannotDirectlyAssignOwner,
                "Ownership must be transferred through the ownership transfer workflow.");
        }

        if (Status != WorkspaceMemberStatus.Active)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotChangeRoleOfInactive, "Cannot change role of an inactive or suspended member.");
        }

        WorkspaceOwnerRules.EnsureCanDowngradeOwner(Role, newRole, activeOwnerCount);

        if (Role == newRole) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldRole = Role;
        Role = newRole;

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, oldRole, newRole, updatedBy, updatedAt));
    }

    public void PromoteToOwner(Guid promotedBy, DateTimeOffset promotedAt)
    {
        Guard.NotEmpty(promotedBy);

        if (Status != WorkspaceMemberStatus.Active)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotPromoteInactiveToOwner, "Cannot promote an inactive member to owner.");

        if (Role == WorkspaceRole.Owner) return;

        var audit = PrepareAuditUpdate(promotedBy, promotedAt);
        var oldRole = Role;
        Role = WorkspaceRole.Owner;

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, oldRole, WorkspaceRole.Owner, promotedBy, promotedAt));
    }

    public void Suspend(
        Guid updatedBy,
        DateTimeOffset updatedAt,
        int activeOwnerCount)
    {
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceMemberStatus.Removed)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotSuspendRemoved, "Cannot suspend a removed member.");

        if (Status == WorkspaceMemberStatus.Suspended) return;

        WorkspaceOwnerRules.EnsureCanSuspendOwner(Role, activeOwnerCount);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = WorkspaceMemberStatus.Suspended;

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberSuspendedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public void Activate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceMemberStatus.Removed)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotActivateRemoved, "Cannot activate a removed member.");
        }

        if (Status == WorkspaceMemberStatus.Active) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = WorkspaceMemberStatus.Active;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberActivatedDomainEvent(AccountId, WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public void Remove(int activeOwnerCount, Guid removedBy, DateTimeOffset removedAt, string? reason = null)
    {
        Guard.NotEmpty(removedBy);

        if (Status == WorkspaceMemberStatus.Removed) return;

        WorkspaceOwnerRules.EnsureCanRemoveOwner(Role, activeOwnerCount);

        var audit = PrepareAuditUpdate(removedBy, removedAt);
        Status = WorkspaceMemberStatus.Removed;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRemovedDomainEvent(AccountId, WorkspaceId, Id, UserId, removedBy, removedAt));
    }

    /// <summary>
    /// Sets or clears the member's Workspace job title. The value is trimmed and inner runs of
    /// whitespace are collapsed to one space; a blank value clears the title. Control characters
    /// and values over <see cref="MaxJobTitleLength"/> are rejected before any state changes. A
    /// request that leaves the stored value unchanged is a semantic no-op (no version bump, no
    /// audit). A removed member's participation has ended, so its profile is not editable.
    /// Returns true when state changed.
    /// </summary>
    public bool ChangeJobTitle(string? jobTitle, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceMemberStatus.Removed)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotEditRemovedProfile, "Cannot edit the profile of a removed member.");
        }

        var normalized = NormalizeJobTitle(jobTitle);
        if (string.Equals(normalized, JobTitle, StringComparison.Ordinal)) return false;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        JobTitle = normalized;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        return true;
    }

    /// <summary>Normalizes a job title the same way <see cref="ChangeJobTitle"/> stores it; null when blank.</summary>
    public static string? NormalizeJobTitle(string? jobTitle)
    {
        if (string.IsNullOrWhiteSpace(jobTitle)) return null;

        if (jobTitle.Any(char.IsControl))
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_InvalidJobTitle, "A job title cannot contain control characters or line breaks.");
        }

        var collapsed = string.Join(' ', jobTitle.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length > MaxJobTitleLength)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_JobTitleTooLong, $"A job title is at most {MaxJobTitleLength} characters.");
        }

        return collapsed;
    }
}
