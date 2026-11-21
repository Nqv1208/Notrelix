using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardPeople;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// <c>GET /api/v1/boards/{boardId}/people</c> on PostgreSQL through the real pipeline: the board's people are
/// listed with a name and no email, only a manager reads the per-person fields, a caller who cannot read the
/// board (an outsider, another workspace, a member with no access) gets the identical NotFound, and a person
/// with no role or grant on the board is never listed, not even to a Guest.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardPeopleIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BoardPeopleIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Manager_ListsEveryBoardPerson_WithNameAndLevels_AndNobodyWithoutARoleOrGrant()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var lead = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var bystander = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Bystander");
        var board = await _host.SeedBoardAsync(stack, "Ledger", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, lead, BoardRole.Owner);
        await _host.SeedBoardMemberAsync(board, editor, BoardRole.Member);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Viewer);

        var people = await GetAsync(stack, lead, board);

        people.Select(p => p.UserId).Should().BeEquivalentTo([lead, editor, guest]);
        people.Should().NotContain(p => p.UserId == bystander, "a workspace member with no role or grant on the board is not one of its people");
        people.Should().OnlyContain(p => p.Name.StartsWith("Org "), "each person is named from the profile lookup");
        people.Single(p => p.UserId == guest).IsGuest.Should().BeTrue();
        people.Single(p => p.UserId == guest).IsBoardMember.Should().BeFalse();
        people.Single(p => p.UserId == editor).IsBoardMember.Should().BeTrue();
        people.Should().OnlyContain(p => p.Level != null, "a manager reads each person's level");
    }

    [Fact]
    public async Task AGuestWithAGrant_ListsOnlyThePeopleOnTheBoard_WithoutLevelsAndNeverTheWorkspaceDirectory()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var lead = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var bystander = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Bystander");
        var board = await _host.SeedBoardAsync(stack, "Ledger", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, lead, BoardRole.Owner);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Editor);

        var people = await GetAsync(stack, guest, board);

        people.Select(p => p.UserId).Should().BeEquivalentTo([lead, guest]);
        people.Should().NotContain(p => p.UserId == bystander, "no workspace directory reaches a Guest");
        people.Should().OnlyContain(p => p.Level == null && p.IsGuest == null && p.IsBoardMember == null,
            "a person who does not manage the board reads names only");
        people.Should().OnlyContain(p => p.Name.StartsWith("Org "));
    }

    [Fact]
    public async Task ABoardMember_ListsThePeople_WithoutLevels()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var lead = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var board = await _host.SeedBoardAsync(stack, "Roadmap", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, lead, BoardRole.Owner);
        await _host.SeedBoardMemberAsync(board, member, BoardRole.Member);

        var people = await GetAsync(stack, member, board);

        people.Select(p => p.UserId).Should().BeEquivalentTo([lead, member]);
        people.Should().OnlyContain(p => p.Level == null && p.IsGuest == null && p.IsBoardMember == null);
    }

    [Fact]
    public async Task CallersWhoCannotReadTheBoard_AreNotFound_OutsiderOtherWorkspaceAndANonPartyGuest()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var hidden = await _host.SeedBoardAsync(stack, "Salaries", BoardVisibility.Private);
        var shared = await _host.SeedBoardAsync(stack, "Announcements");
        var foreign = await _host.SeedBoardAsync(other, "Foreign");

        foreach (var (user, board) in new[] { (member, hidden), (guest, shared), (guest, hidden), (outsider, shared) })
        {
            using var provider = CreateProvider(stack, user);
            var act = () => OrgTreeTestHost.SendAsync(provider, new GetBoardPeopleQuery(board));
            await act.Should().ThrowAsync<AppNotFound>("a board the caller cannot read answers like a missing one");
        }

        using var asOwner = CreateProvider(stack, stack.OwnerId);
        var cross = () => OrgTreeTestHost.SendAsync(asOwner, new GetBoardPeopleQuery(foreign));
        await cross.Should().ThrowAsync<AppNotFound>("a board of another workspace is unreachable");
    }

    [Fact]
    public async Task AReaderOfAWorkspaceBoardWhoIsNoPartyToIt_IsNotFound_SoNoNameLeaks()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var reader = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Reader");
        var grantee = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Grantee");
        var board = await _host.SeedBoardAsync(stack, "Announcements");
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, grantee, PermissionLevel.Manager);

        using var provider = CreateProvider(stack, reader);
        var act = () => OrgTreeTestHost.SendAsync(provider, new GetBoardPeopleQuery(board));

        await act.Should().ThrowAsync<AppNotFound>("a reader who is neither a member nor a grant holder reads counts in the sharing view, never the people");
    }

    private async Task<List<Notrelix.Application.Features.WorkManagement.Common.DTOs.BoardPersonDto>> GetAsync(OrgStack stack, Guid userId, Guid boardId)
    {
        using var provider = CreateProvider(stack, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetBoardPeopleQuery(boardId));
    }

    private ServiceProvider CreateProvider(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IBoardAccessSubjectFacts, BoardAccessSubjectFactsAdapter>();
            services.AddScoped<IResourceAccessEntriesReader, PostgresResourceAccessEntriesReader>();
            services.AddScoped<IResourceAccessSummary, ResourceAccessSummaryService>();
            services.AddScoped<IBoardSharingSummary>(sp => (ResourceAccessSummaryService)sp.GetRequiredService<IResourceAccessSummary>());
        });
}
