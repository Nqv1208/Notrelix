using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;
using Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardAccess;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Resource levels are enforced for every access source on PostgreSQL through
/// the real pipeline: an explicit Commenter reads and comments but cannot edit
/// the Board, a board Observer only reads, a Space Member edits but cannot
/// manage permissions, a Guest with a Manager grant is capped at Editor, and the
/// workspace Owner does everything. The access summary reports the enforced
/// level, and an archived Board is NotFound for the access read exactly as for
/// the full board read.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ResourceLevelEnforcementIntegrationTests : IAsyncLifetime
{
    private const string BoardKind = "work-management.board";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public ResourceLevelEnforcementIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExplicitCommenter_ReadsAndComments_ButCannotEdit()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var commenter = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Commenter");
        var board = await _host.SeedBoardAsync(stack, "Audit", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Reconcile ledger");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, commenter, PermissionLevel.Commenter);

        using var provider = CreateProvider(stack, commenter);

        await OrgTreeTestHost.QueryAsync(provider, new GetBoardQuery(board));
        await OrgTreeTestHost.SendAsync(provider, CreateCommentCommand.ForBoardItem(item, "Looks fine", null));
        await FluentActions.Awaiting(() => UpdateBoardAsync(provider, board, "Renamed by a commenter"))
            .Should().ThrowAsync<AppForbidden>("a Commenter cannot edit the Board (Editor action)");
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardItemStatusCommand(item, "Done")))
            .Should().ThrowAsync<AppForbidden>("a Commenter cannot update an item");
        (await OrgTreeTestHost.QueryAsync(provider, new GetBoardAccessQuery(board))).MyLevel.Should().Be("commenter");
    }

    [Fact]
    public async Task BoardObserver_OnlyReads()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var observer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Observer");
        var board = await _host.SeedBoardAsync(stack, "Roadmap", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, observer, BoardRole.Observer);

        using var provider = CreateProvider(stack, observer);

        await OrgTreeTestHost.QueryAsync(provider, new GetBoardQuery(board));
        await FluentActions.Awaiting(() => UpdateBoardAsync(provider, board, "Renamed by an observer"))
            .Should().ThrowAsync<AppForbidden>();
        (await OrgTreeTestHost.QueryAsync(provider, new GetBoardAccessQuery(board))).MyLevel.Should().Be("viewer");
    }

    [Fact]
    public async Task GuestWithAManagerGrant_IsCappedAtEditor()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var colleague = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Colleague");
        var board = await _host.SeedBoardAsync(stack, "Shared", BoardVisibility.Private);
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, guest, PermissionLevel.Manager);

        using var provider = CreateProvider(stack, guest);

        await UpdateBoardAsync(provider, board, "Edited by the guest");
        await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(provider, new GetResourcePermissionsQuery(BoardKind, board)))
            .Should().ThrowAsync<AppForbidden>("a Guest never manages permissions (GOV-030)");
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider,
                new GrantResourcePermissionCommand(BoardKind, board, "User", colleague, "Viewer")))
            .Should().ThrowAsync<AppForbidden>();
        (await OrgTreeTestHost.QueryAsync(provider, new GetBoardAccessQuery(board))).MyLevel.Should().Be("editor");
    }

    [Fact]
    public async Task SpaceMember_Edits_ButCannotManagePermissions()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var space = await _host.SeedSpaceAsync(stack, "Engineering", SpaceVisibility.Private, stack.OwnerId,
            transition: s => s.AddMember(SpaceMemberSubjectType.User, editor, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime));
        var board = await _host.SeedBoardAsync(stack, "Sprint", spaceId: space);

        using var provider = CreateProvider(stack, editor);

        await UpdateBoardAsync(provider, board, "Sprint 42");
        await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(provider, new GetResourcePermissionsQuery(BoardKind, board)))
            .Should().ThrowAsync<AppForbidden>();
        (await OrgTreeTestHost.QueryAsync(provider, new GetBoardAccessQuery(board))).MyLevel.Should().Be("editor");
    }

    [Fact]
    public async Task WorkspaceOwner_EditsAndManagesPermissions()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var board = await _host.SeedBoardAsync(stack, "Company", BoardVisibility.Private);

        using var provider = CreateProvider(stack, stack.OwnerId);

        await UpdateBoardAsync(provider, board, "Company 2026");
        await OrgTreeTestHost.QueryAsync(provider, new GetResourcePermissionsQuery(BoardKind, board));
        (await OrgTreeTestHost.QueryAsync(provider, new GetBoardAccessQuery(board))).MyLevel.Should().Be("manager");
    }

    [Fact]
    public async Task ArchivedBoard_IsNotFound_ForTheAccessRead_AsForTheFullRead()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var board = await _host.SeedBoardAsync(stack, "Old", transition: b => b.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime));

        using var provider = CreateProvider(stack, stack.OwnerId);

        var full = await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetFullBoardQuery(board)))
            .Should().ThrowAsync<AppNotFound>();
        var access = await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardAccessQuery(board)))
            .Should().ThrowAsync<AppNotFound>();
        access.Which.GetType().Should().Be(full.Which.GetType());
    }

    private async Task UpdateBoardAsync(ServiceProvider provider, Guid boardId, string title)
    {
        var version = await _host.ReadAsync(c => c.Boards.Where(b => b.Id == boardId).Select(b => b.Version).SingleAsync());
        await OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardCommand(boardId, title, null, null, null, version));
    }

    private ServiceProvider CreateProvider(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IBoardAccessSubjectFacts, BoardAccessSubjectFactsAdapter>();
            services.AddScoped<IResourceAccessEntriesReader, PostgresResourceAccessEntriesReader>();
            services.AddScoped<IResourceAccessSummary, ResourceAccessSummaryService>();
        });
}
