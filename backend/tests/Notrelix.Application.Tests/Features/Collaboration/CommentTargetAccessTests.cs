using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Collaboration.Comments.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;

namespace Notrelix.Application.Tests.Features.Collaboration;

public class CommentTargetAccessTests
{
    [Theory]
    [InlineData(CommentTargetRight.View)]
    [InlineData(CommentTargetRight.Edit)]
    public async Task ADeletedBoardItem_GrantsNoRight_WithoutAskingTheEngine(CommentTargetRight right)
    {
        var workspace = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var reader = new Mock<IExecutionContextReader>();
        reader.Setup(r => r.Snapshot).Returns(new ExecutionContextSnapshot(
            Guid.CreateVersion7(), Guid.CreateVersion7(), workspace, null,
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "corr"));
        var items = new Mock<IWorkItemNotificationFacts>();
        items.Setup(i => i.FindAsync(workspace, itemId, null, It.IsAny<CancellationToken>())).ReturnsAsync((WorkItemNotificationFacts?)null);
        var facts = new Mock<IAccessFactsProvider>(MockBehavior.Strict);
        var access = new CommentTargetAccess(
            new Mock<IRequestDescriptorRegistry>(MockBehavior.Strict).Object, facts.Object,
            new Mock<IAccessPolicyEvaluator>(MockBehavior.Strict).Object, reader.Object, items.Object);

        var granted = await access.HasAsync(
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), itemId, workspace), right, default);

        granted.Should().BeFalse();
    }
}
