using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class DeleteWorkspaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IAuditService> _audit = new();

    private DeleteWorkspaceCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _audit.Object);

    private void VerifyAudit(Times times) => _audit.Verify(a => a.RecordAsync(
        It.IsAny<Guid>(), It.IsAny<Guid>(), "DeleteWorkspace", It.IsAny<ResourceRef>(), It.IsAny<AuditMetadata>(),
        AuditSeverity.Critical, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), times);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_DeletesSuccessfully_AndRecordsOneAuditFact()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var sut = CreateSut();
        var result = await sut.Handle(new DeleteWorkspaceCommand(workspace.Id, 1), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        workspace.IsDeleted.Should().BeTrue();
        workspace.DeletedBy.Should().Be(TestUserId);
        VerifyAudit(Times.Once());
    }

    [Fact]
    public async Task Handle_WhenAlreadyDeleted_IsANoOpWithoutASecondAuditFact()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(TestUserId, TestNow);
        var version = workspace.Version;
        SetupWorkspaces(workspace);

        var result = await CreateSut().Handle(new DeleteWorkspaceCommand(workspace.Id, version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        workspace.Version.Should().Be(version);
        VerifyAudit(Times.Never());
    }

    [Fact]
    public async Task Handle_WhenPersonalWorkspace_IsRefusedWithTheRuleCode_AndRecordsNothing()
    {
        var personal = Workspace.Create(TestAccountId, TestUserId, "Mine", "mine-space", TestNow, isPersonal: true);
        SetupWorkspaces(personal);

        Func<Task> act = () => CreateSut().Handle(new DeleteWorkspaceCommand(personal.Id, 1), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_CannotDeletePersonal);
        personal.IsDeleted.Should().BeFalse();
        VerifyAudit(Times.Never());
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new DeleteWorkspaceCommand(TestWorkspaceId, 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
