using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.MakeBoardPrivate;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardSharing;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;
using Notrelix.Domain.Governance.AccessChanges;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// CTR-GAP-WM-BOARD-SHARING on PostgreSQL through the real pipeline (NRX-003, NRX-004, GOV-025, GOV-030,
/// WSP-026): entries and names only for managers and board members, counts for everyone else who can read
/// the Board, NotFound for everyone who cannot; entry id = user id or Team id; a Guest is identified by
/// membership and capped at Viewer; email only for Owner/Admin and never for a Guest; the log and the
/// Private impact only for managers; and the atomic make-private command.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardSharingIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BoardSharingIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Manager_SeesEveryEntry_OnePerPersonAtTheHighestLevel_WithGuestCapAndEntryIds()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var lead = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var both = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Both");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var linh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Linh");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(linh, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        var board = await _host.SeedBoardAsync(stack, "Ledger", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, lead, BoardRole.Owner);
        await _host.SeedBoardMemberAsync(board, both, BoardRole.Observer);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, both, PermissionLevel.Editor);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Manager);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.Team, team.Id, PermissionLevel.Viewer);

        var sharing = await GetAsync(stack, lead, board);

        sharing.CanManage.Should().BeTrue();
        sharing.EntriesDisclosed.Should().BeTrue();
        sharing.Visibility.Should().Be("Private");
        sharing.Entries.Select(e => e.Id).Should().BeEquivalentTo([lead, both, guest, team.Id]);
        sharing.Entries.Should().OnlyHaveUniqueItems(e => e.Id, "one entry per person or Team");

        var owner = sharing.Entries.Single(e => e.Id == lead);
        owner.Owner.Should().BeTrue();
        owner.Level.Should().Be("manager");
        owner.Source.Should().Be("member");
        owner.MemberRole.Should().Be("Owner");
        owner.You.Should().BeTrue();
        sharing.Entries.First().Id.Should().Be(lead, "the owner is listed first");

        var combined = sharing.Entries.Single(e => e.Id == both);
        combined.Source.Should().Be("both");
        combined.Level.Should().Be("viewer", "a read-only board role caps the person at Viewer over an explicit grant");
        combined.PermissionId.Should().NotBeNull();

        var capped = sharing.Entries.Single(e => e.Id == guest);
        capped.Kind.Should().Be("guest", "a Guest is the Workspace role, never an email domain");
        capped.Level.Should().Be("viewer");
        capped.LevelCapped.Should().BeTrue("a stored Manager level is capped at Viewer for a Guest (GOV-030)");
        capped.ExpiresAt.Should().BeNull();

        var teamEntry = sharing.Entries.Single(e => e.Id == team.Id);
        teamEntry.Kind.Should().Be("team");
        teamEntry.Name.Should().Be("Accounting");
        teamEntry.TeamPeople.Should().Be(1);
        teamEntry.Level.Should().Be("viewer");

        sharing.PeopleCount.Should().BeGreaterThanOrEqualTo(5);
        sharing.GuestCount.Should().Be(1);
        sharing.PrivateImpact.Should().NotBeNull();
    }

    [Fact]
    public async Task BoardMember_SeesEntries_ButNoLogNoImpact_AndNoEmailWithoutAnAdminRole()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other");
        var board = await _host.SeedBoardAsync(stack, "Roadmap", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, editor, BoardRole.Member);
        await _host.SeedBoardMemberAsync(board, other, BoardRole.Member);

        var sharing = await GetAsync(stack, editor, board);

        sharing.CanManage.Should().BeFalse();
        sharing.EntriesDisclosed.Should().BeTrue("a board member sees who else is on the board");
        sharing.Entries.Select(e => e.Id).Should().BeEquivalentTo([editor, other]);
        sharing.Entries.Should().OnlyContain(e => e.Email == null, "only an Owner or Admin sees email");
        sharing.Log.Should().BeEmpty();
        sharing.PrivateImpact.Should().BeNull();
    }

    [Fact]
    public async Task ANonMemberReaderOfAWorkspaceBoard_GetsCountsOnly_NoNamesNoEntries()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var reader = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Reader");
        var grantee = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Grantee");
        var board = await _host.SeedBoardAsync(stack, "Announcements");
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, grantee, PermissionLevel.Manager);

        var sharing = await GetAsync(stack, reader, board);

        sharing.CanManage.Should().BeFalse();
        sharing.EntriesDisclosed.Should().BeFalse();
        sharing.Entries.Should().BeEmpty("names are disclosed to managers and board members only");
        sharing.Log.Should().BeEmpty();
        sharing.PrivateImpact.Should().BeNull();
        sharing.PeopleCount.Should().BeGreaterThan(0, "the count of people is still given");
    }

    [Fact]
    public async Task OwnerAndAdmin_SeeEmailOfNonGuestPeople_AndNeverOfAGuest()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var board = await _host.SeedBoardAsync(stack, "Shared", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, admin, BoardRole.Owner);
        await _host.SeedBoardMemberAsync(board, member, BoardRole.Member);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Editor);

        foreach (var reader in new[] { stack.OwnerId, admin })
        {
            var sharing = await GetAsync(stack, reader, board);
            sharing.Entries.Single(e => e.Id == member).Email.Should().NotBeNullOrEmpty().And.EndWith("@example.com");
            sharing.Entries.Single(e => e.Id == guest).Email.Should().BeNull("a Guest's email is never shown");
        }

        var asMember = await GetAsync(stack, member, board);
        asMember.Entries.Should().OnlyContain(e => e.Email == null);
    }

    [Fact]
    public async Task CallersWithoutTheSingleRead_AreNotFound_GuestOutsiderAndOtherWorkspace()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var hidden = await _host.SeedBoardAsync(stack, "Salaries", BoardVisibility.Private);
        var shared = await _host.SeedBoardAsync(stack, "Announcements");
        var foreign = await _host.SeedBoardAsync(other, "Foreign");

        foreach (var (user, board) in new[] { (member, hidden), (guest, shared), (guest, hidden) })
        {
            using var provider = CreateProvider(stack, user);
            var act = () => OrgTreeTestHost.SendAsync(provider, new GetBoardSharingQuery(board));
            await act.Should().ThrowAsync<AppNotFound>("a board the caller cannot read answers like a missing one");
        }

        using (var asOutsider = CreateProvider(stack, outsider))
        {
            var act = () => OrgTreeTestHost.SendAsync(asOutsider, new GetBoardSharingQuery(shared));
            await act.Should().ThrowAsync<AppForbidden>();
        }

        using var asOwner = CreateProvider(stack, stack.OwnerId);
        var cross = () => OrgTreeTestHost.SendAsync(asOwner, new GetBoardSharingQuery(foreign));
        await cross.Should().ThrowAsync<AppNotFound>("a board of another workspace is unreachable");
    }

    // Every member change moves the Board's version, so a later write names the version it has now.
    private Task<long> BoardVersionAsync(Guid board) =>
        _host.ReadAsync(c => c.Boards.Where(x => x.Id == board).Select(x => x.Version).SingleAsync());

    [Fact]
    public async Task EveryAccessChange_IsLogged_ForManagersOnly_NewestFirst_WithGuestFlag()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var worker = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Worker");
        var board = await _host.SeedBoardAsync(stack, "Plan");
        // The creator owns the Board, as creating it gives; a Board always keeps one.
        await _host.SeedBoardMemberAsync(board, stack.OwnerId, BoardRole.Owner);
        using var asOwner = CreateProvider(stack, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(asOwner, new GrantResourcePermissionCommand("work-management.board", board, "User", guest, "Manager"));
        _host.Now = _host.Now.AddMinutes(1);
        // Changing a level names the version it was read at.
        var read = (await GetAsync(stack, stack.OwnerId, board)).Entries.Single(e => e.Id == guest).PermissionVersion;
        await OrgTreeTestHost.SendAsync(asOwner, new GrantResourcePermissionCommand("work-management.board", board, "User", guest, "Viewer", ExpectedVersion: read));
        _host.Now = _host.Now.AddMinutes(1);
        await OrgTreeTestHost.SendIdempotentAsync(asOwner, new AddBoardMemberCommand(board, editor, BoardRole.Member));
        _host.Now = _host.Now.AddMinutes(1);
        await OrgTreeTestHost.SendIdempotentAsync(asOwner, new UpdateBoardMemberRoleCommand(board, editor, BoardRole.Admin, ExpectedVersion: await BoardVersionAsync(board)));
        _host.Now = _host.Now.AddMinutes(1);
        await OrgTreeTestHost.SendIdempotentAsync(asOwner, new RemoveBoardMemberCommand(board, editor, ExpectedVersion: await BoardVersionAsync(board)));
        _host.Now = _host.Now.AddMinutes(1);
        var permissionId = (await GetAsync(stack, stack.OwnerId, board)).Entries.Single(e => e.Id == guest).PermissionId!.Value;
        await OrgTreeTestHost.SendAsync(asOwner, new RevokeResourcePermissionCommand("work-management.board", board, permissionId));
        _host.Now = _host.Now.AddMinutes(1);
        await OrgTreeTestHost.SendIdempotentAsync(asOwner, new MakeBoardPrivateCommand(board, [worker]));

        var sharing = await GetAsync(stack, stack.OwnerId, board);

        sharing.Log.Select(l => l.Kind).Should().Equal(
            "private", "added", "removed", "removed", "changed", "added", "changed", "added");
        sharing.Log.Should().BeInDescendingOrder(l => l.At);
        sharing.Log.Should().OnlyContain(l => l.Actor != null && l.Actor.UserId == stack.OwnerId);
        sharing.Log.Where(l => l.Target == "Org Guest").Should().OnlyContain(l => l.Guest);
        var changed = sharing.Log.Single(l => l.Kind == "changed" && l.Target == "Org Guest");
        (changed.From, changed.To).Should().Be(("manager", "viewer"));
        sharing.Log.Single(l => l.Kind == "private").Target.Should().BeNull();

        var asWorker = await GetAsync(stack, worker, board);
        asWorker.Log.Should().BeEmpty("only managers read the log");
        asWorker.PrivateImpact.Should().BeNull();
    }

    [Fact]
    public async Task ChangesOlderThanNinetyDays_AreNotServed()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var board = await _host.SeedBoardAsync(stack, "Plan");
        var old = ResourceAccessChange.Record(
            stack.AccountId, stack.WorkspaceId, ResourceKind.Create("work-management.board"), board,
            ResourceAccessChangeKind.Added, stack.OwnerId, "User", stack.OwnerId, null, "editor",
            _host.Now - TimeSpan.FromDays(91));
        var recent = ResourceAccessChange.Record(
            stack.AccountId, stack.WorkspaceId, ResourceKind.Create("work-management.board"), board,
            ResourceAccessChangeKind.Added, stack.OwnerId, "User", stack.OwnerId, null, "editor",
            _host.Now - TimeSpan.FromDays(10));
        await _host.WithSystemContextAsync(c => { c.ResourceAccessChanges.AddRange(old, recent); return Task.CompletedTask; });

        var sharing = await GetAsync(stack, stack.OwnerId, board);

        sharing.Log.Select(l => l.Id).Should().Equal(recent.Id);
    }

    [Fact]
    public async Task PrivateImpact_NamesWhoLosesTheBoard_WithUnfinishedAndOverdueItems()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var keeps = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Keeps");
        var loses = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Loses");
        var idle = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Idle");
        var space = await _host.SeedSpaceAsync(stack, "Engineering", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department, transition: s =>
        {
            s.AddMember(SpaceMemberSubjectType.User, keeps, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, loses, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, idle, SpaceRole.Viewer, stack.OwnerId, OrgTreeTestHost.BaseTime);
        });
        var board = await _host.SeedBoardAsync(stack, "Sprint", spaceId: space);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, keeps, PermissionLevel.Editor);
        await _host.WithSystemContextAsync(c =>
        {
            var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, board, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.BoardGroups.Add(group);
            foreach (var (name, assignee, due) in new (string, Guid, int?)[] { ("Late", loses, -2), ("Ontime", loses, 5), ("Mine", keeps, null) })
            {
                var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, board, group.Id, name, FractionalIndex.Create($"a{c.BoardItems.Local.Count}"), stack.OwnerId, OrgTreeTestHost.BaseTime);
                if (due is { } days)
                    item.SetTimeline(null, _host.Now.AddDays(days), stack.OwnerId, _host.Now);
                c.BoardItems.Add(item);
                c.BoardItemMembers.Add(BoardItemMember.Create(stack.AccountId, stack.WorkspaceId, board, item.Id, assignee, stack.OwnerId, OrgTreeTestHost.BaseTime));
            }

            return Task.CompletedTask;
        });

        var sharing = await GetAsync(stack, stack.OwnerId, board);

        var impact = sharing.PrivateImpact!;
        impact.Lose.Should().Be(2, "Loses and Idle reach the Board only through the Space; Keeps has a grant, the Owner is a Workspace Owner");
        var person = impact.People.Should().ContainSingle().Subject;
        (person.UserId, person.Items, person.Overdue).Should().Be((loses, 2, 1));
        impact.Keep.Should().Contain("Org Keeps");
        sharing.SpaceName.Should().Be("Engineering");
        sharing.SpacePeople.Should().Be(4);
        sharing.Entries.Single(e => e.Id == keeps).SpaceRole.Should().Be("Member");
    }

    [Fact]
    public async Task MakePrivate_AddsPeopleAndSetsPrivateAtomically_AndAFailureLeavesTheBoardUnchanged()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var colleague = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Colleague");
        var outsider = await _host.AddOutsiderAsync(stack);
        var board = await _host.SeedBoardAsync(stack, "Roadmap");
        // The creator owns the Board, as creating it gives; a Private Board always keeps an owner.
        await _host.SeedBoardMemberAsync(board, stack.OwnerId, BoardRole.Owner);
        using var asOwner = CreateProvider(stack, stack.OwnerId);

        var failing = () => OrgTreeTestHost.SendIdempotentAsync(asOwner, new MakeBoardPrivateCommand(board, [colleague, outsider]));
        await failing.Should().ThrowAsync<Exception>("a person outside the workspace cannot be added");
        var unchanged = await _host.ReadAsync(async c => new
        {
            Visibility = (await c.Boards.SingleAsync(b => b.Id == board)).Visibility,
            Members = await c.BoardMembers.CountAsync(m => m.BoardId == board),
            Changes = await c.ResourceAccessChanges.CountAsync(x => x.ResourceId == board),
        });
        unchanged.Should().Be(new { Visibility = BoardVisibility.Workspace, Members = 1, Changes = 0 });

        await OrgTreeTestHost.SendIdempotentAsync(asOwner, new MakeBoardPrivateCommand(board, [colleague, colleague]));

        var after = await _host.ReadAsync(async c => new
        {
            Visibility = (await c.Boards.SingleAsync(b => b.Id == board)).Visibility,
            Members = await c.BoardMembers.Where(m => m.BoardId == board).Select(m => new { m.UserId, m.Role }).ToListAsync(),
        });
        after.Visibility.Should().Be(BoardVisibility.Private);
        after.Members.Should().BeEquivalentTo([new { UserId = stack.OwnerId, Role = BoardRole.Owner }, new { UserId = colleague, Role = BoardRole.Member }]);
        (await GetAsync(stack, colleague, board)).EntriesDisclosed.Should().BeTrue();
    }

    [Fact]
    public async Task MakePrivate_NeedsBoardManagement_APlainMemberAndAGuestAreRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var target = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Target");
        var board = await _host.SeedBoardAsync(stack, "Roadmap");
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Editor);

        foreach (var user in new[] { editor, guest })
        {
            using var provider = CreateProvider(stack, user);
            var act = () => OrgTreeTestHost.SendIdempotentAsync(provider, new MakeBoardPrivateCommand(board, [target]));
            await act.Should().ThrowAsync<Exception>();
        }

        var state = await _host.ReadAsync(async c => (await c.Boards.SingleAsync(b => b.Id == board)).Visibility);
        state.Should().Be(BoardVisibility.Workspace);
    }

    [Fact]
    public async Task NonManagerBoardMember_SeesEntries_WithoutPermissionIdOrGrantor_ManagerStillGetsBoth()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var grantee = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Grantee");
        var linh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Linh");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(linh, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        var board = await _host.SeedBoardAsync(stack, "Roadmap", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, member, BoardRole.Member);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, grantee, PermissionLevel.Editor);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.Team, team.Id, PermissionLevel.Viewer);

        var asMember = await GetAsync(stack, member, board);

        asMember.CanManage.Should().BeFalse();
        asMember.EntriesDisclosed.Should().BeTrue();
        asMember.Entries.Select(e => e.Id).Should().BeEquivalentTo([member, grantee, team.Id]);
        asMember.Entries.Should().OnlyContain(e => e.PermissionId == null && e.GrantedBy == null, "grant ids and grantors are management data");
        asMember.Entries.Single(e => e.Id == grantee).Level.Should().Be("editor");

        var asOwner = await GetAsync(stack, stack.OwnerId, board);
        asOwner.CanManage.Should().BeTrue();
        asOwner.Entries.Single(e => e.Id == grantee).PermissionId.Should().NotBeNull();
        asOwner.Entries.Single(e => e.Id == grantee).GrantedBy.Should().NotBeNull();
        asOwner.Entries.Single(e => e.Id == team.Id).PermissionId.Should().NotBeNull();
        asOwner.Entries.Single(e => e.Id == team.Id).GrantedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task CallerWhoCannotSeeTheSpace_GetsNoSpaceRoleOutsideFlagOrSpaceId_ManagerWhoCanStillGetsThem()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var inSpace = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "InSpace");
        var space = await _host.SeedSpaceAsync(stack, "Hidden", SpaceVisibility.Private, stack.OwnerId, transition: s =>
            s.AddMember(SpaceMemberSubjectType.User, inSpace, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime));
        var board = await _host.SeedBoardAsync(stack, "Roadmap", spaceId: space);
        await _host.SeedBoardMemberAsync(board, member, BoardRole.Member);
        await _host.SeedBoardMemberAsync(board, inSpace, BoardRole.Member);

        var blind = await GetAsync(stack, member, board);

        blind.EntriesDisclosed.Should().BeTrue();
        blind.SpaceId.Should().BeNull();
        blind.SpaceName.Should().BeNull();
        blind.SpacePeople.Should().BeNull();
        blind.Entries.Should().OnlyContain(e => e.SpaceRole == null && !e.OutsideSpace);

        var sighted = await GetAsync(stack, stack.OwnerId, board);
        sighted.SpaceId.Should().Be(space);
        sighted.Entries.Single(e => e.Id == inSpace).SpaceRole.Should().Be("Member");
        sighted.Entries.Single(e => e.Id == member).OutsideSpace.Should().BeTrue();
    }

    private async Task<BoardSharingDto> GetAsync(OrgStack stack, Guid userId, Guid boardId)
    {
        using var provider = CreateProvider(stack, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetBoardSharingQuery(boardId));
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
