using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Application.Features.Collaboration.Activity.Ports;
using Notrelix.Application.Features.Collaboration.Activity.Queries.GetPageActivity;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>The page activity feed: gated by view access to the page, paged, and worded from the stored rows.</summary>
public sealed class GetPageActivityQueryTests
{
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid PageId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void TheQuery_NeedsViewAccessToThePage()
    {
        var query = new GetPageActivityQuery(PageId);

        query.Action.Should().Be(PermissionAction.ViewPage);
        query.Resource.ResourceId.Should().Be(PageId);
        query.Resource.Kind.Value.Should().Be(PageActivityTypes.ResourceKind);
    }

    [Fact]
    public async Task TheFeed_ReadsThePageResourceOfTheCallersWorkspace_WithTheRequestedPage()
    {
        var reader = new Mock<IResourceActivityReader>();
        var row = new ResourceActivityRow(Guid.CreateVersion7(), UserId, PageActivityTypes.Renamed, "renamed the page", "New", "{\"from\":\"Old\",\"to\":\"New\"}", DateTimeOffset.UtcNow);
        reader.Setup(r => r.ListAsync(WorkspaceId, PageActivityTypes.ResourceKind, PageId, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceActivityPage([row], 6));
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(UserId, "Hà", null)]);
        var request = new FakeCurrentRequestContext().AsUser(UserId);
        request.Tenant.SetWorkspace(Guid.CreateVersion7(), WorkspaceId, UserId);

        var result = await new GetPageActivityQueryHandler(reader.Object, actors.Object, request)
            .Handle(new GetPageActivityQuery(PageId, 2, 5), CancellationToken.None);

        var feed = result.Data!;
        feed.Total.Should().Be(6);
        var entry = feed.Data.Single();
        entry.Type.Should().Be(PageActivityTypes.Renamed);
        entry.ActorName.Should().Be("Hà");
        entry.Change!.From.Should().Be("Old");
        entry.Change.To.Should().Be("New");
    }
}
