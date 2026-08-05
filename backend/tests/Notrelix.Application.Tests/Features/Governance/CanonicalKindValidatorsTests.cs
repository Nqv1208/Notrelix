using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;
using Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Spec 4.3: commands and queries accept the canonical kind string; the kind is
/// validated by the Application validator, never by the API endpoint.
/// </summary>
public class CanonicalKindValidatorsTests
{
    public static TheoryData<string> ValidKinds => new()
    {
        "work-management.board",
        "work-management.board-item",
        "documents.page",
        "documents.block",
        "governance.role.assigned"
    };

    public static TheoryData<string> InvalidKinds => new()
    {
        "",
        "   ",
        "board",
        "Work-Management.board",
        "work-management..board",
        "work-management.Board",
        "work management.board",
        new string('a', 129)
    };

    [Theory]
    [MemberData(nameof(ValidKinds))]
    public void GrantResourcePermission_AcceptsCanonicalKind(string kind)
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand(kind, Guid.NewGuid(), "User", Guid.NewGuid(), "Viewer");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidKinds))]
    public void GrantResourcePermission_RejectsMalformedKind(string kind)
    {
        var validator = new GrantResourcePermissionCommandValidator();
        var command = new GrantResourcePermissionCommand(kind, Guid.NewGuid(), "User", Guid.NewGuid(), "Viewer");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResourceKind");
    }

    [Theory]
    [MemberData(nameof(ValidKinds))]
    public void RevokeResourcePermission_AcceptsCanonicalKind(string kind)
    {
        var validator = new RevokeResourcePermissionCommandValidator();
        var command = new RevokeResourcePermissionCommand(kind, Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidKinds))]
    public void RevokeResourcePermission_RejectsMalformedKind(string kind)
    {
        var validator = new RevokeResourcePermissionCommandValidator();
        var command = new RevokeResourcePermissionCommand(kind, Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ValidKinds))]
    public void GetResourcePermissions_AcceptsCanonicalKind(string kind)
    {
        var validator = new GetResourcePermissionsQueryValidator();
        var query = new GetResourcePermissionsQuery(kind, Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidKinds))]
    public void GetResourcePermissions_RejectsMalformedKind(string kind)
    {
        var validator = new GetResourcePermissionsQueryValidator();
        var query = new GetResourcePermissionsQuery(kind, Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ValidKinds))]
    public void CreateShareLink_AcceptsCanonicalKind(string kind)
    {
        var validator = new CreateShareLinkCommandValidator();
        var command = new CreateShareLinkCommand(kind, Guid.NewGuid(), "Viewer");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidKinds))]
    public void CreateShareLink_RejectsMalformedKind(string kind)
    {
        var validator = new CreateShareLinkCommandValidator();
        var command = new CreateShareLinkCommand(kind, Guid.NewGuid(), "Viewer");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
