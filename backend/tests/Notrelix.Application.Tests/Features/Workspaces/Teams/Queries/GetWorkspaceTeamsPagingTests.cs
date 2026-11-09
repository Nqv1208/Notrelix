using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;

namespace Notrelix.Application.Tests.Features.Workspaces.Teams.Queries;

/// <summary>
/// The Team list is a cursor page in name order (LD-06): 50 by default and at most 200, archived Teams included.
/// Walking the pages reads every Team once.
/// </summary>
public sealed class GetWorkspaceTeamsPagingTests : WorkspaceHandlerTestBase
{
    private Task<Result<CursorPage<TeamDto>>> Page(string? cursor = null, int? limit = null) =>
        new GetWorkspaceTeamsQueryHandler(DbContextMock.Object)
            .Handle(new GetWorkspaceTeamsQuery(TestWorkspaceId, cursor, limit), CancellationToken.None);

    private void SeedTeams(int count)
    {
        var teams = Enumerable.Range(0, count)
            .Select(n => Team.Create(TestAccountId, TestWorkspaceId, $"Team {n:D3}", TestUserId, TestNow))
            .ToArray();
        SetupTeams(teams);
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

    [Fact]
    public async Task DefaultPageHoldsFifty_AndTheNextCursorLeadsToTheRest()
    {
        SeedTeams(60);

        var first = (await Page()).Data!;
        first.Items.Should().HaveCount(50);
        first.NextCursor.Should().NotBeNullOrEmpty();

        var second = (await Page(first.NextCursor)).Data!;
        second.Items.Should().HaveCount(10);
        second.NextCursor.Should().BeNull();
        first.Items.Select(t => t.Id).Intersect(second.Items.Select(t => t.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task WalkingOneAtATime_ReadsEveryTeamOnce_InNameOrder()
    {
        SeedTeams(7);

        var walked = await Walk(async c => (await Page(c, 1)).Data!);

        walked.Should().HaveCount(7);
        walked.Select(t => t.Id).Distinct().Should().HaveCount(7);
        walked.Select(t => t.Name).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task ArchivedTeams_AreStillListed()
    {
        SeedTeams(3);
        var archived = DbContextMock.Object.Teams.Single(t => t.Name == "Team 001");
        archived.Archive(TestUserId, TestNow);

        var walked = await Walk(async c => (await Page(c, 2)).Data!);

        walked.Should().HaveCount(3);
        walked.Should().Contain(t => t.Id == archived.Id && t.Status == "Archived");
    }

    [Fact]
    public void Validator_RefusesAnOutOfRangeLimit_AndAForeignCursor()
    {
        var validator = new GetWorkspaceTeamsQueryValidator();

        validator.Validate(new GetWorkspaceTeamsQuery(TestWorkspaceId, null, 201)).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspaceTeamsQuery(TestWorkspaceId, "not-a-cursor")).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspaceTeamsQuery(TestWorkspaceId, null, 200)).IsValid.Should().BeTrue();
    }
}
