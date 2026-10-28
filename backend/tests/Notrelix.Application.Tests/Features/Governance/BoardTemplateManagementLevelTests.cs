using Notrelix.Application.Features.WorkManagement.Templates.Commands.ArchiveBoardTemplate;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.DeleteBoardTemplate;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.PublishBoardTemplate;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Publishing, archiving and deleting a board template is decided on the template's own resource kind
/// (never a board id lookup): workspace Owner and Admin manage it, a Member or Guest never does, and a
/// template that is not found in the workspace is NotFound for everyone.
/// </summary>
public sealed class BoardTemplateManagementLevelTests
{
    private static readonly AccessPolicyEngine Engine = new();

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static readonly RequestDescriptor Descriptor = new(
        typeof(PublishBoardTemplateCommand), ApplicationRequestKind.Command, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, ApplicationDataAccessKind.Write,
        new AccessRequirements(true, false, false, false), false, false);

    public static TheoryData<object> Commands() => new()
    {
        new PublishBoardTemplateCommand(Guid.NewGuid()),
        new ArchiveBoardTemplateCommand(Guid.NewGuid()),
        new DeleteBoardTemplateCommand(Guid.NewGuid()),
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public void Commands_DeclareTheTemplateKind(object command)
    {
        var permission = (IRequirePermission)command;

        permission.Action.Should().Be(PermissionAction.ManageBoard);
        permission.Resource!.Kind.Value.Should().Be("work-management.board-template");
    }

    [Theory]
    [InlineData("Owner", AccessDecisionKind.Allowed)]
    [InlineData("Admin", AccessDecisionKind.Allowed)]
    [InlineData("Member", AccessDecisionKind.Forbidden)]
    [InlineData("Guest", AccessDecisionKind.Forbidden)]
    public void ManagingATemplate_IsDecidedByTheWorkspaceRole(string role, AccessDecisionKind expected) =>
        Decide(new PublishBoardTemplateCommand(Guid.NewGuid()), Facts(role, resourceExists: true)).Should().Be(expected);

    [Fact]
    public void ATemplateThatIsNotFound_IsNotFound_EvenForAnOwner() =>
        Decide(new DeleteBoardTemplateCommand(Guid.NewGuid()), Facts("Owner", resourceExists: false)).Should().Be(AccessDecisionKind.NotFound);

    private static AccessDecisionKind Decide(IRequirePermission request, AccessFacts facts) =>
        Engine.Evaluate(Descriptor, Context, facts, request).Kind;

    private static AccessFacts Facts(string role, bool resourceExists) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: resourceExists,
        ResourceAudience: null,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: false,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: null,
        TargetPermissionRank: null);
}
