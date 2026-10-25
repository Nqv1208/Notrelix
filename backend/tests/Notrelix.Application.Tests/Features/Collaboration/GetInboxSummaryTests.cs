using Notrelix.Application.Features.Collaboration.Notifications.Queries.GetInboxSummary;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Tests.Features.Collaboration;

public class GetInboxSummaryTests
{
    [Fact]
    public async Task Summary_CountsOnlyTheCallersUnreadInWorkspacesTheyStillBelongTo()
    {
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var busy = Guid.CreateVersion7();
        var quiet = Guid.CreateVersion7();
        var left = Guid.CreateVersion7();
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(userId);
        var inbox = new Mock<INotificationInbox>();
        inbox.Setup(i => i.UnreadByWorkspaceAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InboxUnreadByWorkspace> { new(quiet, 0), new(busy, 3), new(left, 5) });
        var access = new Mock<IWorkspaceAccessResolver>();
        access.Setup(a => a.ResolveAsync(busy, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(accountId, busy, userId, CanAccess: true, IsWorkspaceActive: true));
        access.Setup(a => a.ResolveAsync(left, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(accountId, left, userId, CanAccess: false, IsWorkspaceActive: true));

        var result = await new GetInboxSummaryQueryHandler(inbox.Object, access.Object, requestContext.Object)
            .Handle(new GetInboxSummaryQuery(), CancellationToken.None);

        result.Data!.Total.Should().Be(3);
        result.Data.PerWorkspace.Should().ContainSingle(c => c.WorkspaceId == busy && c.Unread == 3);
    }
}
