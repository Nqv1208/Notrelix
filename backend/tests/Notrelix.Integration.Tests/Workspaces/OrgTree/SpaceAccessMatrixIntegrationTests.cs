using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RenameSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ArchiveTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Space-scoped authorization matrix on PostgreSQL (GOV-025..GOV-029, ADR-009):
/// workspace Owner/Admin/Member/Guest × Space Admin/Member/Viewer/none × direct
/// or Team-derived entries, Private child boundaries, Private boards inside a
/// Space, an explicit cross-department grant, and revocation through Team
/// membership/lifecycle. Every list is checked against the single-read engine
/// decision for the same principal (parity), and management actions against
/// the GOV-028 authority table.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SpaceAccessMatrixIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public SpaceAccessMatrixIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record Org(
        OrgStack Stack,
        Guid Admin,
        Guid AccountingAdmin,
        Guid TeamMember,
        Guid PayrollClerk,
        Guid GuestViewer,
        Guid ItEngineer,
        Guid Unassigned,
        Guid AccountingTeam,
        Guid Accounting,
        Guid Payroll,
        Guid Reports,
        Guid It,
        Guid Company,
        Guid AccountingBoard,
        Guid PayrollBoard,
        Guid ReportsBoard,
        Guid AccountingPrivateBoard,
        Guid ItBoard,
        Guid CompanyBoard);

    /// <summary>
    /// Accounting (Department, Private): Admin = AccountingAdmin, Member = Team
    /// "Accounting team" (TeamMember), Viewer = GuestViewer (a workspace Guest).
    ///   ├─ Payroll (Private, own list: Admin = Owner, Member = PayrollClerk)
    ///   └─ Reports (Workspace-visible: inherits Accounting)
    /// IT (Department, Private): Admin = ItEngineer.
    /// Company (Folder, Workspace-visible root): open to workspace Members.
    /// Boards: one Workspace-visible board in each Space, plus a Private board in
    /// Accounting granted explicitly to ItEngineer (cross-department).
    /// </summary>
    private async Task<Org> SeedOrgAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var accountingAdmin = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "AccountingAdmin");
        var teamMember = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "TeamMember");
        var payrollClerk = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "PayrollClerk");
        var guestViewer = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "GuestViewer");
        var itEngineer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "ItEngineer");
        var unassigned = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Unassigned");

        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting team", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(teamMember, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });

        var accounting = await _host.SeedSpaceAsync(stack, "Accounting", SpaceVisibility.Private, accountingAdmin, type: SpaceType.Department,
            transition: s =>
            {
                s.AddMember(SpaceMemberSubjectType.Team, team.Id, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
                s.AddMember(SpaceMemberSubjectType.User, guestViewer, SpaceRole.Viewer, stack.OwnerId, OrgTreeTestHost.BaseTime);
            });
        // Created by the Owner so the Accounting Admin reaches Payroll only through
        // inheritance across the Private boundary (Admin passes it).
        var payroll = await _host.SeedSpaceAsync(stack, "Payroll", SpaceVisibility.Private, stack.OwnerId, accounting,
            transition: s => s.AddMember(SpaceMemberSubjectType.User, payrollClerk, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime));
        var reports = await _host.SeedSpaceAsync(stack, "Reports", SpaceVisibility.Workspace, accountingAdmin, accounting);
        var it = await _host.SeedSpaceAsync(stack, "IT", SpaceVisibility.Private, itEngineer, type: SpaceType.Department);
        var company = await _host.SeedSpaceAsync(stack, "Company", SpaceVisibility.Workspace, stack.OwnerId);

        var accountingBoard = await _host.SeedBoardAsync(stack, "Ledger", spaceId: accounting);
        var payrollBoard = await _host.SeedBoardAsync(stack, "Salaries", spaceId: payroll);
        var reportsBoard = await _host.SeedBoardAsync(stack, "Quarterly", spaceId: reports);
        var accountingPrivate = await _host.SeedBoardAsync(stack, "Audit findings", BoardVisibility.Private, accounting);
        var itBoard = await _host.SeedBoardAsync(stack, "Incidents", spaceId: it);
        var companyBoard = await _host.SeedBoardAsync(stack, "Announcements", spaceId: company);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", accountingPrivate, PermissionSubjectType.User, itEngineer, PermissionLevel.Viewer);

        return new Org(stack, admin, accountingAdmin, teamMember, payrollClerk, guestViewer, itEngineer, unassigned, team.Id,
            accounting, payroll, reports, it, company,
            accountingBoard, payrollBoard, reportsBoard, accountingPrivate, itBoard, companyBoard);
    }

    [Fact]
    public async Task BoardAndSpaceVisibility_FollowsTheMatrix_AndListsEqualSingleReads()
    {
        var org = await SeedOrgAsync();
        var allBoards = new[] { org.AccountingBoard, org.PayrollBoard, org.ReportsBoard, org.AccountingPrivateBoard, org.ItBoard, org.CompanyBoard };
        var allSpaces = new[] { org.Accounting, org.Payroll, org.Reports, org.It, org.Company };

        var expectations = new (string Who, Guid User, Guid[] Boards, Guid[] Spaces)[]
        {
            ("workspace Owner", org.Stack.OwnerId, allBoards, allSpaces),
            ("workspace Admin (no Private board grant)", org.Admin,
                [org.AccountingBoard, org.PayrollBoard, org.ReportsBoard, org.ItBoard, org.CompanyBoard], allSpaces),
            ("Accounting Space Admin (Admin passes the Private Payroll boundary)", org.AccountingAdmin,
                [org.AccountingBoard, org.PayrollBoard, org.ReportsBoard, org.CompanyBoard], [org.Accounting, org.Payroll, org.Reports, org.Company]),
            ("Accounting Member through a Team (stopped by Private Payroll)", org.TeamMember,
                [org.AccountingBoard, org.ReportsBoard, org.CompanyBoard], [org.Accounting, org.Reports, org.Company]),
            ("Payroll-only member", org.PayrollClerk,
                [org.PayrollBoard, org.CompanyBoard], [org.Payroll, org.Company]),
            ("workspace Guest with an Accounting Viewer entry (no open-Space default)", org.GuestViewer,
                [org.AccountingBoard, org.ReportsBoard], [org.Accounting, org.Reports]),
            ("IT Admin with a cross-department grant", org.ItEngineer,
                [org.AccountingPrivateBoard, org.ItBoard, org.CompanyBoard], [org.It, org.Company]),
            ("workspace Member in no department", org.Unassigned,
                [org.CompanyBoard], [org.Company]),
        };

        foreach (var (who, user, boards, spaces) in expectations)
        {
            using var provider = _host.CreateProvider(org.Stack.AccountId, user);

            var listed = (await OrgTreeTestHost.QueryAsync(provider, new GetBoardsQuery(org.Stack.WorkspaceId))).Select(b => b.Id).ToList();
            var singles = new List<Guid>();
            foreach (var board in allBoards)
            {
                if (await SucceedsAsync(provider, new GetBoardQuery(board)))
                    singles.Add(board);
            }

            listed.Should().BeEquivalentTo(singles, $"{who}: the Board list equals the single-read decision (GOV-029)");
            listed.Should().BeEquivalentTo(boards, $"{who}: visible Boards");

            var tree = (await OrgTreeTestHost.QueryAsync(provider, new GetSpaceTreeQuery(org.Stack.WorkspaceId))).Items.Select(n => n.Id).ToList();
            var singleSpaces = new List<Guid>();
            foreach (var space in allSpaces)
            {
                if (await SucceedsAsync(provider, new GetSpaceQuery(org.Stack.WorkspaceId, space)))
                    singleSpaces.Add(space);
            }

            tree.Should().BeEquivalentTo(singleSpaces, $"{who}: the Space tree equals the single Space read");
            tree.Should().BeEquivalentTo(spaces, $"{who}: visible Spaces");
        }
    }

    [Fact]
    public async Task SpaceLevels_GateBoardActions_AndTheTreeReportsTheCallersLevel()
    {
        var org = await SeedOrgAsync();

        using (var guest = _host.CreateProvider(org.Stack.AccountId, org.GuestViewer))
        {
            var update = () => UpdateBoardAsync(guest, org.AccountingBoard, "Viewer cannot rename");
            await update.Should().ThrowAsync<AppForbidden>("a Space Viewer only reads (GOV-027)");
            (await OrgTreeTestHost.QueryAsync(guest, new GetSpaceTreeQuery(org.Stack.WorkspaceId))).Items
                .Single(n => n.Id == org.Accounting).AccessLevel.Should().Be("Viewer");
        }

        using (var member = _host.CreateProvider(org.Stack.AccountId, org.TeamMember))
        {
            // Renaming the Board is Board management, which needs Manager; a Space
            // Member is an Editor (content only). Previously this rename passed.
            var rename = () => UpdateBoardAsync(member, org.AccountingBoard, "Ledger 2027");
            await rename.Should().ThrowAsync<AppForbidden>("a Space Member edits content, not the Board's settings");
            (await OrgTreeTestHost.QueryAsync(member, new GetSpaceTreeQuery(org.Stack.WorkspaceId))).Items
                .Single(n => n.Id == org.Accounting).AccessLevel.Should().Be("Editor");
        }

        using (var spaceAdmin = _host.CreateProvider(org.Stack.AccountId, org.AccountingAdmin))
        {
            await UpdateBoardAsync(spaceAdmin, org.AccountingBoard, "Ledger 2027");
        }

        using (var grantee = _host.CreateProvider(org.Stack.AccountId, org.ItEngineer))
        {
            (await SucceedsAsync(grantee, new GetBoardQuery(org.AccountingPrivateBoard))).Should().BeTrue("the explicit grant reaches the Private board");
            (await SucceedsAsync(grantee, new GetBoardQuery(org.AccountingBoard))).Should().BeFalse("the grant does not open the rest of the department");
        }
    }

    [Fact]
    public async Task SpaceManagement_FollowsTheGov028AuthorityTable()
    {
        var org = await SeedOrgAsync();
        var ws = org.Stack.WorkspaceId;

        using var accountingAdmin = _host.CreateProvider(org.Stack.AccountId, org.AccountingAdmin);
        await OrgTreeTestHost.SendAsync(accountingAdmin, new RenameSpaceCommand(ws, org.Payroll, "Payroll & benefits", await _host.SpaceVersionAsync(org.Payroll)));
        (await OrgTreeTestHost.QueryAsync(accountingAdmin, new CreateSpaceCommand(ws, "Tax", ParentSpaceId: org.Accounting))).Should().NotBeEmpty(
            "a Space Admin creates child Spaces in its subtree");
        var outsideSubtree = async () => await OrgTreeTestHost.SendAsync(accountingAdmin, new RenameSpaceCommand(ws, org.It, "Mine now", await _host.SpaceVersionAsync(org.It)));
        await outsideSubtree.Should().ThrowAsync<AppNotFound>("a Space outside the subtree is invisible, not merely forbidden");
        var rootSpace = () => OrgTreeTestHost.SendAsync(accountingAdmin, new CreateSpaceCommand(ws, "Legal"));
        await rootSpace.Should().ThrowAsync<AppForbidden>("root Spaces are a workspace Owner/Admin capability");

        using var teamMember = _host.CreateProvider(org.Stack.AccountId, org.TeamMember);
        var memberRename = async () => await OrgTreeTestHost.SendAsync(teamMember, new RenameSpaceCommand(ws, org.Accounting, "Not allowed", await _host.SpaceVersionAsync(org.Accounting)));
        await memberRename.Should().ThrowAsync<AppForbidden>("a Space Member sees but does not manage the Space");

        using var admin = _host.CreateProvider(org.Stack.AccountId, org.Admin);
        await OrgTreeTestHost.SendAsync(admin, new RenameSpaceCommand(ws, org.It, "Information Technology", await _host.SpaceVersionAsync(org.It)));
        (await OrgTreeTestHost.QueryAsync(admin, new CreateSpaceCommand(ws, "Legal", SpaceType: "Department"))).Should().NotBeEmpty();
        (await OrgTreeTestHost.QueryAsync(admin, new CreateTeamCommand(ws, "Auditors", null))).Should().NotBeEmpty("workspace Admins manage Teams");

        var memberTeam = () => OrgTreeTestHost.QueryAsync(teamMember, new CreateTeamCommand(ws, "Shadow team", null));
        await memberTeam.Should().ThrowAsync<AppForbidden>("Team management is a workspace Owner/Admin capability");
    }

    [Fact]
    public async Task TeamDerivedAccess_IsRevoked_WhenTheMemberLeaves_OrTheTeamIsArchived()
    {
        var org = await SeedOrgAsync();
        var ws = org.Stack.WorkspaceId;
        using var admin = _host.CreateProvider(org.Stack.AccountId, org.Admin);
        using var teamMember = _host.CreateProvider(org.Stack.AccountId, org.TeamMember);
        using var unassigned = _host.CreateProvider(org.Stack.AccountId, org.Unassigned);

        (await SucceedsAsync(teamMember, new GetBoardQuery(org.AccountingBoard))).Should().BeTrue();

        await OrgTreeTestHost.SendAsync(admin, new RemoveTeamMemberCommand(ws, org.AccountingTeam, org.TeamMember, await TeamVersionAsync(org.AccountingTeam)));
        (await SucceedsAsync(teamMember, new GetBoardQuery(org.AccountingBoard))).Should().BeFalse("leaving the Team revokes its Space role on the next evaluation");
        (await OrgTreeTestHost.QueryAsync(teamMember, new GetBoardsQuery(ws))).Should().NotContain(b => b.Id == org.AccountingBoard);

        await OrgTreeTestHost.SendAsync(admin, new AddTeamMemberCommand(ws, org.AccountingTeam, org.Unassigned, "Member", await TeamVersionAsync(org.AccountingTeam)));
        (await SucceedsAsync(unassigned, new GetBoardQuery(org.AccountingBoard))).Should().BeTrue("joining the Team grants its Space role");

        await OrgTreeTestHost.SendAsync(admin, new ArchiveTeamCommand(ws, org.AccountingTeam, await TeamVersionAsync(org.AccountingTeam)));
        (await SucceedsAsync(unassigned, new GetBoardQuery(org.AccountingBoard))).Should().BeFalse("an archived Team grants nothing");
    }

    [Fact]
    public async Task AddingANonMemberToATeam_IsRejected()
    {
        var org = await SeedOrgAsync();
        var outsider = await _host.AddOutsiderAsync(org.Stack);
        using var admin = _host.CreateProvider(org.Stack.AccountId, org.Admin);
        var version = await TeamVersionAsync(org.AccountingTeam);

        var act = () => OrgTreeTestHost.SendAsync(admin, new AddTeamMemberCommand(org.Stack.WorkspaceId, org.AccountingTeam, outsider, "Member", version));

        await act.Should().ThrowAsync<AppNotFound>("a Team member is an active member of the same Workspace (WSP-014)");
    }

    private Task<long> TeamVersionAsync(Guid teamId) =>
        _host.ReadAsync(c => c.Teams.Where(t => t.Id == teamId).Select(t => t.Version).SingleAsync());

    private async Task UpdateBoardAsync(ServiceProvider provider, Guid boardId, string title)
    {
        var version = await _host.ReadAsync(c => c.Boards.Where(b => b.Id == boardId).Select(b => b.Version).SingleAsync());
        await OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardCommand(boardId, title, null, null, null, version));
    }

    private static async Task<bool> SucceedsAsync(ServiceProvider provider, object request)
    {
        try
        {
            await OrgTreeTestHost.SendAsync(provider, request);
            return true;
        }
        catch (AppNotFound)
        {
            return false;
        }
        catch (AppForbidden)
        {
            return false;
        }
    }
}
