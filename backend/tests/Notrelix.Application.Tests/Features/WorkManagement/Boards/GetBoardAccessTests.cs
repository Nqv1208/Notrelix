using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardAccess;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// The Board access read maps the Governance summary onto the Work contract and
/// returns the same NotFound for a Board the caller cannot see as for a missing
/// Board (GOV-025): it never answers from Work data alone.
/// </summary>
public sealed class GetBoardAccessTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid BoardId = Guid.NewGuid();

    [Fact]
    public void Query_IsAViewBoardRead_OnTheBoard()
    {
        var query = new GetBoardAccessQuery(BoardId);

        query.Action.Should().Be(PermissionAction.ViewBoard);
        query.Resource.Should().Be(ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId));
        query.Should().BeAssignableTo<IReadRequest>();
    }

    [Fact]
    public async Task MapsTheSummary_WithTheBoardVersion()
    {
        var grantor = Guid.NewGuid();
        var spaceId = Guid.NewGuid();
        var summary = new BoardAccessSummary(
            AccessLevels.Commenter,
            new AccessSource(AccessSourceKinds.Explicit, new AccessGrantor(grantor, "Hà Anh")),
            3,
            1,
            false,
            [
                new AccessSourceCount(AccessSourceKinds.Space, spaceId, "Phòng Công nghệ", 2),
                new AccessSourceCount(AccessSourceKinds.Guest, null, null, 1),
            ]);
        var (handler, access) = Create(new BoardAccessSubject(BoardId, 12, []), summary);
        var query = new GetBoardAccessQuery(BoardId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(new BoardAccessDto(
            BoardId,
            12,
            "commenter",
            new BoardAccessSourceDto("explicit", new BoardAccessGrantorDto(grantor, "Hà Anh")),
            3,
            1,
            false,
            [
                new BoardAccessSourceCountDto("space", spaceId, "Phòng Công nghệ", 2),
                new BoardAccessSourceCountDto("guest", null, null, 1),
            ]));
        access.Verify(a => a.GetBoardAccessAsync(BoardId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CallerWithoutAccess_IsNotFound()
    {
        var (handler, _) = Create(new BoardAccessSubject(BoardId, 3, []), summary: null);

        var act = () => handler.Handle(new GetBoardAccessQuery(BoardId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MissingBoard_IsNotFound_WithoutAskingForTheSummary()
    {
        var (handler, access) = Create(board: null, summary: null);

        var act = () => handler.Handle(new GetBoardAccessQuery(BoardId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        access.Verify(a => a.GetBoardAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (GetBoardAccessQueryHandler Handler, Mock<IResourceAccessSummary> Access) Create(
        BoardAccessSubject? board,
        BoardAccessSummary? summary)
    {
        var access = new Mock<IResourceAccessSummary>();
        access.Setup(a => a.GetBoardAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        var boards = new Mock<IBoardAccessSubjectFacts>();
        boards.Setup(b => b.FindAsync(WorkspaceId, BoardId, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.RequireWorkspaceId()).Returns(WorkspaceId);

        return (new GetBoardAccessQueryHandler(access.Object, boards.Object, requestContext.Object), access);
    }
}
