using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;
using Notrelix.Domain.ValueObjects;

namespace Notrelix.Domain.Tests;

public class WorkspaceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreatePersonal_WhenNameIsEmptyOrWhitespace_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var act = () => Workspace.CreatePersonal(invalidName!, ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Tên workspace không được để trống*");
    }

    [Fact]
    public void CreatePersonal_WhenValid_ShouldTrimNameAndGenerateSlugAndAddOwnerAsMember()
    {
        // Arrange
        var name = "   My Personal Todo-App  ";
        var ownerId = Guid.NewGuid();

        // Act
        var workspace = Workspace.CreatePersonal(name, ownerId);

        // Assert
        workspace.Name.Should().Be("My Personal Todo-App");
        workspace.Slug.Should().StartWith("my-personal-todo-app-");
        workspace.IsPersonal.Should().BeTrue();
        workspace.OwnerId.Should().Be(ownerId);
        workspace.IsArchived.Should().BeFalse();
        workspace.Icon.Value.Should().Be("📝"); // Icon.Default has 📝 emoji

        workspace.Members.Should().HaveCount(1);
        var ownerMember = workspace.Members.First();
        ownerMember.UserId.Should().Be(ownerId);
        ownerMember.Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public void CreateTeam_WhenValid_ShouldTrimDescriptionAndAddOwnerAsMember()
    {
        // Arrange
        var name = "Engineering Team";
        var description = "   Software Development Team   ";
        var ownerId = Guid.NewGuid();

        // Act
        var workspace = Workspace.CreateTeam(name, ownerId, description);

        // Assert
        workspace.Name.Should().Be(name);
        workspace.Description.Should().Be("Software Development Team");
        workspace.IsPersonal.Should().BeFalse();
        workspace.OwnerId.Should().Be(ownerId);
        workspace.Icon.Value.Should().Be("👥");

        workspace.Members.Should().HaveCount(1);
        workspace.Members.First().Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public void AddMember_WhenUserAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Team Workspace", ownerId);
        
        // Act
        var act = () => workspace.AddMember(ownerId, WorkspaceRole.Member);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User đã là thành viên của workspace");
    }

    [Fact]
    public void AddMember_WhenWorkspaceIsPersonalAndRoleIsNotGuest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreatePersonal("Personal Workspace", ownerId);
        var guestId = Guid.NewGuid();

        // Act
        var act = () => workspace.AddMember(guestId, WorkspaceRole.Member);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Personal workspace chỉ cho phép thêm Guest");
    }

    [Fact]
    public void AddMember_WhenWorkspaceIsPersonalAndRoleIsGuest_ShouldSucceed()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreatePersonal("Personal Workspace", ownerId);
        var guestId = Guid.NewGuid();

        // Act
        var member = workspace.AddMember(guestId, WorkspaceRole.Guest);

        // Assert
        member.UserId.Should().Be(guestId);
        member.Role.Should().Be(WorkspaceRole.Guest);
        workspace.Members.Should().Contain(member);
    }

    [Fact]
    public void RemoveMember_WhenUserIsOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Team Workspace", ownerId);

        // Act
        var act = () => workspace.RemoveMember(ownerId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Không thể xóa Owner khỏi workspace");
    }

    [Fact]
    public void RemoveMember_WhenUserExistsAndIsNotOwner_ShouldRemoveSuccessfully()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Team Workspace", ownerId);
        var memberId = Guid.NewGuid();
        workspace.AddMember(memberId, WorkspaceRole.Member);

        // Act
        workspace.RemoveMember(memberId);

        // Assert
        workspace.Members.Should().HaveCount(1);
        workspace.IsMember(memberId).Should().BeFalse();
    }

    [Fact]
    public void UpdateMemberRole_WhenUserIsOwnerAndNewRoleIsNotOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Team Workspace", ownerId);

        // Act
        var act = () => workspace.UpdateMemberRole(ownerId, WorkspaceRole.Admin);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Không thể thay đổi role của Owner");
    }

    [Fact]
    public void UpdateMemberRole_WhenUserDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Team Workspace", ownerId);
        var missingUserId = Guid.NewGuid();

        // Act
        var act = () => workspace.UpdateMemberRole(missingUserId, WorkspaceRole.Admin);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User không phải là thành viên của workspace");
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, true, true)]
    [InlineData(WorkspaceRole.Admin, true, true)]
    [InlineData(WorkspaceRole.Member, true, false)]
    [InlineData(WorkspaceRole.Guest, false, false)]
    public void Permissions_ShouldBeCorrectlyMappedForRoles(WorkspaceRole role, bool expectedCanEdit, bool expectedCanAdmin)
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Permissions Test", ownerId);
        
        var userId = Guid.NewGuid();
        if (role == WorkspaceRole.Owner)
        {
            // Owner is automatically added at creation, so use OwnerId
            userId = ownerId;
        }
        else
        {
            workspace.AddMember(userId, role);
        }

        // Act & Assert
        workspace.CanUserEdit(userId).Should().Be(expectedCanEdit);
        workspace.CanUserAdmin(userId).Should().Be(expectedCanAdmin);
    }

    [Fact]
    public void ArchiveAndUnarchive_ShouldModifyStateAsExpected()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("State Workspace", ownerId);

        // Act & Assert (Archive)
        workspace.Archive();
        workspace.IsArchived.Should().BeTrue();

        // Act & Assert (Unarchive)
        workspace.Unarchive();
        workspace.IsArchived.Should().BeFalse();
    }
}
