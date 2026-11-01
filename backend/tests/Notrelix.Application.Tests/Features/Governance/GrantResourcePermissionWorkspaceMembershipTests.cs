using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

namespace Notrelix.Application.Tests.Features.Governance;

public class GrantResourcePermissionWorkspaceMembershipTests
{
    [Fact]
    public async Task Handle_UserSubjectOutsideWorkspace_IsRejectedBeforeAnyWrite()
    {
        var workspaceId = Guid.CreateVersion7();
        var accountId = Guid.CreateVersion7();
        var context = new Mock<IGovernanceDbContext>(MockBehavior.Strict);
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.RequireWorkspaceId()).Returns(workspaceId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContext.Setup(r => r.UserId).Returns(Guid.CreateVersion7());
        var access = new Mock<IWorkspaceAccessResolver>();
        access.Setup(r => r.ResolveAsync(workspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(accountId, workspaceId, Guid.Empty, CanAccess: false, IsWorkspaceActive: true));

        var handler = new GrantResourcePermissionCommandHandler(
            context.Object,
            requestContext.Object,
            new Mock<IDateTimeProvider>().Object,
            new Mock<IAuditService>().Object,
            new Mock<IResourceAccessChangeRecorder>().Object,
            access.Object,
            new Mock<Notrelix.Application.Features.Governance.Public.Access.IBoardSharingSummary>().Object);

        var command = new GrantResourcePermissionCommand(
            "work-management.board", Guid.CreateVersion7(), "User", Guid.CreateVersion7(), "Viewer");

        await handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleException>();
        context.VerifyNoOtherCalls();
    }
}
