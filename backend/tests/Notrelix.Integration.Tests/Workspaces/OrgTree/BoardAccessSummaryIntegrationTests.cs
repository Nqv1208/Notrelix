using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardAccess;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Board access summary on PostgreSQL through the real pipeline, facts query,
/// engine and access-entries SQL: the caller's level and source, NotFound for a
/// caller without the single read (identical to a missing Board), people and
/// guest counts that count a person once, and the completeness flag.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardAccessSummaryIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BoardAccessSummaryIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExplicitCommenter_IsCommenter_WithTheGrantor_AndAPrivateBoardCountIsComplete()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var commenter = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Commenter");
        var board = await _host.SeedBoardAsync(stack, "Audit", BoardVisibility.Private);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, commenter, PermissionLevel.Commenter);

        var access = await GetAccessAsync(stack, commenter, board);

        access.MyLevel.Should().Be("commenter");
        access.MySource.Kind.Should().Be("explicit");
        access.MySource.GrantedBy.Should().Be(new BoardAccessGrantorDto(stack.OwnerId, "Org Owner"));
        access.PeopleCount.Should().Be(2, "the workspace Owner and the grantee can open the Private board");
        access.GuestCount.Should().Be(0);
        access.PeopleCountComplete.Should().BeTrue();
        access.Sources.Sum(s => s.Count).Should().Be(access.PeopleCount);
        access.Sources.Should().NotContain(s => s.Name != null, "a commenter cannot see who holds grants");
    }

    [Fact]
    public async Task WorkspaceOwner_IsManager_FromTheOwnerSource()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var board = await _host.SeedBoardAsync(stack, "Roadmap");

        var access = await GetAccessAsync(stack, stack.OwnerId, board);

        access.MyLevel.Should().Be("manager");
        access.MySource.Kind.Should().Be("owner");
        access.PeopleCountComplete.Should().BeFalse("an unplaced Workspace-visible board reaches every member implicitly");
    }

    [Fact]
    public async Task SpaceMember_IsEditor_FromTheSpace_AndTheSpaceIsNamed()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var engineer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Engineer");
        var space = await _host.SeedSpaceAsync(stack, "Phòng Công nghệ", SpaceVisibility.Private, stack.OwnerId,
            transition: s => s.AddMember(SpaceMemberSubjectType.User, engineer, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime));
        var board = await _host.SeedBoardAsync(stack, "Sprint", spaceId: space);

        var access = await GetAccessAsync(stack, engineer, board);

        access.MyLevel.Should().Be("editor");
        access.MySource.Should().Be(new BoardAccessSourceDto("space", null));
        access.Sources.Should().ContainSingle(s => s.Kind == "space")
            .Which.Should().Be(new BoardAccessSourceCountDto("space", space, "Phòng Công nghệ", 2),
                "the Space creator (its first Admin, also the workspace Owner) and the engineer are its direct entries");
        access.PeopleCount.Should().Be(2);
        access.PeopleCountComplete.Should().BeFalse();
    }

    [Fact]
    public async Task Guest_WithAGrant_HasTheGuestSource_AndIsCountedAsAGuest()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var board = await _host.SeedBoardAsync(stack, "Shared");
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Editor);

        var access = await GetAccessAsync(stack, guest, board);

        access.MyLevel.Should().Be("editor");
        access.MySource.Should().Be(new BoardAccessSourceDto("guest", new BoardAccessGrantorDto(stack.OwnerId, "Org Owner")));
        access.GuestCount.Should().Be(1);
        access.Sources.Should().Contain(new BoardAccessSourceCountDto("guest", null, null, 1));
    }

    [Fact]
    public async Task CallerWithoutAccess_GetsTheSameNotFound_AsForAMissingBoard()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Outsider");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var privateBoard = await _host.SeedBoardAsync(stack, "Salaries", BoardVisibility.Private);
        var workspaceBoard = await _host.SeedBoardAsync(stack, "Announcements");

        var hidden = await CaptureNotFoundAsync(stack, outsider, privateBoard);
        var missing = await CaptureNotFoundAsync(stack, outsider, Guid.NewGuid());
        var guestHidden = await CaptureNotFoundAsync(stack, guest, workspaceBoard);

        hidden.Should().BeOfType(missing.GetType());
        guestHidden.Should().BeOfType(missing.GetType(), "a Guest needs explicit access (GOV-025 step 6)");
    }

    [Fact]
    public async Task APersonReachableByTwoSources_IsCountedOnce()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var linh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Linh");
        var hoa = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Hoa");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Nhóm Kế toán", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(linh, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(hoa, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(guest, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        var board = await _host.SeedBoardAsync(stack, "Ledger", BoardVisibility.Private);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.Team, team.Id, PermissionLevel.Editor);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, linh, PermissionLevel.Manager);
        await _host.SeedBoardMemberAsync(board, linh, BoardRole.Member);
        await _host.SeedBoardMemberAsync(board, hoa, BoardRole.Member);

        var access = await GetAccessAsync(stack, linh, board);

        access.MyLevel.Should().Be("manager");
        access.MySource.Kind.Should().Be("explicit", "the User grant (Manager) is higher than the Team grant");
        access.PeopleCount.Should().Be(4, "Owner, Linh, Hoa and the guest — each once");
        access.GuestCount.Should().Be(1);
        access.Sources.Should().Equal(
            new BoardAccessSourceCountDto("team", team.Id, "Nhóm Kế toán", 2),
            new BoardAccessSourceCountDto("user", stack.OwnerId, "Org Owner", 1),
            new BoardAccessSourceCountDto("guest", null, null, 1));
        access.PeopleCountComplete.Should().BeTrue();
    }

    [Fact]
    public async Task AnApplicableReadRule_MakesThePrivateCountIncomplete()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var reader = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Reader");
        var board = await _host.SeedBoardAsync(stack, "Audit", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, member, BoardRole.Member);
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, reader, PermissionAction.ViewBoard, PermissionEffect.Allow,
            "work-management.board", board);

        var access = await GetAccessAsync(stack, member, board);

        access.PeopleCountComplete.Should().BeFalse();
    }

    private async Task<BoardAccessDto> GetAccessAsync(OrgStack stack, Guid userId, Guid boardId)
    {
        using var provider = CreateProvider(stack, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetBoardAccessQuery(boardId));
    }

    private async Task<Exception> CaptureNotFoundAsync(OrgStack stack, Guid userId, Guid boardId)
    {
        using var provider = CreateProvider(stack, userId);
        var act = () => OrgTreeTestHost.SendAsync(provider, new GetBoardAccessQuery(boardId));
        return (await act.Should().ThrowAsync<AppNotFound>()).Which;
    }

    private ServiceProvider CreateProvider(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IBoardAccessSubjectFacts, BoardAccessSubjectFactsAdapter>();
            services.AddScoped<IResourceAccessEntriesReader, PostgresResourceAccessEntriesReader>();
            services.AddScoped<IResourceAccessSummary, ResourceAccessSummaryService>();
        });
}
