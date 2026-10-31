using Notrelix.Domain.Governance.Roles.Events;
namespace Notrelix.Domain.Governance.Roles;

/// <summary>
/// An Account-scoped role definition: a built-in base (Admin, Member or Guest, fixed at creation) plus the
/// workspace-level actions it adds and the actions it removes. It is assigned per workspace through
/// <see cref="MemberRoleAssignment"/>; assigning never changes this aggregate or its version.
/// </summary>
public class CustomRole : AggregateRoot, IAccountScoped
{
    public const int NameMaxLength = 60;
    public const int DescriptionMaxLength = 500;

    public Guid AccountId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public CustomRoleBase BaseRole { get; private set; }
    public CustomRoleStatus Status { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<CustomRolePermission> _permissions = new();
    public IReadOnlyCollection<CustomRolePermission> Permissions => _permissions.AsReadOnly();

    public IReadOnlyCollection<string> AddedActions => _permissions.Where(p => p.IsAllowed).Select(p => p.Action).Order().ToArray();
    public IReadOnlyCollection<string> RemovedActions => _permissions.Where(p => !p.IsAllowed).Select(p => p.Action).Order().ToArray();

    private CustomRole() : base() { }

    public static CustomRole Create(
        Guid accountId,
        string name,
        string? description,
        CustomRoleBase baseRole,
        IReadOnlyCollection<string> added,
        IReadOnlyCollection<string> removed,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        var cleanName = CleanName(name);
        Guard.MaxLength(description, DescriptionMaxLength);

        var role = new CustomRole
        {
            AccountId = accountId,
            Name = cleanName,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            BaseRole = baseRole,
            Status = CustomRoleStatus.Active
        };
        role.ReplaceActions(added, removed);
        role.SetAuditOnCreate(createdBy, createdAt);
        role.RaiseDomainEvent(new CustomRoleCreatedDomainEvent(accountId, role.Id, role.Name, createdBy, createdAt));
        return role;
    }

    /// <summary>
    /// Changes name, description and the action sets together. Returns false for a semantic no-op
    /// (nothing differs), which bumps no version and raises no event.
    /// </summary>
    public bool Revise(
        string name,
        string? description,
        IReadOnlyCollection<string> added,
        IReadOnlyCollection<string> removed,
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        if (Status == CustomRoleStatus.Archived)
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Role_Archived, "An archived role cannot be changed.");

        var cleanName = CleanName(name);
        Guard.MaxLength(description, DescriptionMaxLength);
        var cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        var sameActions = added.ToHashSet().SetEquals(AddedActions) && removed.ToHashSet().SetEquals(RemovedActions);
        if (cleanName == Name && cleanDescription == Description && sameActions)
            return false;

        // Validate the whole prospective state before any mutation (failure atomicity).
        var prospective = Validated(added, removed);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = cleanName;
        Description = cleanDescription;
        if (!sameActions)
        {
            _permissions.Clear();
            _permissions.AddRange(prospective.Select(p => CustomRolePermission.Create(Id, p.Action, p.Allowed)));
        }
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleUpdatedDomainEvent(AccountId, Id, updatedBy, updatedAt));
        return true;
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        if (Status == CustomRoleStatus.Archived) return;

        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = CustomRoleStatus.Archived;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleArchivedDomainEvent(AccountId, Id, archivedBy, archivedAt));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt)
    {
        if (Status != CustomRoleStatus.Archived) return;

        var pending = PrepareAuditUpdate(activatedBy, activatedAt);
        Status = CustomRoleStatus.Active;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleActivatedDomainEvent(AccountId, Id, activatedBy, activatedAt));
    }

    private static string CleanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Role_NameRequired, "A role needs a name.");
        var clean = name.Trim();
        if (clean.Length > NameMaxLength)
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Role_NameTooLong, $"A role name has at most {NameMaxLength} characters.");
        return clean;
    }

    private void ReplaceActions(IReadOnlyCollection<string> added, IReadOnlyCollection<string> removed)
    {
        _permissions.Clear();
        _permissions.AddRange(Validated(added, removed).Select(p => CustomRolePermission.Create(Id, p.Action, p.Allowed)));
    }

    private static List<(string Action, bool Allowed)> Validated(IReadOnlyCollection<string> added, IReadOnlyCollection<string> removed)
    {
        var addedSet = added.Select(a => a.Trim()).Where(a => a.Length > 0).ToHashSet();
        var removedSet = removed.Select(a => a.Trim()).Where(a => a.Length > 0).ToHashSet();
        if (addedSet.Count == 0 && removedSet.Count == 0)
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Role_NoChanges, "A role must add or remove at least one action.");
        if (addedSet.Overlaps(removedSet))
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Role_ActionConflict, "An action cannot be both added and removed.");
        return addedSet.Order().Select(a => (a, true)).Concat(removedSet.Order().Select(a => (a, false))).ToList();
    }
}
