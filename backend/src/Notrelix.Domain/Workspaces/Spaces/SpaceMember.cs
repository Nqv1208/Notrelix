namespace Notrelix.Domain.Workspaces.Spaces;

/// <summary>
/// One membership entry of a Space: a User or a Team of the same Workspace with
/// a Space role. Owned by the <see cref="Space"/> aggregate; created, changed and
/// removed only through it so the last-Admin rule holds.
/// </summary>
public class SpaceMember : AuditableEntity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public SpaceMemberSubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public SpaceRole Role { get; private set; }

    private SpaceMember() : base() { }

    internal static SpaceMember Create(
        Guid accountId,
        Guid workspaceId,
        Guid spaceId,
        SpaceMemberSubjectType subjectType,
        Guid subjectId,
        SpaceRole role,
        Guid addedBy,
        DateTimeOffset addedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(subjectId);
        Guard.NotEmpty(addedBy);

        var member = new SpaceMember
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Role = role,
        };

        member.SetAuditOnCreate(addedBy, addedAt);
        return member;
    }

    internal bool Addresses(SpaceMemberSubjectType subjectType, Guid subjectId) =>
        SubjectType == subjectType && SubjectId == subjectId;

    internal void ChangeRole(SpaceRole role, Guid updatedBy, DateTimeOffset updatedAt)
    {
        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Role = role;
        ApplyAuditUpdate(audit);
    }
}
