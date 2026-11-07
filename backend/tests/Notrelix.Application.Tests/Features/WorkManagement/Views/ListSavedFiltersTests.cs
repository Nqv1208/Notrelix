using Notrelix.Application.Features.WorkManagement.Views.Queries.ListSavedFilters;

namespace Notrelix.Application.Tests.Features.WorkManagement.Views;

public class ListSavedFiltersTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IWorkspaceAccessResolver> _access = new();
    private readonly ListSavedFiltersQueryHandler _handler;
    private readonly Guid _boardId = Guid.CreateVersion7();
    private readonly Guid _colleagueId = Guid.CreateVersion7();

    public ListSavedFiltersTests()
    {
        _handler = new ListSavedFiltersQueryHandler(
            DbContextMock.Object, RequestContextMock.Object, _actors.Object, _access.Object);
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_colleagueId, "Hà Nguyễn", null)]);
    }

    private SavedFilter Shared(Guid by) => SavedFilter.Create(
        TestAccountId, TestWorkspaceId, _boardId, "Việc gấp", Array.Empty<FilterRule>(), by, TestNow,
        visibility: SavedFilterVisibility.Public);

    private void SetupAccess(bool guest) => _access
        .Setup(a => a.ResolveAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new WorkspaceAccessSnapshot(
            TestAccountId, TestWorkspaceId, TestUserId, true, true, IsGuest: guest));

    [Fact]
    public async Task Handle_SharedByAColleague_NamesTheCreatorAndNotMyOwn()
    {
        SetupAccess(guest: false);
        SetupSavedFilters(Shared(_colleagueId), Shared(TestUserId));

        var result = await _handler.Handle(new ListSavedFiltersQuery(_boardId), CancellationToken.None);

        result.Data!.Should().HaveCount(2);
        result.Data!.Single(f => !f.IsMine).CreatedByName.Should().Be("Hà Nguyễn");
        result.Data!.Single(f => f.IsMine).CreatedByName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AWorkspaceGuest_NeverGetsTheCreatorsName()
    {
        SetupAccess(guest: true);
        SetupSavedFilters(Shared(_colleagueId));

        var result = await _handler.Handle(new ListSavedFiltersQuery(_boardId), CancellationToken.None);

        result.Data!.Single().CreatedByName.Should().BeNull();
        _actors.Verify(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
