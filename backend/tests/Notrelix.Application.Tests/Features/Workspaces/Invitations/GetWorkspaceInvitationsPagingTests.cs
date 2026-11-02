using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations;

/// <summary>
/// The invitation list is a cursor page: newest first, invitation id on ties, default 50, at most 200. A cursor
/// this list did not issue is refused by the validator (the API answers 400). Walking the pages yields every
/// invitation exactly once, even when several were created at the same instant.
/// </summary>
public sealed class GetWorkspaceInvitationsPagingTests : WorkspaceHandlerTestBase
{
    public GetWorkspaceInvitationsPagingTests() => SetupWorkspaces(CreateWorkspace(TestWorkspaceId));

    private Task<Result<CursorPage<WorkspaceInvitationDto>>> PageAsync(string? cursor = null, int? limit = null) =>
        new GetWorkspaceInvitationsQueryHandler(DbContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new GetWorkspaceInvitationsQuery(TestWorkspaceId, cursor, limit), CancellationToken.None);

    private static string Digest(int n) => new string('b', 62) + n.ToString("x2");

    private WorkspaceInvitation CreatedAt(DateTimeOffset at, int n) =>
        WorkspaceInvitation.Create(
            TestAccountId, TestWorkspaceId, $"person{n}@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Digest(n)), 1, TestUserId, at, TimeSpan.FromDays(7));

    private void Seed(int count, Func<int, DateTimeOffset> at) =>
        SetupInvitations(Enumerable.Range(0, count).Select(n => CreatedAt(at(n), n)).ToArray());

    private async Task<List<WorkspaceInvitationDto>> WalkAsync(int limit)
    {
        var seen = new List<WorkspaceInvitationDto>();
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
    public async Task DefaultPage_HoldsFiftyInvitations_AndTheNextCursorLeadsToTheRest()
    {
        Seed(60, n => TestNow.AddMinutes(-n));

        var first = (await PageAsync()).Data!;
        first.Items.Should().HaveCount(50);
        first.NextCursor.Should().NotBeNullOrEmpty();

        var second = (await PageAsync(first.NextCursor)).Data!;
        second.Items.Should().HaveCount(10);
        second.NextCursor.Should().BeNull("the last page has no cursor");
        first.Items.Select(i => i.Id).Intersect(second.Items.Select(i => i.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task APageThatEndsExactlyOnTheLastInvitation_HasNoCursor()
    {
        Seed(3, n => TestNow.AddMinutes(-n));

        var page = (await PageAsync(limit: 3)).Data!;

        page.Items.Should().HaveCount(3);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task ALimitOfOne_ReturnsOneInvitation_AndACursor()
    {
        Seed(3, n => TestNow.AddMinutes(-n));

        var page = (await PageAsync(limit: 1)).Data!;

        page.Items.Should().ContainSingle();
        page.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Invitations_AreListedNewestFirst()
    {
        var oldest = CreatedAt(TestNow.AddDays(-2), 0);
        var newest = CreatedAt(TestNow, 1);
        var middle = CreatedAt(TestNow.AddDays(-1), 2);
        SetupInvitations(oldest, newest, middle);

        var page = (await PageAsync()).Data!;

        page.Items.Select(i => i.Id).Should().Equal(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task InvitationsCreatedTogether_AreEachListedOnce_WhenWalkedOneAtATime()
    {
        Seed(7, _ => TestNow);

        var walked = await WalkAsync(limit: 1);

        walked.Should().HaveCount(7);
        walked.Select(i => i.Id).Distinct().Should().HaveCount(7, "a tie on created time must not repeat or skip an invitation");
    }

    [Fact]
    public async Task InvitationsCreatedTogether_AreListedByIdDescending()
    {
        Seed(5, _ => TestNow);

        var walked = await WalkAsync(limit: 2);

        walked.Select(i => i.Id).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task WalkingMixedTimes_ListsEveryInvitationOnce_InOrder()
    {
        Seed(25, n => TestNow.AddMinutes(-(n / 3)));

        var walked = await WalkAsync(limit: 4);

        walked.Should().HaveCount(25);
        walked.Select(i => i.Id).Distinct().Should().HaveCount(25);
        walked.Select(i => i.CreatedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task AnInvitationOfAnotherWorkspace_IsNeverListed()
    {
        var mine = CreatedAt(TestNow, 0);
        var foreign = WorkspaceInvitation.Create(
            TestAccountId, Guid.CreateVersion7(), "x@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Digest(9)), 1, TestUserId, TestNow, TimeSpan.FromDays(7));
        SetupInvitations(mine, foreign);

        (await PageAsync()).Data!.Items.Select(i => i.Id).Should().Equal(mine.Id);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(200, true)]
    [InlineData(0, false)]
    [InlineData(201, false)]
    [InlineData(-5, false)]
    public void Limit_IsBetweenOneAndTwoHundred(int limit, bool valid) =>
        new GetWorkspaceInvitationsQueryValidator().Validate(new GetWorkspaceInvitationsQuery(TestWorkspaceId, null, limit)).IsValid.Should().Be(valid);

    [Fact]
    public void NoLimit_IsValid_AndMeansFifty()
    {
        new GetWorkspaceInvitationsQueryValidator().Validate(new GetWorkspaceInvitationsQuery(TestWorkspaceId)).IsValid.Should().BeTrue();
        GetWorkspaceInvitationsQuery.DefaultLimit.Should().Be(50);
        GetWorkspaceInvitationsQuery.MaxLimit.Should().Be(200);
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("MTIzOjQ1Ng")]
    [InlineData("!!!")]
    public void ACursorThisListDidNotIssue_IsRejected(string cursor) =>
        new GetWorkspaceInvitationsQueryValidator().Validate(new GetWorkspaceInvitationsQuery(TestWorkspaceId, cursor, null)).IsValid.Should().BeFalse();

    [Fact]
    public async Task TheCursorTheListIssued_IsAccepted()
    {
        Seed(3, n => TestNow.AddMinutes(-n));
        var cursor = (await PageAsync(limit: 1)).Data!.NextCursor;

        new GetWorkspaceInvitationsQueryValidator().Validate(new GetWorkspaceInvitationsQuery(TestWorkspaceId, cursor, 1)).IsValid.Should().BeTrue();
    }
}
