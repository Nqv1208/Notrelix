using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ResolveBoardItemKey;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>An Item opens by its key, or by the old key it kept as an alias after moving to another board (D76.3, UC-WM-A10).</summary>
public class ResolveBoardItemKeyTests : WorkManagementHandlerTestBase
{
    private readonly Guid _source = Guid.CreateVersion7();
    private readonly Guid _target = Guid.CreateVersion7();
    private readonly Mock<IBoardItemKeyAliases> _aliases = new();
    private readonly Mock<IResourceVisibility> _visibility = new();
    private readonly BoardItem _here;
    private readonly BoardItem _moved;

    public ResolveBoardItemKeyTests()
    {
        _here = BoardItem.CreateRoot(TestAccountId, TestWorkspaceId, _source, Guid.CreateVersion7(), "here", FractionalIndex.Create("a0"), TestUserId, TestNow, itemKey: "WEB-1", itemSequence: 1);
        _moved = BoardItem.CreateRoot(TestAccountId, TestWorkspaceId, _target, Guid.CreateVersion7(), "moved", FractionalIndex.Create("a0"), TestUserId, TestNow, itemKey: "OPS-9", itemSequence: 9);
        SetupBoardItems(_here, _moved);
        _aliases.Setup(a => a.FindItemAsync(_source, "WEB-3", It.IsAny<CancellationToken>())).ReturnsAsync(_moved.Id);
    }

    private ResolveBoardItemKeyQueryHandler Handler(params Guid[] visible)
    {
        _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>())).ReturnsAsync(visible);
        return new ResolveBoardItemKeyQueryHandler(DbContextMock.Object, RequestContextMock.Object, _visibility.Object, _aliases.Object);
    }

    [Fact]
    public async Task ACurrentKey_ResolvesOnItsBoard()
    {
        var resolved = await Handler(_source).Handle(new ResolveBoardItemKeyQuery(_source, " WEB-1 "), CancellationToken.None);

        resolved.Should().Be(new ResolvedBoardItemKeyDto(_here.Id, _source, "WEB-1", false));
    }

    [Fact]
    public async Task AnOldKey_ResolvesToTheMovedItem_WithItsNewKey()
    {
        var resolved = await Handler(_source, _target).Handle(new ResolveBoardItemKeyQuery(_source, "WEB-3"), CancellationToken.None);

        resolved.Should().Be(new ResolvedBoardItemKeyDto(_moved.Id, _target, "OPS-9", true));
    }

    [Fact]
    public async Task AnOldKey_IsNotFound_WhenTheNewBoardIsNotVisible_LikeAKeyNeverIssued()
    {
        var hidden = () => Handler(_source).Handle(new ResolveBoardItemKeyQuery(_source, "WEB-3"), CancellationToken.None);
        var never = () => Handler(_source, _target).Handle(new ResolveBoardItemKeyQuery(_source, "WEB-77"), CancellationToken.None);

        await hidden.Should().ThrowAsync<NotFoundException>();
        await never.Should().ThrowAsync<NotFoundException>();
    }
}
