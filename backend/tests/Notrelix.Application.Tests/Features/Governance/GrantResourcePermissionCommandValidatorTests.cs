using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

namespace Notrelix.Application.Tests.Features.Governance;

public class GrantResourcePermissionCommandValidatorTests
{
    [Theory]
    [InlineData("Owner")]
    [InlineData("owner")]
    [InlineData("Viewer")]
    [InlineData("Commenter")]
    public void Grant_AcceptsKnownLevel(string level)
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand("work-management.board", Guid.NewGuid(), "User", Guid.NewGuid(), level);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("None")]
    [InlineData("SuperAdmin")]
    [InlineData("None ")]
    public void Grant_RejectsUnknownOrNoneLevel(string level)
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand("work-management.board", Guid.NewGuid(), "User", Guid.NewGuid(), level);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("WorkspaceRole")]
    [InlineData("Team")]
    [InlineData("PublicLink")]
    [InlineData("ExternalEmail")]
    public void Grant_AcceptsKnownSubjectType(string subjectType)
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand("work-management.board", Guid.NewGuid(), subjectType, Guid.NewGuid(), "Viewer");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Member")]
    [InlineData("Group")]
    public void Grant_RejectsUnknownSubjectType(string subjectType)
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand("work-management.board", Guid.NewGuid(), subjectType, Guid.NewGuid(), "Viewer");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubjectType");
    }

    [Fact]
    public void Grant_RejectsExpirationDate()
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand(
            "work-management.board", Guid.NewGuid(), "User", Guid.NewGuid(), "Viewer", DateTime.UtcNow.AddDays(1));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiresAt");
    }

    [Fact]
    public void Grant_AcceptsNullExpiration()
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand("work-management.board", Guid.NewGuid(), "User", Guid.NewGuid(), "Viewer", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}