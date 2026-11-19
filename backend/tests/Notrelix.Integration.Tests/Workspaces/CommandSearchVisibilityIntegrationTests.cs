using Notrelix.Application.Features.Workspaces.Search.Queries.CommandSearch;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The command palette's search on PostgreSQL through the real pipeline, the real Governance visibility and the real
/// search facts (ADR-010): a person finds only the Boards and Items they can read, a Private Board outside their reach
/// is neither listed nor counted, a Guest finds only what was shared with them, a Board shared afterwards appears at
/// once, a person who is not a member is told the workspace does not exist, and a short or empty query answers empty.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CommandSearchVisibilityIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public CommandSearchVisibilityIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record Seeded(OrgStack Stack, Guid Open, Guid Secret, Guid Member, Guid Guest, Guid Outsider);

    private async Task<Seeded> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var open = await _host.SeedBoardAsync(stack, "Zeta Roadmap");
        var secret = await _host.SeedBoardAsync(stack, "Zeta Secret", BoardVisibility.Private);
        await _host.SeedAssignedItemAsync(stack, open, stack.OwnerId, "Zeta invoice open");
        await _host.SeedAssignedItemAsync(stack, secret, stack.OwnerId, "Zeta invoice secret");
        return new Seeded(stack, open, secret, member, guest, outsider);
    }

    private async Task<CommandSearchDto> SearchAsync(OrgStack stack, Guid userId, string? text)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new CommandSearchQuery(stack.WorkspaceId, text));
    }

    [Fact]
    public async Task APrivateBoardOutsideTheCallersReach_IsNeitherFoundNorCounted_ButTheOwnerFindsIt()
    {
        var s = await SeedAsync();

        var asMember = await SearchAsync(s.Stack, s.Member, "zeta");
        asMember.Boards.Select(b => b.Id).Should().Equal(s.Open);
        asMember.Boards.Single().MatchedItems.Should().Be(1, "the count never includes an Item of a Board the caller cannot read");
        asMember.Work.Select(w => w.BoardId).Should().OnlyContain(id => id == s.Open);
        asMember.Work.Select(w => w.Title).Should().Equal("Zeta invoice open");

        var asOwner = await SearchAsync(s.Stack, s.Stack.OwnerId, "zeta");
        asOwner.Boards.Select(b => b.Id).Should().BeEquivalentTo(new[] { s.Open, s.Secret });
        asOwner.Work.Select(w => w.Title).Should().BeEquivalentTo("Zeta invoice open", "Zeta invoice secret");
    }

    [Fact]
    public async Task AGuestFindsNothingUntilABoardIsSharedWithThem_ThenOnlyThatBoard()
    {
        var s = await SeedAsync();

        var before = await SearchAsync(s.Stack, s.Guest, "zeta");
        before.Boards.Should().BeEmpty("a Guest has no Board but those shared with them");
        before.Work.Should().BeEmpty();
        before.Docs.Should().BeEmpty();

        await _host.SeedBoardMemberAsync(s.Open, s.Guest, BoardRole.Guest);
        var after = await SearchAsync(s.Stack, s.Guest, "zeta");
        after.Boards.Select(b => b.Id).Should().Equal(s.Open);
        after.Work.Select(w => w.BoardId).Should().OnlyContain(id => id == s.Open);
    }

    [Fact]
    public async Task ABoardSharedWithAMemberAfterwards_AppearsOnTheNextSearch()
    {
        var s = await SeedAsync();
        (await SearchAsync(s.Stack, s.Member, "secret")).Boards.Should().BeEmpty();

        await _host.SeedBoardMemberAsync(s.Secret, s.Member, BoardRole.Member);

        (await SearchAsync(s.Stack, s.Member, "secret")).Boards.Select(b => b.Id).Should().Equal(s.Secret);
    }

    [Fact]
    public async Task ANonMember_IsToldTheWorkspaceDoesNotExist()
    {
        var s = await SeedAsync();
        using var provider = _host.CreateProvider(s.Stack.AccountId, s.Outsider);

        var search = () => OrgTreeTestHost.QueryAsync(provider, new CommandSearchQuery(s.Stack.WorkspaceId, "zeta"));

        await search.Should().ThrowAsync<AppNotFound>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("z")]
    [InlineData("  z ")]
    public async Task AnEmptyOrShortQuery_AnswersEmptyForEveryone(string? text)
    {
        var s = await SeedAsync();

        var result = await SearchAsync(s.Stack, s.Stack.OwnerId, text);

        result.Work.Should().BeEmpty();
        result.Docs.Should().BeEmpty();
        result.Boards.Should().BeEmpty();
    }
}
