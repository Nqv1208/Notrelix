using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Queries;

/// <summary>
/// The member list is a cursor page (LD-06): oldest member first, user id on ties, default 50, at
/// most 200, removed members never listed and never counted against a page. Walking the pages
/// yields every member exactly once, even when several joined at the same instant.
/// </summary>
public sealed class GetWorkspaceMembersPagingTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IMemberPhoneLookup> _phones = new();

    public GetWorkspaceMembersPagingTests()
    {
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Select(id => new ActorSnapshot(id, $"User {id:N}", null)).ToList());
        _phones.Setup(p => p.FindSharedPhonesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                (IReadOnlyDictionary<Guid, string>)ids.ToDictionary(id => id, _ => "+84901234567"));
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
    }

    private Task<Result<CursorPage<WorkspaceMemberDto>>> PageAsync(string? cursor = null, int? limit = null) =>
        new GetWorkspaceMembersQueryHandler(DbContextMock.Object, _actors.Object, _phones.Object, RequestContextMock.Object)
            .Handle(new GetWorkspaceMembersQuery(TestWorkspaceId, cursor, limit), CancellationToken.None);

    private WorkspaceMember JoinedAt(DateTimeOffset at, Guid? userId = null, WorkspaceRole role = WorkspaceRole.Member) =>
        WorkspaceMember.Create(TestAccountId, TestWorkspaceId, userId ?? Guid.CreateVersion7(), role, TestUserId, at);

    private async Task<List<WorkspaceMemberDto>> WalkAsync(int limit)
    {
        var seen = new List<WorkspaceMemberDto>();
        string? cursor = null;
        for (var pages = 0; pages < 1000; pages++)
        {
            var page = (await PageAsync(cursor, limit)).Data!;
            seen.AddRange(page.Items);
            cursor = page.NextCursor;
            if (cursor is null) return seen;
        }

        throw new InvalidOperationException("The cursor never ended.");
    }

    [Fact]
    public async Task DefaultPage_HoldsFiftyMembers_AndTheNextCursorLeadsToTheRest()
    {
        SetupMembers(Enumerable.Range(0, 60).Select(i => JoinedAt(TestNow.AddMinutes(i))).ToArray());

        var first = (await PageAsync()).Data!;
        first.Items.Should().HaveCount(50);
        first.NextCursor.Should().NotBeNullOrEmpty();

        var second = (await PageAsync(first.NextCursor)).Data!;
        second.Items.Should().HaveCount(10);
        second.NextCursor.Should().BeNull("the last page has no cursor");
        first.Items.Select(m => m.UserId).Intersect(second.Items.Select(m => m.UserId)).Should().BeEmpty();
    }

    [Fact]
    public async Task APageThatEndsExactlyOnTheLastMember_HasNoCursor()
    {
        SetupMembers(Enumerable.Range(0, 3).Select(i => JoinedAt(TestNow.AddMinutes(i))).ToArray());

        var page = (await PageAsync(limit: 3)).Data!;

        page.Items.Should().HaveCount(3);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Members_AreListedOldestFirst()
    {
        var late = JoinedAt(TestNow.AddDays(2));
        var early = JoinedAt(TestNow);
        var middle = JoinedAt(TestNow.AddDays(1));
        SetupMembers(late, early, middle);

        var page = (await PageAsync()).Data!;

        page.Items.Select(m => m.UserId).Should().Equal(early.UserId, middle.UserId, late.UserId);
    }

    [Fact]
    public async Task PeopleWhoJoinedTogether_AreEachListedOnce_WhenWalkedOneAtATime()
    {
        SetupMembers(Enumerable.Range(0, 7).Select(_ => JoinedAt(TestNow)).ToArray());

        var walked = await WalkAsync(limit: 1);

        walked.Should().HaveCount(7);
        walked.Select(m => m.UserId).Distinct().Should().HaveCount(7, "a tie on joined time must not repeat or skip a person");
    }

    [Fact]
    public async Task RemovedMembers_AreNeverListed_AndDoNotUseUpAPageSlot()
    {
        var removed = JoinedAt(TestNow);
        removed.Remove(activeOwnerCount: 1, TestUserId, TestNow.AddMinutes(1));
        var first = JoinedAt(TestNow.AddMinutes(1));
        var second = JoinedAt(TestNow.AddMinutes(2));
        SetupMembers(removed, first, second);

        var page = (await PageAsync(limit: 2)).Data!;

        page.Items.Select(m => m.UserId).Should().Equal(first.UserId, second.UserId);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task TheReaderNeedNotBeOnThePage_ToSeeTheOptedInPhones()
    {
        // The reader joined last, so the first page does not contain their own row.
        var colleagues = Enumerable.Range(0, 3).Select(i => JoinedAt(TestNow.AddMinutes(i))).ToList();
        var reader = JoinedAt(TestNow.AddDays(1), TestUserId);
        SetupMembers([.. colleagues, reader]);

        var page = (await PageAsync(limit: 3)).Data!;

        page.Items.Select(m => m.UserId).Should().NotContain(TestUserId);
        page.Items.Should().OnlyContain(m => m.Phone == "+84901234567");
    }

    [Fact]
    public async Task AGuestReaderOffThePage_StillGetsNoPhone()
    {
        var colleagues = Enumerable.Range(0, 3).Select(i => JoinedAt(TestNow.AddMinutes(i))).ToList();
        var reader = JoinedAt(TestNow.AddDays(1), TestUserId, WorkspaceRole.Guest);
        SetupMembers([.. colleagues, reader]);

        var page = (await PageAsync(limit: 3)).Data!;

        page.Items.Should().OnlyContain(m => m.Phone == null);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(200, true)]
    [InlineData(0, false)]
    [InlineData(201, false)]
    [InlineData(-5, false)]
    public void Limit_IsBetweenOneAndTwoHundred(int limit, bool valid) =>
        new GetWorkspaceMembersQueryValidator().Validate(new GetWorkspaceMembersQuery(TestWorkspaceId, null, limit)).IsValid.Should().Be(valid);

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("MTIzOjQ1Ng")]
    public void ACursorThisListDidNotIssue_IsRejected(string cursor) =>
        new GetWorkspaceMembersQueryValidator().Validate(new GetWorkspaceMembersQuery(TestWorkspaceId, cursor, null)).IsValid.Should().BeFalse();

    [Fact]
    public async Task TheCursorTheListIssued_IsAccepted()
    {
        SetupMembers(Enumerable.Range(0, 3).Select(i => JoinedAt(TestNow.AddMinutes(i))).ToArray());
        var cursor = (await PageAsync(limit: 1)).Data!.NextCursor;

        new GetWorkspaceMembersQueryValidator().Validate(new GetWorkspaceMembersQuery(TestWorkspaceId, cursor, 1)).IsValid.Should().BeTrue();
    }
}
