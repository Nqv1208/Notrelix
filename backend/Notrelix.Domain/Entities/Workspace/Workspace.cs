using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;
using Notrelix.Domain.ValueObjects;

namespace Notrelix.Domain.Entities.Workspace;

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

        return workspace;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên workspace không được để trống", nameof(name));

        Name = name.Trim();
    }

    public void UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug không được để trống", nameof(slug));

        Slug = slug.Trim().ToLowerInvariant();
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
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId)
            throw new InvalidOperationException("Không thể xóa Owner khỏi workspace");

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            _members.Remove(member);
        }
    }

    public void UpdateMemberRole(Guid userId, WorkspaceRole newRole)
    {
        if (userId == OwnerId && newRole != WorkspaceRole.Owner)
            throw new InvalidOperationException("Không thể thay đổi role của Owner");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User không phải là thành viên của workspace");

        member.UpdateRole(newRole);
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
}
