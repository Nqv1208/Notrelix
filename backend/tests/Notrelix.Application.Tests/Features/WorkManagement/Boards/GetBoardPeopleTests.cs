using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardPeople;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// The board-scoped people read (UC-WM-A23, UC-WM-C13): a Guest reads the people who can see one Board
/// through its Governance summary, names and avatars only, and a Board the caller cannot see is NotFound.
/// </summary>
public sealed class GetBoardPeopleTests
{
    private static readonly Guid BoardId = Guid.NewGuid();

    [Fact]
    public void Query_IsAViewBoardRead_OnTheBoard()
    {
        var query = new GetBoardPeopleQuery(BoardId);

        query.Action.Should().Be(PermissionAction.ViewBoard);
        query.Resource.Should().Be(ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId));
        query.Should().BeAssignableTo<IReadRequest>();
    }

    [Fact]
    public async Task ListsBoardPeople_WithNamesAndLevels_AndNoEmail()
    {
        var member = Guid.NewGuid();
        var guest = Guid.NewGuid();
        var sharing = new Mock<IBoardSharingSummary>();
        sharing.Setup(s => s.GetBoardPeopleAsync(BoardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardPeople(
            [
                new BoardPerson(member, "editor", false, true),
                new BoardPerson(guest, "viewer", true, false),
            ]));
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ActorSnapshot(member, "Lê Hoàng Phúc", "https://cdn.test/a.png"),
                new ActorSnapshot(guest, "Đặng Minh Tuấn", null),
            ]);
        var handler = new GetBoardPeopleQueryHandler(sharing.Object, actors.Object);

        var result = await handler.Handle(new GetBoardPeopleQuery(BoardId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(new[]
        {
            new BoardPersonDto(member, "Lê Hoàng Phúc", "https://cdn.test/a.png", "editor", true, false),
            new BoardPersonDto(guest, "Đặng Minh Tuấn", null, "viewer", false, true),
        });
    }

    [Fact]
    public async Task CallerWhoCannotSeeTheBoard_IsNotFound()
    {
        var sharing = new Mock<IBoardSharingSummary>();
        sharing.Setup(s => s.GetBoardPeopleAsync(BoardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BoardPeople?)null);
        var handler = new GetBoardPeopleQueryHandler(sharing.Object, new Mock<IActorLookupService>().Object);

        var act = () => handler.Handle(new GetBoardPeopleQuery(BoardId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task NonManagerParty_GetsNameAndAvatarOnly_NoLevelOrFlags()
    {
        var guest = Guid.NewGuid();
        var sharing = new Mock<IBoardSharingSummary>();
        sharing.Setup(s => s.GetBoardPeopleAsync(BoardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BoardPeople([new BoardPerson(guest, null, null, null)]));
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(guest, "Đặng Minh Tuấn", null)]);
        var handler = new GetBoardPeopleQueryHandler(sharing.Object, actors.Object);

        var result = await handler.Handle(new GetBoardPeopleQuery(BoardId), CancellationToken.None);

        result.Data.Should().BeEquivalentTo(new[]
        {
            new BoardPersonDto(guest, "Đặng Minh Tuấn", null, null, null, null),
        });
        result.Data!.Single().Level.Should().BeNull();
        result.Data!.Single().IsGuest.Should().BeNull();
        result.Data!.Single().IsBoardMember.Should().BeNull();
    }
}

/// <summary>The disclosure rule of the people read (UC-WM-A23): a manager sees the fields, a party sees the names, anyone else gets NotFound.</summary>
public sealed class BoardPeopleDisclosureTests
{
    private static readonly IReadOnlyList<BoardPersonRow> Rows =
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "editor", false, true),
        new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "viewer", true, false),
    ];

    [Fact]
    public void Manager_SeesEveryField()
    {
        var people = BoardPeopleDisclosure.Disclose(canManage: true, readerIsParty: false, Rows);

        people!.People.Should().BeEquivalentTo(new[]
        {
            new BoardPerson(Rows[0].UserId, "editor", false, true),
            new BoardPerson(Rows[1].UserId, "viewer", true, false),
        });
    }

    [Fact]
    public void BoardMember_OrGrantHolder_SeesOnlyTheIds()
    {
        var people = BoardPeopleDisclosure.Disclose(canManage: false, readerIsParty: true, Rows);

        people!.People.Select(p => p.UserId).Should().Equal(Rows[0].UserId, Rows[1].UserId);
        people.People.Should().OnlyContain(p => p.Level == null && p.IsGuest == null && p.IsBoardMember == null);
    }

    [Fact]
    public void CallerWithNoStake_GetsNothing()
    {
        BoardPeopleDisclosure.Disclose(canManage: false, readerIsParty: false, Rows).Should().BeNull();
    }
}
