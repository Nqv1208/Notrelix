using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Workspace;
using Notrelix.Domain.ValueObjects;

namespace Notrelix.Domain.Entities.Workspaces;

/// <summary>
/// Entity đại diện cho không gian làm việc (Personal hoặc Team)
/// </summary>
public class Workspace : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsPersonal { get; private set; }
    public Guid OwnerId { get; private set; }
    public WorkspacePlan Plan { get; private set; } = WorkspacePlan.Free;
    public string Settings { get; private set; } = "{}";
    public Icon Icon { get; private set; } = null!;
    public string? CoverUrl { get; private set; }
    public bool IsArchived { get; private set; }

    // Navigation - Members
    private readonly List<WorkspaceMember> _members = new();
    public IReadOnlyCollection<WorkspaceMember> Members => _members.AsReadOnly();

    private Workspace() : base() { }

    public static Workspace CreatePersonal(string name, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên workspace không được để trống", nameof(name));

        var workspace = new Workspace
        {
            Name = name.Trim(),
            Slug = ValueObjects.Slug.GenerateFromName(name),
            IsPersonal = true,
            OwnerId = ownerId,
            Icon = Icon.Default,
            IsArchived = false
        };

        // Owner tự động là member với role Owner
        workspace._members.Add(WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner));

        workspace.AddDomainEvent(new WorkspaceCreatedEvent(workspace.Id, workspace.Name, workspace.OwnerId, workspace.IsPersonal));

        return workspace;
    }

    public static Workspace CreateTeam(string name, Guid ownerId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên workspace không được để trống", nameof(name));

        var workspace = new Workspace
        {
            Name = name.Trim(),
            Slug = ValueObjects.Slug.GenerateFromName(name),
            Description = description?.Trim(),
            IsPersonal = false,
            OwnerId = ownerId,
            Icon = Icon.FromEmoji("👥"),
            IsArchived = false
        };

        workspace._members.Add(WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner));

        workspace.AddDomainEvent(new WorkspaceCreatedEvent(workspace.Id, workspace.Name, workspace.OwnerId, workspace.IsPersonal));

        return workspace;
    }

    public void UpdateName(string name)
    {
        Rename(name, Guid.Empty);
    }

    public void Rename(string name, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên workspace không được để trống", nameof(name));

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        AddDomainEvent(new WorkspaceUpdatedEvent(Id, updatedBy, Name, Slug));
    }

    public void UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug không được để trống", nameof(slug));

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (Slug == normalizedSlug) return;

        Slug = normalizedSlug;
        AddDomainEvent(new WorkspaceUpdatedEvent(Id, Guid.Empty, Name, Slug));
    }

    public void UpdateSettings(string settingsJson)
    {
        Settings = string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson;
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
    }

    public void UpdateIcon(Icon icon)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
    }

    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;

    public void UpdatePlan(WorkspacePlan plan)
    {
        Plan = plan;
    }

    public WorkspaceMember AddMember(Guid userId, WorkspaceRole role = WorkspaceRole.Member)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User đã là thành viên của workspace");

        if (IsPersonal && role != WorkspaceRole.Guest)
            throw new InvalidOperationException("Personal workspace chỉ cho phép thêm Guest");

        var member = WorkspaceMember.Create(Id, userId, role);
        _members.Add(member);
        AddDomainEvent(new WorkspaceMemberJoinedEvent(Id, userId, role, member.InvitedBy));
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        RemoveMember(userId, Guid.Empty);
    }

    public void RemoveMember(Guid userId, Guid removedBy)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            return;

        if (member.IsOwner && OwnerCount <= 1)
            throw new InvalidOperationException("Không thể xóa Owner khỏi workspace");

        _members.Remove(member);
        AddDomainEvent(new WorkspaceMemberRemovedEvent(Id, userId, member.Role, removedBy));

        if (OwnerId == userId)
        {
            OwnerId = _members.First(m => m.IsOwner).UserId;
        }
    }

    public void UpdateMemberRole(Guid userId, WorkspaceRole newRole)
    {
        ChangeMemberRole(userId, newRole, Guid.Empty);
    }

    public void ChangeMemberRole(Guid userId, WorkspaceRole newRole, Guid changedBy)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User không phải là thành viên của workspace");

        if (member.IsOwner && newRole != WorkspaceRole.Owner && OwnerCount <= 1)
            throw new InvalidOperationException("Không thể thay đổi role của Owner");

        var oldRole = member.Role;
        if (oldRole == newRole) return;

        member.UpdateRole(newRole);
        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(Id, userId, oldRole, newRole, changedBy));

        if (OwnerId == userId && newRole != WorkspaceRole.Owner)
        {
            OwnerId = _members.First(m => m.IsOwner).UserId;
        }
    }

    public void TransferOwnership(Guid newOwnerId, Guid transferredBy)
    {
        if (transferredBy != OwnerId)
            throw new InvalidOperationException("Chỉ owner hiện tại mới được chuyển quyền sở hữu workspace");

        if (newOwnerId == OwnerId)
            return;

        var currentOwner = _members.FirstOrDefault(m => m.UserId == OwnerId)
            ?? throw new InvalidOperationException("Workspace thiếu owner hiện tại");

        var newOwner = _members.FirstOrDefault(m => m.UserId == newOwnerId)
            ?? throw new InvalidOperationException("Owner mới phải là thành viên của workspace");

        var oldOwnerId = OwnerId;
        var oldTargetRole = newOwner.Role;

        currentOwner.UpdateRole(WorkspaceRole.Admin);
        newOwner.UpdateRole(WorkspaceRole.Owner);
        OwnerId = newOwnerId;

        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(Id, oldOwnerId, WorkspaceRole.Owner, WorkspaceRole.Admin, transferredBy));
        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(Id, newOwnerId, oldTargetRole, WorkspaceRole.Owner, transferredBy));
        AddDomainEvent(new WorkspaceOwnershipTransferredEvent(Id, oldOwnerId, newOwnerId, transferredBy));
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public WorkspaceRole? GetMemberRole(Guid userId) => 
        _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    public bool CanUserEdit(Guid userId)
    {
        var role = GetMemberRole(userId);
        return role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Member;
    }

    public bool CanUserAdmin(Guid userId)
    {
        var role = GetMemberRole(userId);
        return role is WorkspaceRole.Owner or WorkspaceRole.Admin;
    }

    private int OwnerCount => _members.Count(m => m.IsOwner);
}
