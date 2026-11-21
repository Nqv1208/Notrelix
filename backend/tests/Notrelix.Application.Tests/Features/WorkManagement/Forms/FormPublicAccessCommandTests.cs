using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SetFormPublicAccess;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetFormPublicAccess;
using Notrelix.Application.Features.Workspaces.Public.Sharing;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Forms;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Forms;

/// <summary>Only an Owner or Admin switches a form public; going private ends every link at once.</summary>
public class FormPublicAccessCommandTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IWorkspaceSharingFacts> _sharing = new();

    public FormPublicAccessCommandTests()
    {
        DateTimeProviderMock.Setup(d => d.UtcNow).Returns(TestNow);
        RequestContextMock.SetupGet(r => r.UserId).Returns(TestUserId);
        _sharing.Setup(s => s.FindAsync(TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceSharingFacts(true, true));
    }

    private SetFormPublicAccessCommandHandler Handler() =>
        new(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _sharing.Object);

    [Theory]
    [InlineData("Owner", BaselineDecision.Allow)]
    [InlineData("Admin", BaselineDecision.Allow)]
    [InlineData("Member", BaselineDecision.Deny)]
    [InlineData("Guest", BaselineDecision.Deny)]
    public void TheSwitch_NeedsWorkspaceSettings_SoOnlyOwnerAndAdminHoldIt(string role, BaselineDecision expected)
    {
        var command = new SetFormPublicAccessCommand(Guid.NewGuid(), true);

        command.Action.Should().Be(PermissionAction.ManageWorkspaceSettings);
        WorkspaceRoleBaseline.Decide(role, command.Action).Should().Be(expected);
        WorkspaceRoleBaseline.AppliesToResourceKind(command.Action, command.Resource.Kind.Value).Should().BeTrue(
            "the role cell must apply to the form resource, or a role could never switch (or always could)");
    }

    [Fact]
    public void TheKeyIsReadOnlyByThoseWhoCanSwitch()
    {
        new GetFormPublicAccessQuery(Guid.NewGuid()).Action.Should().Be(PermissionAction.ManageWorkspaceSettings);
    }

    [Fact]
    public async Task Enable_MakesTheFormPublic_WithAFreshKey()
    {
        var form = CreateForm();
        SetupForms(form);

        var result = await Handler().Handle(new SetFormPublicAccessCommand(form.Id, true), CancellationToken.None);

        result.Data!.IsPublic.Should().BeTrue();
        result.Data.PublicKey.Should().Be(form.PublicKey).And.HaveLength(22);
    }

    [Fact]
    public async Task Disable_RevokesTheKey()
    {
        var form = CreateForm();
        form.MakePublic(Form.NewPublicKey(), TestUserId, TestNow);
        SetupForms(form);

        var result = await Handler().Handle(new SetFormPublicAccessCommand(form.Id, false), CancellationToken.None);

        result.Data!.IsPublic.Should().BeFalse();
        form.PublicKey.Should().BeNull();
    }

    [Fact]
    public async Task Enable_WhenAlreadyPublic_KeepsTheLinkAlreadyShared()
    {
        var form = CreateForm();
        var key = Form.NewPublicKey();
        form.MakePublic(key, TestUserId, TestNow);
        SetupForms(form);

        var result = await Handler().Handle(new SetFormPublicAccessCommand(form.Id, true), CancellationToken.None);

        result.Data!.PublicKey.Should().Be(key);
    }

    [Fact]
    public async Task Enable_WhenTheWorkspaceDisallowsPublicLinks_IsRefused_AndStaysPrivate()
    {
        var form = CreateForm();
        SetupForms(form);
        _sharing.Setup(s => s.FindAsync(TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceSharingFacts(true, false));

        var act = () => Handler().Handle(new SetFormPublicAccessCommand(form.Id, true), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Form_PublicSharingDisabled);
        form.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task Disable_WhenTheWorkspaceDisallowsPublicLinks_StillWorks()
    {
        var form = CreateForm();
        form.MakePublic(Form.NewPublicKey(), TestUserId, TestNow);
        SetupForms(form);
        _sharing.Setup(s => s.FindAsync(TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceSharingFacts(true, false));

        var result = await Handler().Handle(new SetFormPublicAccessCommand(form.Id, false), CancellationToken.None);

        result.Data!.IsPublic.Should().BeFalse("a switch-off is never blocked");
    }

    [Fact]
    public async Task Switch_UnknownOrDeletedForm_IsNotFound()
    {
        var deleted = CreateForm();
        deleted.Delete(TestUserId, TestNow);
        SetupForms(deleted);

        var act = () => Handler().Handle(new SetFormPublicAccessCommand(deleted.Id, true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
