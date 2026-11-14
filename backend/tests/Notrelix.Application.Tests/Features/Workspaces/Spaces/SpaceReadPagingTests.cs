using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembers;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Workspaces.Spaces;

/// <summary>
/// The Space tree and the Space list are cursor pages over only the Spaces the caller may see. The tree runs in
/// Space id order and the list in name order with the id on ties; both default to 50 and at most 200; a cursor the
/// list did not issue is refused by the validator (the API answers 400); walking the pages reads every visible
/// Space exactly once and never a hidden one.
/// </summary>
public sealed class SpaceReadPagingTests : WorkspaceHandlerTestBase
{
    private readonly InMemorySpaceStore _store = new();
    private readonly FakeResourceVisibility _visibility = new();

    public SpaceReadPagingTests() => SetupWorkspaces(CreateWorkspace(TestWorkspaceId));

    private Space Seed(string name, bool visible = true)
    {
        var space = Space.Create(TestAccountId, TestWorkspaceId, name, SpaceVisibility.Workspace, TestUserId, TestNow);
        _store.Spaces.Add(space);
        if (visible) _visibility.Spaces.Add(space.Id);
        return space;
    }

    private void SeedMany(int count) => Enumerable.Range(0, count).ToList().ForEach(n => Seed($"Space {n:D3}"));

    private Task<Result<CursorPage<SpaceTreeNodeDto>>> TreePage(string? cursor = null, int? limit = null) =>
        new GetSpaceTreeQueryHandler(_store, _visibility, RequestContextMock.Object)
            .Handle(new GetSpaceTreeQuery(TestWorkspaceId, cursor, limit), CancellationToken.None);

    private Task<Result<CursorPage<SpaceDto>>> ListPage(string? cursor = null, int? limit = null)
    {
        SetupSpaces([.. _store.Spaces]);
        return new GetWorkspaceSpacesQueryHandler(DbContextMock.Object, _visibility, RequestContextMock.Object)
            .Handle(new GetWorkspaceSpacesQuery(TestWorkspaceId, cursor, limit), CancellationToken.None);
    }

    private static async Task<List<T>> Walk<T>(Func<string?, Task<CursorPage<T>>> page)
    {
        var seen = new List<T>();
        string? cursor = null;
        for (var pages = 0; pages < 1000; pages++)
        {
            var next = await page(cursor);
            seen.AddRange(next.Items);
            cursor = next.NextCursor;
            if (cursor is null) return seen;
        }

        throw new InvalidOperationException("The cursor never ended.");
    }

    // ── Tree ────────────────────────────────────────────────

    [Fact]
    public async Task Tree_DefaultPageHoldsFifty_AndTheNextCursorLeadsToTheRest()
    {
        SeedMany(60);

        var first = (await TreePage()).Data!;
        first.Items.Should().HaveCount(50);
        first.NextCursor.Should().NotBeNullOrEmpty();

        var second = (await TreePage(first.NextCursor)).Data!;
        second.Items.Should().HaveCount(10);
        second.NextCursor.Should().BeNull("the last page has no cursor");
        first.Items.Select(n => n.Id).Intersect(second.Items.Select(n => n.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Tree_APageThatEndsExactlyOnTheLastSpace_HasNoCursor()
    {
        SeedMany(3);

        var page = (await TreePage(limit: 3)).Data!;

        page.Items.Should().HaveCount(3);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Tree_ALimitOfOne_ReturnsOneNode_AndACursor()
    {
        SeedMany(3);

        var page = (await TreePage(limit: 1)).Data!;

        page.Items.Should().ContainSingle();
        page.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Tree_WalkingOneAtATime_ReadsEverySpaceOnce_InIdOrder()
    {
        SeedMany(9);

        var walked = await Walk(async c => (await TreePage(c, 1)).Data!);

        walked.Should().HaveCount(9);
        walked.Select(n => n.Id).Distinct().Should().HaveCount(9);
        walked.Select(n => n.Id).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Tree_NeverReadsAHiddenSpace_OnAnyPage()
    {
        SeedMany(5);
        var hidden = Seed("Hidden", visible: false);

        var walked = await Walk(async c => (await TreePage(c, 2)).Data!);

        walked.Should().HaveCount(5);
        walked.Select(n => n.Id).Should().NotContain(hidden.Id);
    }

    [Fact]
    public async Task Tree_WithNothingVisible_IsAnEmptyPageWithNoCursor()
    {
        Seed("Hidden", visible: false);

        var page = (await TreePage()).Data!;

        page.Items.Should().BeEmpty();
        page.NextCursor.Should().BeNull();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(200, true)]
    [InlineData(0, false)]
    [InlineData(201, false)]
    [InlineData(-5, false)]
    public void Tree_LimitIsBetweenOneAndTwoHundred(int limit, bool valid) =>
        new GetSpaceTreeQueryValidator().Validate(new GetSpaceTreeQuery(TestWorkspaceId, null, limit)).IsValid.Should().Be(valid);

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("MTIzOjQ1Ng")]
    [InlineData("!!!")]
    public void Tree_ACursorTheListDidNotIssue_IsRejected(string cursor) =>
        new GetSpaceTreeQueryValidator().Validate(new GetSpaceTreeQuery(TestWorkspaceId, cursor, null)).IsValid.Should().BeFalse();

    [Fact]
    public async Task Tree_TheCursorTheListIssued_IsAccepted()
    {
        SeedMany(3);
        var cursor = (await TreePage(limit: 1)).Data!.NextCursor;

        new GetSpaceTreeQueryValidator().Validate(new GetSpaceTreeQuery(TestWorkspaceId, cursor, 1)).IsValid.Should().BeTrue();
    }

    // ── List ────────────────────────────────────────────────

    [Fact]
    public async Task List_DefaultPageHoldsFifty_AndTheNextCursorLeadsToTheRest()
    {
        SeedMany(60);

        var first = (await ListPage()).Data!;
        first.Items.Should().HaveCount(50);
        first.NextCursor.Should().NotBeNullOrEmpty();

        var second = (await ListPage(first.NextCursor)).Data!;
        second.Items.Should().HaveCount(10);
        second.NextCursor.Should().BeNull("the last page has no cursor");
        first.Items.Select(s => s.Id).Intersect(second.Items.Select(s => s.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task List_APageThatEndsExactlyOnTheLastSpace_HasNoCursor()
    {
        SeedMany(3);

        var page = (await ListPage(limit: 3)).Data!;

        page.Items.Should().HaveCount(3);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task List_ALimitOfOne_ReturnsOneSpace_AndACursor()
    {
        SeedMany(3);

        var page = (await ListPage(limit: 1)).Data!;

        page.Items.Should().ContainSingle();
        page.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task List_IsOrderedByName()
    {
        Seed("Charlie");
        Seed("Alpha");
        Seed("Bravo");

        var page = (await ListPage()).Data!;

        page.Items.Select(s => s.Name).Should().Equal("Alpha", "Bravo", "Charlie");
    }

    [Fact]
    public async Task List_SpacesWithTheSameName_AreEachListedOnce_WhenWalkedOneAtATime()
    {
        Enumerable.Range(0, 7).ToList().ForEach(_ => Seed("Same name"));

        var walked = await Walk(async c => (await ListPage(c, 1)).Data!);

        walked.Should().HaveCount(7);
        walked.Select(s => s.Id).Distinct().Should().HaveCount(7, "a tie on name must not repeat or skip a Space");
        walked.Select(s => s.Id).Should().BeInAscendingOrder("the id breaks a tie on name");
    }

    [Fact]
    public async Task List_NeverReadsAHiddenSpace_OnAnyPage()
    {
        SeedMany(5);
        var hidden = Seed("Hidden", visible: false);

        var walked = await Walk(async c => (await ListPage(c, 2)).Data!);

        walked.Should().HaveCount(5);
        walked.Select(s => s.Id).Should().NotContain(hidden.Id);
    }

    [Fact]
    public async Task List_WithNothingVisible_IsAnEmptyPageWithNoCursor()
    {
        Seed("Hidden", visible: false);

        var page = (await ListPage()).Data!;

        page.Items.Should().BeEmpty();
        page.NextCursor.Should().BeNull();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(200, true)]
    [InlineData(0, false)]
    [InlineData(201, false)]
    [InlineData(-5, false)]
    public void List_LimitIsBetweenOneAndTwoHundred(int limit, bool valid) =>
        new GetWorkspaceSpacesQueryValidator().Validate(new GetWorkspaceSpacesQuery(TestWorkspaceId, null, limit)).IsValid.Should().Be(valid);

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("MTIzOjQ1Ng")]
    [InlineData("!!!")]
    public void List_ACursorTheListDidNotIssue_IsRejected(string cursor) =>
        new GetWorkspaceSpacesQueryValidator().Validate(new GetWorkspaceSpacesQuery(TestWorkspaceId, cursor, null)).IsValid.Should().BeFalse();

    [Fact]
    public async Task List_TheCursorTheListIssued_IsAccepted()
    {
        SeedMany(3);
        var cursor = (await ListPage(limit: 1)).Data!.NextCursor;

        new GetWorkspaceSpacesQueryValidator().Validate(new GetWorkspaceSpacesQuery(TestWorkspaceId, cursor, 1)).IsValid.Should().BeTrue();
    }

    // ── Members ─────────────────────────────────────────────

    private Space SeedWithMembers(int others)
    {
        var space = Seed("Finance");
        for (var n = 0; n < others; n++)
            space.AddMember(SpaceMemberSubjectType.User, Guid.CreateVersion7(), SpaceRole.Member, TestUserId, TestNow);
        return space;
    }

    private Task<Result<CursorPage<SpaceMemberDto>>> MemberPage(Space space, string? cursor = null, int? limit = null)
    {
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ActorSnapshot>());
        return new GetSpaceMembersQueryHandler(_store, _visibility, actors.Object, RequestContextMock.Object)
            .Handle(new GetSpaceMembersQuery(TestWorkspaceId, space.Id, cursor, limit), CancellationToken.None);
    }

    [Fact]
    public async Task Members_DefaultPageHoldsFifty_AndTheNextCursorLeadsToTheRest()
    {
        var space = SeedWithMembers(59);

        var first = (await MemberPage(space)).Data!;
        first.Items.Should().HaveCount(50);
        first.NextCursor.Should().NotBeNullOrEmpty();

        var second = (await MemberPage(space, first.NextCursor)).Data!;
        second.Items.Should().HaveCount(10, "the creator plus 59 others is 60 entries");
        second.NextCursor.Should().BeNull();
        first.Items.Select(m => m.SubjectId).Intersect(second.Items.Select(m => m.SubjectId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Members_WalkingOneAtATime_ReadsEveryEntryOnce_InSubjectIdOrder()
    {
        var space = SeedWithMembers(8);

        var walked = await Walk(async c => (await MemberPage(space, c, 1)).Data!);

        walked.Should().HaveCount(9);
        walked.Select(m => m.SubjectId).Distinct().Should().HaveCount(9);
        walked.Select(m => m.SubjectId).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Members_OfASpaceTheCallerCannotSee_AreNotFound()
    {
        var hidden = Seed("Hidden", visible: false);

        Func<Task> act = () => MemberPage(hidden);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Members_Validator_RefusesAForeignCursorAndAnOutOfRangeLimit()
    {
        var validator = new GetSpaceMembersQueryValidator();

        validator.Validate(new GetSpaceMembersQuery(TestWorkspaceId, Guid.NewGuid(), "not-a-cursor")).IsValid.Should().BeFalse();
        validator.Validate(new GetSpaceMembersQuery(TestWorkspaceId, Guid.NewGuid(), null, 201)).IsValid.Should().BeFalse();
        validator.Validate(new GetSpaceMembersQuery(TestWorkspaceId, Guid.NewGuid(), null, 200)).IsValid.Should().BeTrue();
    }
}
