using Npgsql;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeletePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ResolveComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;
using Notrelix.Application.Features.Collaboration.Comments.Services;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;
using Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageBacklinks;
using Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;
using Notrelix.Application.Features.Documents.Pages.Commands.DeletePage;
using Notrelix.Application.Features.Documents.Pages.Commands.RestorePage;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;
using Notrelix.Application.Features.Documents.Pages.Queries.GetTrashedPages;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.DisableShareLink;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;
using Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Relations.DTOs;
using Notrelix.Application.Features.WorkManagement.Relations.Queries.ListBoardRelations;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Integration.Tests.Governance;

/// <summary>
/// TAC-WG-FLOW-04/05/06 — canonical authorization pipeline over real PostgreSQL.
/// Proves grant/revoke/get resource-permission mutations and reads, page-scoped
/// authorization, and canonical pipeline routing (allow/deny) through the one
/// AccessPolicyEngine — no second evaluator.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class GovernanceResourcePermissionFlowTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public GovernanceResourcePermissionFlowTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 41D — GrantResourcePermission variants ──────────────────────────────

    [Fact]
    public async Task Grant_OnBoard_ByOwner_Allows_WhenSubjectOwnedByOwner()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", ownerId, "Editor"));

        result.Succeeded.Should().BeTrue();
        result.Data.Level.Should().Be("Editor");
        result.Data.ResourceKind.Should().Be("work-management.board");

        await AssertAuditRecordedAsync(workspaceId, ownerId, "GrantResourcePermission");
    }

    [Fact]
    public async Task WorkspaceMember_OnWorkspaceVisibleBoard_WithoutMembershipOrAcl_Allows()
    {
        var (accountId, _, _, boardId, memberId) = await SeedBoardStackAsync();

        // Board.Create defaults to Workspace visibility; the member has no
        // board_members row and no governance.resource_permissions row. The
        // canonical facts query must still report the board's Workspace
        // audience so the engine keeps the pre-existing member baseline.
        using var provider = CreateProvider(accountId, memberId);
        var result = await SendAsync<Result<List<BoardRelationDto>>>(provider,
            new ListBoardRelationsQuery(boardId));

        result.Succeeded.Should().BeTrue(
            "a workspace member on a workspace-visible board was allowed before the page facts change");
    }

    [Fact]
    public async Task Grant_OnBoard_ByNonOwnerWithNoResourceLevel_IsForbidden()
    {
        var (accountId, ownerId, _, boardId, memberId) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountId, memberId);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", ownerId, "Viewer"));

        await act.Should().ThrowAsync<AppForbidden>(
            "a workspace member without explicit resource-level authority cannot grant permissions");
    }

    [Fact]
    public async Task Grant_OnBoard_ToUserOutsideTheWorkspace_IsRejected_AndWritesNothing()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        var outsiderId = Guid.CreateVersion7();

        using var provider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", outsiderId, "Viewer"));

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(
            "a permission cannot be granted to a user who does not belong to the workspace");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.ResourcePermissions.AnyAsync(p => p.SubjectId == outsiderId)).Should().BeFalse();
    }

    // WG-DEBT-CERT-006: board-specific cross-account gate. A workspace owner of
    // account A addressing a board that belongs to account B is told it does not
    // exist and nothing is written to account B.
    [Fact]
    public async Task Grant_OnBoardOfAnotherAccount_IsNotFound_AndWritesNothing()
    {
        var (accountA, ownerA, workspaceA, _, _) = await SeedBoardStackAsync();
        var (accountB, ownerB, workspaceB, boardB, _) = await SeedBoardStackAsync();
        accountA.Should().NotBe(accountB);
        workspaceA.Should().NotBe(workspaceB);

        using var provider = CreateProvider(accountA, ownerA);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("work-management.board", boardB, "User", ownerB, "Editor"));

        await act.Should().ThrowAsync<AppNotFound>(
            "a board of another account must be hidden, not reported as forbidden");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.ResourcePermissions.IgnoreQueryFilters()
            .AnyAsync(p => p.ResourceId == boardB && p.SubjectId == ownerB)).Should().BeFalse(
            "the cross-account request must not create an entry on the foreign board");
    }

    [Fact]
    public async Task Grant_OnBoard_CannotDowngrade_ExistingHigherTargetLevel()
    {
        var (accountId, ownerId, workspaceId, boardId, memberId) = await SeedBoardStackAsync();

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var granted = await SendAsync<Result<ResourcePermissionDto>>(ownerProvider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", memberId, "Manager"));
        granted.Succeeded.Should().BeTrue();

        // Second actor with only Editor-level authority on the same board
        // attempts to replace the existing Manager-level row — denied by the
        // engine's grant ceiling (max(requested, existing target)).
        var editorId = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, editorId, WorkspaceRole.Member);
        await SeedResourcePermissionAsync(accountId, workspaceId, "work-management.board", boardId, editorId, PermissionLevel.Editor);

        using var editorProvider = CreateProvider(accountId, editorId);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(editorProvider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", memberId, "Viewer"));

        await act.Should().ThrowAsync<AppForbidden>(
            "grant ceiling = max(requested, existing target) must exceed the editor's authority");
    }

    [Fact]
    public async Task Grant_OnPage_ByOwner_Allows_AndUsesPageAction()
    {
        var (accountId, ownerId, workspaceId, pageId, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", ownerId, "Commenter"));

        result.Succeeded.Should().BeTrue("a guessed page permission action must not be required; ManagePagePermission authorizes the grant");
        result.Data.ResourceKind.Should().Be("documents.page");
        result.Data.Level.Should().Be("Commenter");
    }

    [Fact]
    public async Task Grant_OnPage_SameSubjectAndLevel_ReplacesWithoutDuplicate()
    {
        var (accountId, ownerId, _, pageId, memberId) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);

        var first = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Viewer"));
        first.Succeeded.Should().BeTrue();
        var second = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Viewer"));

        second.Succeeded.Should().BeTrue();
        second.Data.Id.Should().Be(first.Data.Id, "grant is a semantic upsert: same subject+level reuses the active row");
    }

    [Fact]
    public async Task Grant_OnPage_ExpiredDates_AreRejectedByValidation()
    {
        var (accountId, ownerId, _, pageId, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", ownerId, "Viewer", DateTime.UtcNow.AddDays(1)));

        await act.Should().ThrowAsync<AppValidation>(
            "expiration is not supported for resource permissions");
    }

    [Fact]
    public async Task Concurrent_Grants_ForSameActiveSubject_RaceOnUniqueIndex()
    {
        // Stale-read race: both transactions must observe the absent row
        // BEFORE either inserts, then the unique active-subject index must
        // reject the second insert at the database, not in application code.
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        var now = DateTimeOffset.UtcNow;
        var firstAbsent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAbsent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBarrier = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var kind = ResourceKind.Create("documents.page");

        async Task<Exception?> InsertWithinTransaction(
            Guid txUserId, TaskCompletionSource<bool> absenceReported)
        {
            await using var context = _db.CreateOwnerContext(SystemTenant());
            await using var tx = await context.Database.BeginTransactionAsync();
            // Both transactions read the absence of an active row first.
            var absent = !await context.ResourcePermissions
                .AnyAsync(p => p.WorkspaceId == workspaceId
                            && p.ResourceKind == kind
                            && p.ResourceId == pageId
                            && p.SubjectType == PermissionSubjectType.User
                            && p.SubjectId == memberId
                            && p.DeletedAt == null);
            if (!absent)
            {
                return new InvalidOperationException("row already visible before insert");
            }

            // Synchronize on the observed absence itself: no transaction may
            // insert until both have read the row as absent.
            absenceReported.TrySetResult(true);
            await releaseBarrier.Task.WaitAsync(TimeSpan.FromSeconds(10));

            context.ResourcePermissions.Add(ResourcePermission.Grant(
                accountId, workspaceId, kind, pageId,
                PermissionSubjectType.User, memberId, PermissionLevel.Viewer, txUserId, now));
            try
            {
                await context.SaveChangesAsync();
                await tx.CommitAsync();
                return null;
            }
            catch (Exception failure)
            {
                return failure;
            }
        }

        var firstTask = Task.Run(() => InsertWithinTransaction(ownerId, firstAbsent));
        var secondTask = Task.Run(() => InsertWithinTransaction(Guid.NewGuid(), secondAbsent));

        await Task.WhenAll(firstAbsent.Task, secondAbsent.Task)
            .WaitAsync(TimeSpan.FromSeconds(10));
        releaseBarrier.TrySetResult(true);

        var outcomes = await Task.WhenAll(firstTask, secondTask);
        var failures = outcomes.Where(o => o is not null).ToArray();

        failures.Should().HaveCount(1, "exactly one concurrent insert may commit the active row");
        failures.Single().Should().BeOfType<DbUpdateException>();
        var postgres = ((DbUpdateException)failures.Single()!).InnerException as PostgresException;
        postgres.Should().NotBeNull("the race must be rejected by PostgreSQL");
        postgres!.SqlState.Should().Be("23505");
        postgres.ConstraintName.Should().Be("uq_resource_permissions_active_subject");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.ResourcePermissions.CountAsync(p => p.WorkspaceId == workspaceId
            && p.ResourceId == pageId && p.SubjectId == memberId && p.DeletedAt == null))
            .Should().Be(1, "exactly one active permission row survives the race");
    }

    // ── 41E — GetResourcePermissions ─────────────────────────────────────────

    [Fact]
    public async Task Get_OnPage_ReturnsGovernanceStateOnly_InWorkspaceScope()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, memberId, PermissionLevel.Commenter);

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<List<ResourcePermissionDto>>>(provider,
            new GetResourcePermissionsQuery("documents.page", pageId));

        result.Succeeded.Should().BeTrue();
        result.Data.Should().ContainSingle(p => p.SubjectId == memberId && p.Level == "Commenter");
        result.Data.Should().NotContain(p => p.ResourceKind != "documents.page",
            "the query is Governance-owned state scoped to the exact resource");
    }

    [Fact]
    public async Task Get_OnPage_DoesNotReturnRowsFromAnotherResource()
    {
        var (accountId, ownerId, workspaceId, pageId, _) = await SeedPageStackAsync();
        var (_, _, _, otherPageId, _) = await SeedPageStackAsync();
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", otherPageId, ownerId, PermissionLevel.Viewer);

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<List<ResourcePermissionDto>>>(provider,
            new GetResourcePermissionsQuery("documents.page", pageId));

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty("permissions of another page must not leak into this resource's list");
    }

    [Fact]
    public async Task Get_OrdinaryMember_OnWorkspacePage_IsForbidden()
    {
        var (accountId, _, _, pageId, memberId) = await SeedPageStackAsync();

        // Workspace visibility grants page access, never ACL management; the
        // member has no explicit resource permission.
        using var provider = CreateProvider(accountId, memberId);
        var act = () => SendAsync<Result<List<ResourcePermissionDto>>>(provider,
            new GetResourcePermissionsQuery("documents.page", pageId));

        await act.Should().ThrowAsync<AppForbidden>(
            "ordinary workspace members must not read the ACL of a page");
    }

    [Fact]
    public async Task Get_ManagerLevelMember_OnWorkspacePage_IsAllowed()
    {
        var (accountId, _, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, memberId, PermissionLevel.Manager);

        using var provider = CreateProvider(accountId, memberId);
        var result = await SendAsync<Result<List<ResourcePermissionDto>>>(provider,
            new GetResourcePermissionsQuery("documents.page", pageId));

        result.Succeeded.Should().BeTrue(
            "an active Manager-level resource permission authorizes ACL management");
    }

    // ── 41D2 — RevokeResourcePermission authorization (WG-FLOW-04) ──────────

    [Fact]
    public async Task Revoke_OnPage_ByOwner_RemovesActiveRowAndAudits()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var granted = await SendAsync<Result<ResourcePermissionDto>>(ownerProvider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Viewer"));
        granted.Succeeded.Should().BeTrue();

        var result = await SendAsync<Result>(ownerProvider,
            new RevokeResourcePermissionCommand("documents.page", pageId, granted.Data.Id));

        result.Succeeded.Should().BeTrue();
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var row = await verify.ResourcePermissions.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == granted.Data.Id);
        row.DeletedAt.Should().NotBeNull("revoke soft-deletes the active permission row");
        await AssertAuditRecordedAsync(workspaceId, ownerId, "RevokeResourcePermission");
    }

    [Fact]
    public async Task Revoke_PageManagerRank_RevokeEqualOrLowerTargetRank_Allows()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        var managerId = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, managerId, WorkspaceRole.Member);
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, managerId, PermissionLevel.Manager);

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var granted = await SendAsync<Result<ResourcePermissionDto>>(ownerProvider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Viewer"));
        granted.Succeeded.Should().BeTrue();

        using var managerProvider = CreateProvider(accountId, managerId);
        var result = await SendAsync<Result>(managerProvider,
            new RevokeResourcePermissionCommand("documents.page", pageId, granted.Data.Id));

        result.Succeeded.Should().BeTrue(
            "an active Manager-rank authority may revoke an equal-or-lower target rank through the one canonical ceiling");
    }

    [Fact]
    public async Task Revoke_AuthorityBelowTargetRank_IsForbidden_AndRowStaysActive()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        var editorId = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, editorId, WorkspaceRole.Member);
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, editorId, PermissionLevel.Editor);

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var granted = await SendAsync<Result<ResourcePermissionDto>>(ownerProvider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Manager"));
        granted.Succeeded.Should().BeTrue();

        using var editorProvider = CreateProvider(accountId, editorId);
        var act = () => SendAsync<Result>(editorProvider,
            new RevokeResourcePermissionCommand("documents.page", pageId, granted.Data.Id));

        await act.Should().ThrowAsync<AppForbidden>(
            "revoke requires authority >= the target's existing rank");
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.ResourcePermissions.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == granted.Data.Id)).DeletedAt
            .Should().BeNull("a denied revoke must leave the permission row untouched");
    }

    [Fact]
    public async Task Revoke_SubjectWithoutManagementAuthority_IsForbidden_EvenOnOwnRankRow()
    {
        // Being the subject of a permission row is not ACL-management
        // authority: the ManagePagePermission ladder gates the revoke path
        // exactly like the read/grant paths.
        var (accountId, ownerId, _, pageId, memberId) = await SeedPageStackAsync();

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var granted = await SendAsync<Result<ResourcePermissionDto>>(ownerProvider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Viewer"));
        granted.Succeeded.Should().BeTrue();

        using var memberProvider = CreateProvider(accountId, memberId);
        var act = () => SendAsync<Result>(memberProvider,
            new RevokeResourcePermissionCommand("documents.page", pageId, granted.Data.Id));

        await act.Should().ThrowAsync<AppForbidden>(
            "a Viewer subject holds no page-management authority");
    }

    [Fact]
    public async Task Revoke_UnknownPermission_IsNotFound()
    {
        var (accountId, ownerId, _, pageId, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result>(provider,
            new RevokeResourcePermissionCommand("documents.page", pageId, Guid.NewGuid()));

        await act.Should().ThrowAsync<AppNotFound>();
    }

    [Fact]
    public async Task Revoke_ForeignAccountTarget_IsNotFound_AndForeignRowStaysActive()
    {
        // Cross-account existence must stay hidden on the mutation path too:
        // the foreign permission id neither leaks through Forbidden nor is
        // revoked.
        var (accountA, ownerA, _, _, _) = await SeedPageStackAsync();
        var (accountB, ownerB, _, pageB, memberB) = await SeedPageStackAsync();

        using var providerB = CreateProvider(accountB, ownerB);
        var granted = await SendAsync<Result<ResourcePermissionDto>>(providerB,
            new GrantResourcePermissionCommand("documents.page", pageB, "User", memberB, "Viewer"));
        granted.Succeeded.Should().BeTrue();

        using var providerA = CreateProvider(accountA, ownerA);
        var act = () => SendAsync<Result>(providerA,
            new RevokeResourcePermissionCommand("documents.page", pageB, granted.Data.Id));

        await act.Should().ThrowAsync<AppNotFound>(
            "a foreign-account permission must be hidden from the revoke mutation");
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.ResourcePermissions.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == granted.Data.Id)).DeletedAt
            .Should().BeNull("the foreign row must remain active after the hidden attempt");
    }

    // ── 41F — Canonical authorization pipeline routing ───────────────────────

    [Fact]
    public async Task MoveBoardItem_Owner_Allows()
    {
        var (accountId, ownerId, _, boardId, _) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var itemId = await ResolveBoardItemIdAsync(boardId);
        var groupId = await ResolveOtherGroupIdAsync(boardId);

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
                .Set($"gov-move-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);

            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new MoveBoardItemCommand(itemId, groupId, 1));
        }
    }

    [Fact]
    public async Task MoveBoardItem_BoardObserver_IsDenied_ByTheBoardDecision()
    {
        // Board children are decided on their Board (GOV-033): an ordinary
        // member of a Workspace-visible Board is now an Editor and may move
        // items. The denied principal is therefore made a board Observer
        // (Viewer) on a Private Board; the protected property is unchanged —
        // the canonical decision denies and nothing else may bypass it.
        var (accountId, _, _, boardId, memberId) = await SeedBoardStackAsync();

        await using (var restrict = _db.CreateOwnerContext(SystemTenant()))
        {
            var restricted = await restrict.Boards.SingleAsync(b => b.Id == boardId);
            restricted.ChangeVisibility(BoardVisibility.Private, restricted.CreatedBy!.Value, DateTimeOffset.UtcNow);
            restrict.BoardMembers.Add(BoardMember.Create(boardId, memberId, BoardRole.Observer, DateTimeOffset.UtcNow));
            await restrict.SaveChangesAsync();
        }

        using var provider = CreateProvider(accountId, memberId);
        var itemId = await ResolveBoardItemIdAsync(boardId);
        var groupId = await ResolveOtherGroupIdAsync(boardId);

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
                .Set($"gov-move-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);

            var act = () => scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new MoveBoardItemCommand(itemId, groupId, 1));

            await act.Should().ThrowAsync<AppForbidden>(
                "a Viewer on the Board cannot move its items; no second evaluator may bypass the canonical decision");
        }
    }

    [Fact]
    public async Task CreatePage_NonMember_IsForbidden()
    {
        var (accountId, _, workspaceId, _, _) = await SeedPageStackAsync();
        var outsider = Guid.NewGuid();

        using var provider = CreateProvider(accountId, outsider);
        var act = () => SendAsync<Result<Guid>>(provider,
            new CreatePageCommand(workspaceId, "Outsider Page", null));

        await act.Should().ThrowAsync<AppForbidden>(
            "a non-member has no workspace role, so the engine denies CreatePage");
    }

    [Fact]
    public async Task CreatePage_Owner_Allows()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<Guid>>(provider,
            new CreatePageCommand(workspaceId, "Owner Page", null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePage_WorkspaceMember_Allows()
    {
        // M2G intended policy: page creation is a workspace usage right for
        // authenticated members — the DC-FLOW-01 actor is the workspace member.
        var (accountId, _, workspaceId, _, memberId) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, memberId);
        var result = await SendAsync<Result<Guid>>(provider,
            new CreatePageCommand(workspaceId, "Member Page", null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePage_GivesTheCreatorAManagerEntry_SoTheyCanDeleteItForGoodFromTheTrash()
    {
        var (accountId, _, workspaceId, _, memberId) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, memberId);
        var created = await SendAsync<Result<Guid>>(provider,
            new CreatePageCommand(workspaceId, "Member Page", null));
        var pageId = created.Data;

        await using (var probe = _db.CreateOwnerContext(SystemTenant()))
        {
            var entry = await probe.ResourcePermissions.IgnoreQueryFilters()
                .SingleAsync(p => p.ResourceId == pageId);
            entry.SubjectId.Should().Be(memberId);
            entry.Level.Should().Be(PermissionLevel.Manager, "the creator manages what they made");
        }

        // Manager of the page: may trash it and then delete it for good.
        (await SendAsync<Result>(provider, new ArchivePageCommand(pageId))).Succeeded.Should().BeTrue();
        (await SendAsync<Result>(provider, new DeletePageCommand(pageId))).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Backlinks_ListThePagesThatReferenceOrMentionAPage_AndNothingElse()
    {
        var (accountId, _, workspaceId, _, memberId) = await SeedPageStackAsync();
        using var provider = CreateProvider(accountId, memberId);
        var target = (await SendAsync<Result<Guid>>(provider, new CreatePageCommand(workspaceId, "Target", null))).Data;
        var referencing = (await SendAsync<Result<Guid>>(provider, new CreatePageCommand(workspaceId, "Referencing", null))).Data;
        var mentioning = (await SendAsync<Result<Guid>>(provider, new CreatePageCommand(workspaceId, "Mentioning", null))).Data;
        var unrelated = (await SendAsync<Result<Guid>>(provider, new CreatePageCommand(workspaceId, "Unrelated", null))).Data;

        await SendAsync<Result<BlockDto>>(provider, new CreateBlockCommand(
            referencing, null, "page_reference", null, Json($"{{\"pageId\":\"{target}\"}}"), null, null));
        await SendAsync<Result<BlockDto>>(provider, new CreateBlockCommand(
            mentioning, null, "paragraph",
            Json($"{{\"v\":1,\"runs\":[{{\"text\":\"see\",\"marks\":[{{\"type\":\"pageRef\",\"pageId\":\"{target}\"}}]}}]}}"),
            null, null, null));
        await SendAsync<Result<BlockDto>>(provider, new CreateBlockCommand(
            unrelated, null, "page_reference", null, Json($"{{\"pageId\":\"{Guid.NewGuid()}\"}}"), null, null));

        var backlinks = await SendAsync<Result<PageBacklinksDto>>(provider, new GetPageBacklinksQuery(target));

        backlinks.Data!.Docs.Select(d => d.Id).Should().BeEquivalentTo([referencing, mentioning]);
    }

    [Fact]
    public async Task UpdateBlock_ChangesTheTypeInPlace_KeepingTheBlockAndItsText()
    {
        var (accountId, _, workspaceId, _, memberId) = await SeedPageStackAsync();
        using var provider = CreateProvider(accountId, memberId);
        var pageId = (await SendAsync<Result<Guid>>(provider, new CreatePageCommand(workspaceId, "Doc", null))).Data;
        var created = (await SendAsync<Result<BlockDto>>(provider, new CreateBlockCommand(
            pageId, null, "paragraph", Json("{\"v\":1,\"runs\":[{\"text\":\"Hello\",\"marks\":[]}]}"), null, null, null))).Data!;
        var blockId = created.Id;

        (await SendAsync<Result<BlockDto>>(provider, new UpdateBlockCommand(pageId, blockId, "heading_2", null, null, created.Version))).Succeeded.Should().BeTrue();

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var block = await probe.Blocks.IgnoreQueryFilters().SingleAsync(b => b.Id == blockId);
        block.Type.Should().Be(BlockType.Heading2);
        block.Content.Data.Value.Should().Be("{\"v\":1,\"runs\":[{\"text\":\"Hello\",\"marks\":[]}]}");
    }

    private static System.Text.Json.JsonElement Json(string json) => System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public async Task CreatePage_Guest_IsForbidden()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var guest = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, guest, WorkspaceRole.Guest);
        await SyncAccessGrantsAsync(accountId, workspaceId, (guest, WorkspaceRole.Guest));

        using var provider = CreateProvider(accountId, guest);
        var act = () => SendAsync<Result<Guid>>(provider,
            new CreatePageCommand(workspaceId, "Guest Page", null));

        await act.Should().ThrowAsync<AppForbidden>("guests are excluded from workspace page creation");
    }

    [Fact]
    public async Task CreateComment_OnPage_Owner_Allows()
    {
        var (accountId, ownerId, _, pageId, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForPage(pageId, "Hello page", null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreateComment_OnPage_WorkspaceMember_Allows()
    {
        var (accountId, _, _, pageId, memberId) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, memberId);
        var result = await SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForPage(pageId, "Hello page", null));

        result.Succeeded.Should().BeTrue(
            "commenting on a workspace-visible page is a usage right, not ACL management");
    }

    [Fact]
    public async Task CreateComment_OnPage_Outsider_IsForbidden()
    {
        var (accountId, _, _, pageId, _) = await SeedPageStackAsync();
        var outsider = Guid.NewGuid();

        using var provider = CreateProvider(accountId, outsider);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForPage(pageId, "Hello page", null));

        await act.Should().ThrowAsync<AppForbidden>("page-scoped actions are denied for non-members");
    }

    [Fact]
    public async Task CreateComment_OnBoardItem_Owner_Allows()
    {
        var (accountId, ownerId, _, boardId, _) = await SeedBoardStackAsync();
        var itemId = await ResolveBoardItemIdAsync(boardId);

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(itemId, "Hello item", null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateComment_OnBoardItem_IsAuthorOnly_NotOwnerOnly()
    {
        var (accountId, ownerId, _, boardId, memberId) = await SeedBoardStackAsync();
        var itemId = await ResolveBoardItemIdAsync(boardId);

        using var memberProvider = CreateProvider(accountId, memberId);
        var commentId = (await SendAsync<Result<Guid>>(memberProvider,
            CreateCommentCommand.ForBoardItem(itemId, "Mine", null))).Data;

        (await SendAsync<Result>(memberProvider, new UpdateCommentCommand(commentId, "Edited"))).Succeeded.Should().BeTrue(
            "an author who can still view the item edits their own comment");

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result>(ownerProvider, new UpdateCommentCommand(commentId, "Rewritten"));
        await act.Should().ThrowAsync<AppForbidden>("the workspace Owner may not rewrite another person's comment");
    }

    [Fact]
    public async Task UpdateComment_OnPage_IsAuthorOnly_NotOwnerOnly()
    {
        var (accountId, ownerId, _, pageId, memberId) = await SeedPageStackAsync();

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var commentId = (await SendAsync<Result<Guid>>(ownerProvider,
            CreateCommentCommand.ForPage(pageId, "Owner's", null))).Data;

        using var memberProvider = CreateProvider(accountId, memberId);
        var act = () => SendAsync<Result>(memberProvider, new UpdateCommentCommand(commentId, "Rewritten"));
        await act.Should().ThrowAsync<AppForbidden>("a member who can view the page may not rewrite the Owner's comment");

        (await SendAsync<Result>(ownerProvider, new UpdateCommentCommand(commentId, "Edited"))).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateComment_WhenTheAuthorLosesViewAccess_IsTheSameNotFoundAsAMissingComment()
    {
        var (accountId, _, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        using var memberProvider = CreateProvider(accountId, memberId);
        var commentId = (await SendAsync<Result<Guid>>(memberProvider, CreateCommentCommand.ForPage(pageId, "Mine", null))).Data;
        await SeedPermissionRuleAsync(accountId, workspaceId, memberId, PermissionAction.ViewPage, "documents.page", pageId, PermissionEffect.Deny);

        var hidden = () => SendAsync<Result>(memberProvider, new UpdateCommentCommand(commentId, "Edited"));
        var missingId = Guid.NewGuid();
        var missing = () => SendAsync<Result>(memberProvider, new UpdateCommentCommand(missingId, "Edited"));

        var hiddenError = (await hidden.Should().ThrowAsync<AppNotFound>()).Which;
        var missingError = (await missing.Should().ThrowAsync<AppNotFound>()).Which;
        hiddenError.Message.Should().Be(missingError.Message.Replace(missingId.ToString(), commentId.ToString()));
    }

    [Fact]
    public async Task ResolveComment_ByAMemberEditor_OfAnotherPersonsThread_Succeeds()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, memberId, PermissionLevel.Editor);
        using var ownerProvider = CreateProvider(accountId, ownerId);
        var commentId = (await SendAsync<Result<Guid>>(ownerProvider, CreateCommentCommand.ForPage(pageId, "Owner's thread", null))).Data;

        using var memberProvider = CreateProvider(accountId, memberId);
        (await SendAsync<Result>(memberProvider, new ResolveCommentCommand(commentId))).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveComment_ByANonEditorWhoIsNotTheStarter_IsForbiddenWithAStableCode()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, memberId, PermissionLevel.Viewer);
        await SeedPermissionRuleAsync(accountId, workspaceId, memberId, PermissionAction.UpdatePage, "documents.page", pageId, PermissionEffect.Deny);
        using var ownerProvider = CreateProvider(accountId, ownerId);
        var commentId = (await SendAsync<Result<Guid>>(ownerProvider, CreateCommentCommand.ForPage(pageId, "Owner's thread", null))).Data;

        using var memberProvider = CreateProvider(accountId, memberId);
        var act = () => SendAsync<Result>(memberProvider, new ResolveCommentCommand(commentId));

        (await act.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(CommentAccessRules.ThreadStarterOrEditorCode);
    }

    [Fact]
    public async Task DeletePageAttachment_ByAnEditor_Succeeds_AndByAViewerWhoUploadedIt_IsEditorOnly()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        var byOwner = await SeedLinkAttachmentAsync(accountId, workspaceId, pageId, ownerId);
        var byMember = await SeedLinkAttachmentAsync(accountId, workspaceId, pageId, memberId);
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, memberId, PermissionLevel.Viewer);
        await SeedPermissionRuleAsync(accountId, workspaceId, memberId, PermissionAction.UpdatePage, "documents.page", pageId, PermissionEffect.Deny);

        using var memberProvider = CreateProvider(accountId, memberId);
        var viewerDelete = () => SendAsync<Result>(memberProvider, new DeletePageAttachmentCommand(pageId, byMember));
        (await viewerDelete.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(AttachmentAccessRules.EditorOnlyCode);

        using var ownerProvider = CreateProvider(accountId, ownerId);
        (await SendAsync<Result>(ownerProvider, new DeletePageAttachmentCommand(pageId, byOwner))).Succeeded.Should().BeTrue();
        (await SendAsync<Result>(ownerProvider, new DeletePageAttachmentCommand(pageId, byMember))).Succeeded.Should().BeTrue();
    }

    private async Task<Guid> SeedLinkAttachmentAsync(Guid accountId, Guid workspaceId, Guid pageId, Guid uploaderId)
    {
        var row = Attachment.Create(
            accountId, workspaceId, ResourceRef.Create(ResourceKind.Create("documents.page"), pageId, workspaceId),
            AttachmentType.Link, FileMetadata.Create("link", 0, "text/uri-list", url: "https://example.com"),
            uploaderId, DateTimeOffset.UtcNow);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Attachments.Add(row);
        await seed.SaveChangesAsync();
        return row.Id;
    }

    [Fact]
    public async Task CreateComment_OnBoardItem_WorkspaceMember_Allows()
    {
        // M2G intended policy: commenting on a board item is a usage right
        // for workspace members, decided by the target resource kind through
        // the one canonical pipeline — no second evaluator.
        var (accountId, _, _, boardId, memberId) = await SeedBoardStackAsync();
        var itemId = await ResolveBoardItemIdAsync(boardId);

        using var provider = CreateProvider(accountId, memberId);
        var result = await SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(itemId, "Hello item", null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreateComment_OnBoardItem_Guest_WithoutExplicitAccess_IsDenied()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        var itemId = await ResolveBoardItemIdAsync(boardId);
        var guest = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, guest, WorkspaceRole.Guest);
        await SyncAccessGrantsAsync(accountId, workspaceId, (guest, WorkspaceRole.Guest));

        using var provider = CreateProvider(accountId, guest);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(itemId, "Hello item", null));

        await act.Should().ThrowAsync<AppNotFound>(
            "board items carry no per-item audience fact, so guests without explicit access are hidden");
    }

    [Fact]
    public async Task CreateComment_OnBoardItem_Guest_WithResourcePermission_IsDenied_GuestsAreViewOnly()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        var itemId = await ResolveBoardItemIdAsync(boardId);
        var guest = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, guest, WorkspaceRole.Guest);
        await SyncAccessGrantsAsync(accountId, workspaceId, (guest, WorkspaceRole.Guest));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "work-management.board-item", itemId, guest, PermissionLevel.Commenter);

        using var provider = CreateProvider(accountId, guest);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(itemId, "Hello item", null));

        await act.Should().ThrowAsync<AppForbidden>(
            "a guest is capped at Viewer whatever the grant, so a Commenter entry does not allow commenting");
    }

    [Fact]
    public async Task CreateComment_OnBoardItem_Outsider_IsDenied()
    {
        var (accountId, _, _, boardId, _) = await SeedBoardStackAsync();
        var itemId = await ResolveBoardItemIdAsync(boardId);
        var outsider = Guid.NewGuid();

        using var provider = CreateProvider(accountId, outsider);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(itemId, "Hello item", null));

        await act.Should().ThrowAsync<AppForbidden>(
            "board-item comments are not granted to non-members; the canonical pipeline denies them");
    }

    [Fact]
    public async Task ArchivePage_Owner_Allows_AndCommitsLifecycle()
    {
        var (accountId, ownerId, _, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result>(provider, new ArchivePageCommand(pageId));

        result.Succeeded.Should().BeTrue();
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId)).Status
            .Should().Be(PageStatus.Archived);
    }

    [Fact]
    public async Task DeletePage_WorkspaceMember_OnWorkspaceVisiblePage_IsForbidden()
    {
        // M2G extension: page visibility is not lifecycle mutation authority.
        // A workspace-visible page only proves the member can see it —
        // ArchivePage requires an active page ResourcePermission of at least
        // Manager (or the workspace Owner fast-path).
        var (accountId, _, _, _, memberId) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();

        using var provider = CreateProvider(accountId, memberId);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>(
            "visibility grants reading, not page lifecycle mutation authority");
    }

    [Fact]
    public async Task DeletePage_PageManagerLevelPermission_Allows()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        await TrashPageAsync(pageId);
        var manager = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, manager, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (manager, WorkspaceRole.Member));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "documents.page", pageId, manager, PermissionLevel.Manager);

        using var provider = CreateProvider(accountId, manager);
        var result = await SendAsync<Result>(provider, new DeletePageCommand(pageId));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePage_PageEditorLevelPermission_IsForbidden()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var editor = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, editor, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (editor, WorkspaceRole.Member));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "documents.page", pageId, editor, PermissionLevel.Editor);

        using var provider = CreateProvider(accountId, editor);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>(
            "Editor rank is below the Manager threshold for page lifecycle authority");
    }

    [Fact]
    public async Task DeletePage_PageViewerLevelPermission_IsForbidden()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var viewer = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, viewer, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (viewer, WorkspaceRole.Member));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "documents.page", pageId, viewer, PermissionLevel.Viewer);

        using var provider = CreateProvider(accountId, viewer);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>("Viewer rank cannot archive");
    }

    [Fact]
    public async Task DeletePage_PageCommenterLevelPermission_IsForbidden()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var commenter = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, commenter, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (commenter, WorkspaceRole.Member));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "documents.page", pageId, commenter, PermissionLevel.Commenter);

        using var provider = CreateProvider(accountId, commenter);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>("Commenter rank cannot archive");
    }

    [Fact]
    public async Task DeletePage_WorkspaceAdmin_WithoutPageManagerAuthority_IsForbidden()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var admin = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, admin, WorkspaceRole.Admin);
        await SyncAccessGrantsAsync(accountId, workspaceId, (admin, WorkspaceRole.Admin));

        using var provider = CreateProvider(accountId, admin);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>(
            "workspace Admin is not a default page lifecycle manager");
    }

    [Fact]
    public async Task DeletePage_ExplicitAllowRule_Allows()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        await TrashPageAsync(pageId);
        var member = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, member, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (member, WorkspaceRole.Member));
        await SeedPermissionRuleAsync(
            accountId, workspaceId, member, PermissionAction.DeletePage,
            "documents.page", pageId, PermissionEffect.Allow);

        using var provider = CreateProvider(accountId, member);
        var result = await SendAsync<Result>(provider, new DeletePageCommand(pageId));

        result.Succeeded.Should().BeTrue(
            "an applicable explicit Allow rule grants the lifecycle authority");
    }

    // WG-DEBT-CERT-005: rule time windows and same-priority tie-break on real Postgres.
    [Fact]
    public async Task DeletePage_ExpiredAllowRule_IsIgnored_AndRequestIsForbidden()
    {
        var (accountId, _, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var member = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, member, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (member, WorkspaceRole.Member));
        await SeedPermissionRuleAsync(
            accountId, workspaceId, member, PermissionAction.DeletePage,
            "documents.page", pageId, PermissionEffect.Allow,
            startsAt: DateTimeOffset.UtcNow.AddDays(-10), expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        using var provider = CreateProvider(accountId, member);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>("an expired Allow rule must not grant authority");
    }

    [Fact]
    public async Task DeletePage_NotYetActiveAllowRule_IsIgnored_AndRequestIsForbidden()
    {
        var (accountId, _, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var member = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, member, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (member, WorkspaceRole.Member));
        await SeedPermissionRuleAsync(
            accountId, workspaceId, member, PermissionAction.DeletePage,
            "documents.page", pageId, PermissionEffect.Allow,
            startsAt: DateTimeOffset.UtcNow.AddDays(1));

        using var provider = CreateProvider(accountId, member);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>("a future-window Allow rule must not grant authority yet");
    }

    [Fact]
    public async Task DeletePage_ExpiredDenyRule_IsIgnored_ManagerStillAllowed()
    {
        var (accountId, _, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        await TrashPageAsync(pageId);
        var manager = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, manager, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (manager, WorkspaceRole.Member));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "documents.page", pageId, manager, PermissionLevel.Manager);
        await SeedPermissionRuleAsync(
            accountId, workspaceId, manager, PermissionAction.DeletePage,
            "documents.page", pageId, PermissionEffect.Deny,
            startsAt: DateTimeOffset.UtcNow.AddDays(-10), expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        using var provider = CreateProvider(accountId, manager);
        var result = await SendAsync<Result>(provider, new DeletePageCommand(pageId));

        result.Succeeded.Should().BeTrue("an expired Deny rule must not block the Manager-rank authority");
    }

    [Fact]
    public async Task DeletePage_SamePriorityDenyAndAllowRules_DenyWins()
    {
        var (accountId, _, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var member = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, member, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (member, WorkspaceRole.Member));
        await SeedPermissionRuleAsync(
            accountId, workspaceId, member, PermissionAction.DeletePage,
            "documents.page", pageId, PermissionEffect.Allow, priority: 50);
        await SeedPermissionRuleAsync(
            accountId, workspaceId, member, PermissionAction.DeletePage,
            "documents.page", pageId, PermissionEffect.Deny, priority: 50);

        using var provider = CreateProvider(accountId, member);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>("at the same priority an active Deny beats an active Allow");
    }

    [Fact]
    public async Task DeletePage_ExplicitDenyRule_IsForbidden_EvenForManager()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var manager = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, manager, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (manager, WorkspaceRole.Member));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "documents.page", pageId, manager, PermissionLevel.Manager);
        await SeedPermissionRuleAsync(
            accountId, workspaceId, manager, PermissionAction.DeletePage,
            "documents.page", pageId, PermissionEffect.Deny);

        using var provider = CreateProvider(accountId, manager);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>(
            "an applicable explicit Deny rule overrides the Manager-rank authority");
    }

    [Fact]
    public async Task DeletePage_WrongTenant_IsNotFound_AndTargetStaysActive()
    {
        // Cross-tenant resource existence must not leak through Forbidden:
        // a page from workspace B addressed from a workspace-A context is
        // hidden from lifecycle mutations entirely.
        var (accountA, _, workspaceA, _, memberA) = await SeedPageStackAsync();
        var (_, _, workspaceB, pageB, _) = await SeedPageStackAsync();
        workspaceA.Should().NotBe(workspaceB);

        using var provider = CreateProvider(accountA, memberA);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageB));

        await act.Should().ThrowAsync<AppNotFound>(
            "a cross-tenant page must be hidden from lifecycle mutations");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageB)).Status
            .Should().Be(PageStatus.Active,
            "the wrong-tenant mutation must leave the foreign page untouched");
        (await verify.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .AnyAsync(m => m.MessageName == "page.archived"
                && (m.WorkspaceId == workspaceA || m.WorkspaceId == workspaceB))).Should().BeFalse(
            "no page.archived fact may be staged by the wrong-tenant request");
    }

    [Fact]
    public async Task DeletePage_Guest_IsDenied()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var guest = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, guest, WorkspaceRole.Guest);
        await SyncAccessGrantsAsync(accountId, workspaceId, (guest, WorkspaceRole.Guest));

        using var provider = CreateProvider(accountId, guest);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppNotFound>(
            "guests without explicit page access are hidden from the lifecycle mutation");
    }

    [Fact]
    public async Task DeletePage_Outsider_IsForbidden()
    {
        var (accountId, _, _, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var outsider = Guid.NewGuid();

        using var provider = CreateProvider(accountId, outsider);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<AppForbidden>(
            "the canonical pipeline denies non-members before any page mutation");
    }

    [Fact]
    public async Task ArchivePage_AlreadyArchived_IsANoOp_ButOtherPageMutationsStayClosed()
    {
        // A page in the trash is reachable only for the trash lifecycle
        // (restore, delete for good): archiving again changes nothing, while
        // any other page action still fails closed as NotFound. Outward event
        // evidence lives in PageArchivedOutboxEvidenceTests.
        var (accountId, ownerId, _, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();

        using var provider = CreateProvider(accountId, ownerId);
        (await SendAsync<Result>(provider, new ArchivePageCommand(pageId))).Succeeded.Should().BeTrue();
        long version;
        await using (var before = _db.CreateOwnerContext(SystemTenant()))
            version = (await before.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId)).Version;

        (await SendAsync<Result>(provider, new ArchivePageCommand(pageId))).Succeeded.Should().BeTrue();

        var act = () => SendAsync<Result>(provider, new UpdatePageCommand(pageId, "Renamed", version));
        await act.Should().ThrowAsync<AppNotFound>(
            "a trashed page accepts no edits");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var page = await verify.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId);
        page.Status.Should().Be(PageStatus.Archived);
        page.Version.Should().Be(version, "the repeated archive is a semantic no-op");
    }

    [Fact]
    public async Task TrashLifecycle_PageEditor_CanTrashAndRestore_ButNotDeleteForGood()
    {
        var (accountId, _, workspaceId, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();
        var editor = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, editor, WorkspaceRole.Member);
        await SyncAccessGrantsAsync(accountId, workspaceId, (editor, WorkspaceRole.Member));
        await SeedResourcePermissionAsync(
            accountId, workspaceId, "documents.page", pageId, editor, PermissionLevel.Editor);

        using var provider = CreateProvider(accountId, editor);
        (await SendAsync<Result>(provider, new ArchivePageCommand(pageId))).Succeeded.Should().BeTrue("an Editor moves a page to the trash");

        var listed = await SendAsync<Result<CursorPage<PageDto>>>(provider, new GetTrashedPagesQuery(workspaceId));
        listed.Data!.Items.Select(p => p.Id).Should().Contain(pageId, "the trash lists the page for someone who can see it");

        var delete = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));
        await delete.Should().ThrowAsync<AppForbidden>("deleting for good is Manager only");

        (await SendAsync<Result>(provider, new RestorePageCommand(pageId))).Succeeded.Should().BeTrue("an Editor restores a trashed page");
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId)).Status.Should().Be(PageStatus.Active);
    }

    [Fact]
    public async Task DeletePage_OfAPageNotInTheTrash_IsRefused_ForTheManager()
    {
        var (accountId, ownerId, _, _, _) = await SeedPageStackAsync();
        var pageId = await ResolvePageIdAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result>(provider, new DeletePageCommand(pageId));

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId)).DeletedAt.Should().BeNull();
    }

    private async Task TrashPageAsync(Guid pageId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var page = await context.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId);
        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();
    }

    private async Task<Guid> ResolvePageIdAsync()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var id = await context.Pages
            .IgnoreQueryFilters()
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        id.Should().NotBeEmpty("expected a seeded page");
        return id;
    }

    [Fact]
    public async Task CreateComment_OnMissingPage_IsNotFound_WithoutPersistence()
    {
        // TAC-DC-004 (page section): missing target → defined failure.
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForPage(Guid.CreateVersion7(), "Ghost page comment", null));

        await act.Should().ThrowAsync<AppNotFound>(
            "the page section requires its own missing-target proof");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Comments.IgnoreQueryFilters()
            .AnyAsync(c => c.WorkspaceId == workspaceId)).Should().BeFalse(
            "no comment may persist for a missing target");
        (await verify.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .AnyAsync(m => m.MessageName == "comment.created"
                && m.WorkspaceId == workspaceId)).Should().BeFalse(
            "no outward fact may be staged for a missing target");
    }

    [Fact]
    public async Task CreateComment_OnMissingBoardItem_IsNotFound_WithoutPersistence()
    {
        // TAC-DC-004 (board-item section): missing target → defined failure.
        var (accountId, ownerId, workspaceId, _, _) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), "Ghost item comment", null));

        await act.Should().ThrowAsync<AppNotFound>(
            "the board-item section requires its own missing-target proof");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Comments.IgnoreQueryFilters()
            .AnyAsync(c => c.WorkspaceId == workspaceId)).Should().BeFalse(
            "no comment may persist for a missing target");
        (await verify.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .AnyAsync(m => m.MessageName == "comment.created"
                && m.WorkspaceId == workspaceId)).Should().BeFalse(
            "no outward fact may be staged for a missing target");
    }

    [Fact]
    public async Task CreateComment_OnCrossScopePage_IsNotFound()
    {
        // TAC-DC-006 (page section): cross-scope target reference fails and
        // cross-tenant existence must not leak through Forbidden.
        // The actor is a legitimate member of tenant A (not a random Guid),
        // proving a real tenant-A member cannot address a tenant-B page.
        var (accountA, _, workspaceA, _, memberA) = await SeedPageStackAsync();
        var (_, _, _, pageB, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountA, memberA);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForPage(pageB, "Cross-tenant page comment", null));

        await act.Should().ThrowAsync<AppNotFound>();

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Comments.IgnoreQueryFilters()
            .AnyAsync(c => c.WorkspaceId == workspaceA)).Should().BeFalse(
            "no comment may persist for a cross-scope target");
        (await verify.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .AnyAsync(m => m.MessageName == "comment.created"
                && m.WorkspaceId == workspaceA)).Should().BeFalse(
            "no outward fact may be staged for a cross-scope target");
        (await verify.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageB)).Status
            .Should().Be(PageStatus.Active,
            "the foreign page must remain untouched");
    }

    [Fact]
    public async Task CreateComment_OnCrossScopeBoardItem_IsNotFound()
    {
        // TAC-DC-006 (board-item section): cross-scope target reference fails
        // and cross-tenant existence must not leak through Forbidden.
        var (accountA, _, workspaceA, _, memberA) = await SeedBoardStackAsync();
        var (_, _, _, boardB, _) = await SeedBoardStackAsync();
        var itemB = await ResolveBoardItemIdAsync(boardB);

        long itemVersionBefore;
        string itemNameBefore;
        await using (var snapshot = _db.CreateOwnerContext(SystemTenant()))
        {
            var item = await snapshot.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == itemB);
            itemVersionBefore = item.Version;
            itemNameBefore = item.Name;
        }

        using var provider = CreateProvider(accountA, memberA);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(itemB, "Cross-tenant item comment", null));

        await act.Should().ThrowAsync<AppNotFound>();

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Comments.IgnoreQueryFilters()
            .AnyAsync(c => c.WorkspaceId == workspaceA)).Should().BeFalse(
            "no comment may persist for a cross-scope target");
        (await verify.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .AnyAsync(m => m.MessageName == "comment.created"
                && m.WorkspaceId == workspaceA)).Should().BeFalse(
            "no outward fact may be staged for a cross-scope target");
        var itemAfter = await verify.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == itemB);
        itemAfter.Version.Should().Be(itemVersionBefore,
            "the foreign board item must remain untouched");
        itemAfter.Name.Should().Be(itemNameBefore);
    }

    /// <summary>
    /// WG-TST-WM-X-003 / WG-TEST-GAP-005 (P2B-CERT-011): a Board owned by
    /// another account must not be reachable through the production pipeline for
    /// a legitimate member of a different account — even when that member holds
    /// a valid grant in their own account. The result is NotFound, never
    /// Forbidden, so cross-tenant existence does not leak.
    /// </summary>
    [Fact]
    public async Task GetBoard_FromForeignAccount_IsNotFound_AndForeignBoardStaysUntouched()
    {
        var (accountA, _, _, _, memberA) = await SeedBoardStackAsync();
        var (_, _, _, boardB, _) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountA, memberA);
        var act = () => SendAsync<Result<BoardDto>>(provider, new GetBoardQuery(boardB));

        await act.Should().ThrowAsync<AppNotFound>(
            "a foreign-account board must be invisible to a granted account-A member");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var foreignBoard = await verify.Boards.IgnoreQueryFilters().SingleAsync(b => b.Id == boardB);
        foreignBoard.Title.Should().NotBeNull("the foreign board must remain intact");
    }

    /// <summary>
    /// M7 RESOURCE-SCOPE freeze — same account, two real workspaces. The
    /// resource-scoped request follows the authoritative resource location:
    /// a workspace-W2 member comments on a W2 board item with the request
    /// context selected to W1, the locator adopts W2, and authorization
    /// evaluates W2 facts. Not a cross-account hide (that path is separate).
    /// </summary>
    private async Task<(Guid AccountId, Guid W1, Guid W2, Guid W2ItemId, Guid W2MemberId)> SeedTwoWorkspacesOneAccountAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var w2MemberId = Guid.NewGuid();
        var account = Account.Create("Gov Two-WS Account", $"gov-{Guid.NewGuid():N}", AccountType.Team, ownerId, now);
        var w1 = Workspace.Create(account.Id, ownerId, "Gov W1", $"gov-w1-{Guid.NewGuid():N}", now);
        var w2 = Workspace.Create(account.Id, ownerId, "Gov W2", $"gov-w2-{Guid.NewGuid():N}", now);
        var board = Board.Create(account.Id, w2.Id, ownerId, "Gov W2 Board", null, now);
        var group = BoardGroup.Create(
            account.Id, w2.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var item = BoardItem.CreateRoot(
            account.Id, w2.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), ownerId, now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, now));
        seed.Workspaces.Add(w1);
        seed.Workspaces.Add(w2);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, w1.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, w1.Id, w2MemberId, WorkspaceRole.Member, ownerId, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, w2.Id, w2MemberId, WorkspaceRole.Member, ownerId, now));
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        await SyncAccessGrantsAsync(
            account.Id, w1.Id, (ownerId, WorkspaceRole.Owner), (w2MemberId, WorkspaceRole.Member));
        await SyncAccessGrantsAsync(
            account.Id, w2.Id, (w2MemberId, WorkspaceRole.Member));

        return (account.Id, w1.Id, w2.Id, item.Id, w2MemberId);
    }

    [Fact]
    public async Task CreateComment_SameAccount_CrossWorkspace_MemberOfTarget_Allows_AndPersistsUnderTargetWorkspace()
    {
        var (accountId, w1, w2, w2ItemId, w2MemberId) = await SeedTwoWorkspacesOneAccountAsync();

        // Request context selects W1; the target lives in W2. The locator is
        // the authority: execution adopts W2 and W2 facts decide the outcome.
        using var provider = CreateProvider(accountId, w2MemberId, w1);
        var result = await SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(w2ItemId, "Cross-workspace comment", null));

        result.Succeeded.Should().BeTrue(
            "a W2 member may comment on a W2 item even with W1 selected — the locator is the authority");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var stored = await verify.Comments.IgnoreQueryFilters()
            .SingleAsync(c => c.Target.ResourceId == w2ItemId);
        stored.WorkspaceId.Should().Be(w2,
            "the persisted comment belongs to the located target workspace");
        stored.CreatedBy.Should().Be(w2MemberId);
    }

    [Fact]
    public async Task CreateComment_SameAccount_CrossWorkspace_WithoutTargetAuthority_IsForbidden()
    {
        var (accountId, w1, _, w2ItemId, w2MemberId) = await SeedTwoWorkspacesOneAccountAsync();

        // An outsider to W2 (same account) is denied by the canonical W2
        // policy — same account never auto-allows.
        var outsider = Guid.NewGuid();
        using var provider = CreateProvider(accountId, outsider, w1);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForBoardItem(w2ItemId, "Outsider comment", null));

        await act.Should().ThrowAsync<AppForbidden>(
            "same-account membership does not imply W2 authority");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Comments.IgnoreQueryFilters()
            .AnyAsync(c => c.Target.ResourceId == w2ItemId)).Should().BeFalse(
            "no comment may persist for the denied cross-workspace reference");
    }

    // ── share links: authorization follows the resource in the route ──────────

    private const string BoardKind = "work-management.board";

    [Fact]
    public async Task DisableShareLink_OfAnotherBoard_IsNotFound_AndTheLinkStaysActive()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        var otherBoardId = await SeedBoardAsync(accountId, workspaceId, ownerId);
        var linkOfOtherBoard = await SeedShareLinkAsync(accountId, workspaceId, BoardKind, otherBoardId, ownerId);

        using var provider = CreateProvider(accountId, ownerId, workspaceId);
        var act = () => SendAsync<Result>(provider, new DisableShareLinkCommand(BoardKind, boardId, linkOfOtherBoard));

        await act.Should().ThrowAsync<AppNotFound>(
            "a link of another resource answers the same NotFound as a link that does not exist");
        (await ShareLinkStatusAsync(linkOfOtherBoard)).Should().Be(ShareLinkStatus.Active);
    }

    [Fact]
    public async Task DisableShareLink_NamingTheWrongResourceType_IsNotFound_AndTheLinkStaysActive()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        var linkOfBoard = await SeedShareLinkAsync(accountId, workspaceId, BoardKind, boardId, ownerId);

        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Pages.Add(Page.Create(accountId, workspaceId, "Share page", ownerId, DateTimeOffset.UtcNow));
            await seed.SaveChangesAsync();
        }
        Guid pageId;
        await using (var read = _db.CreateOwnerContext(SystemTenant()))
        {
            pageId = await read.Pages.Where(p => p.WorkspaceId == workspaceId).Select(p => p.Id).SingleAsync();
        }

        using var provider = CreateProvider(accountId, ownerId, workspaceId);

        // The caller may share the page, but the link belongs to the board: the guard must answer NotFound.
        var onAnotherPage = () => SendAsync<Result>(provider, new DisableShareLinkCommand("documents.page", pageId, linkOfBoard));
        await onAnotherPage.Should().ThrowAsync<AppNotFound>();

        // The link's own resource id under the other resource type names a resource that does not exist.
        var sameIdOtherType = () => SendAsync<Result>(provider, new DisableShareLinkCommand("documents.page", boardId, linkOfBoard));
        await sameIdOtherType.Should().ThrowAsync<AppNotFound>();

        (await ShareLinkStatusAsync(linkOfBoard)).Should().Be(ShareLinkStatus.Active);
    }

    [Fact]
    public async Task DisableShareLink_ByAViewerOfTheBoard_IsDenied_AndTheLinkStaysActive()
    {
        var (accountId, ownerId, workspaceId, boardId, memberId) = await SeedBoardStackAsync();
        var link = await SeedShareLinkAsync(accountId, workspaceId, BoardKind, boardId, ownerId);

        await using (var restrict = _db.CreateOwnerContext(SystemTenant()))
        {
            var board = await restrict.Boards.SingleAsync(b => b.Id == boardId);
            board.ChangeVisibility(BoardVisibility.Private, ownerId, DateTimeOffset.UtcNow);
            restrict.BoardMembers.Add(BoardMember.Create(boardId, memberId, BoardRole.Observer, DateTimeOffset.UtcNow));
            await restrict.SaveChangesAsync();
        }

        using var provider = CreateProvider(accountId, memberId, workspaceId);
        var act = () => SendAsync<Result>(provider, new DisableShareLinkCommand(BoardKind, boardId, link));

        await act.Should().ThrowAsync<AppForbidden>("a Viewer of the board cannot turn off its share links");
        (await ShareLinkStatusAsync(link)).Should().Be(ShareLinkStatus.Active);
    }

    [Fact]
    public async Task DisableShareLink_StillWorks_WhileWorkspaceShareLinksAreOff()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        var link = await SeedShareLinkAsync(accountId, workspaceId, BoardKind, boardId, ownerId);
        await SetWorkspaceShareLinksAsync(workspaceId, ownerId, allow: false);

        using var provider = CreateProvider(accountId, ownerId, workspaceId);
        var result = await SendAsync<Result>(provider, new DisableShareLinkCommand(BoardKind, boardId, link));

        result.Succeeded.Should().BeTrue("turning a link off must never need the links to be allowed");
        (await ShareLinkStatusAsync(link)).Should().Be(ShareLinkStatus.Disabled);
    }

    [Fact]
    public async Task CreateShareLink_IsRefused_WhileWorkspaceShareLinksAreOff_AndWritesNothing()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        await SetWorkspaceShareLinksAsync(workspaceId, ownerId, allow: false);

        using var provider = CreateProvider(accountId, ownerId, workspaceId);
        var act = () => SendAsync<Result<CreateShareLinkResponse>>(provider,
            new CreateShareLinkCommand(BoardKind, boardId, "Viewer"));

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be("ShareLinksTurnedOff");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.ShareLinks.IgnoreQueryFilters().AnyAsync(l => l.ResourceId == boardId)).Should().BeFalse();
    }

    [Fact]
    public async Task CreateShareLink_Succeeds_WhileWorkspaceShareLinksAreOn()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();
        await SetWorkspaceShareLinksAsync(workspaceId, ownerId, allow: true);

        using var provider = CreateProvider(accountId, ownerId, workspaceId);
        var result = await SendAsync<Result<CreateShareLinkResponse>>(provider,
            new CreateShareLinkCommand(BoardKind, boardId, "Viewer"));

        result.Succeeded.Should().BeTrue();
        (await ShareLinkStatusAsync(result.Data.ShareLink.Id)).Should().Be(ShareLinkStatus.Active);
    }

    private async Task<Guid> SeedBoardAsync(Guid accountId, Guid workspaceId, Guid ownerId)
    {
        var board = Board.Create(accountId, workspaceId, ownerId, "Other Board", null, DateTimeOffset.UtcNow);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Boards.Add(board);
        await seed.SaveChangesAsync();
        return board.Id;
    }

    private async Task<Guid> SeedShareLinkAsync(Guid accountId, Guid workspaceId, string kind, Guid resourceId, Guid createdBy)
    {
        var link = ShareLink.Create(
            accountId, workspaceId, ResourceKind.Create(kind), resourceId,
            ShareLinkTokenHash.Create(Guid.NewGuid().ToString("N")), ShareLinkAccessMode.WorkspaceOnly,
            createdBy, DateTimeOffset.UtcNow);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ShareLinks.Add(link);
        await seed.SaveChangesAsync();
        return link.Id;
    }

    private async Task<ShareLinkStatus> ShareLinkStatusAsync(Guid linkId)
    {
        await using var read = _db.CreateOwnerContext(SystemTenant());
        return await read.ShareLinks.IgnoreQueryFilters().Where(l => l.Id == linkId).Select(l => l.Status).SingleAsync();
    }

    private async Task SetWorkspaceShareLinksAsync(Guid workspaceId, Guid actorId, bool allow)
    {
        await using var write = _db.CreateOwnerContext(SystemTenant());
        var workspace = await write.Workspaces.SingleAsync(w => w.Id == workspaceId);
        workspace.UpdateSettings(WorkspaceSettings.Create(allowPublicSharing: allow), actorId, DateTimeOffset.UtcNow);
        await write.SaveChangesAsync();
        (await write.Workspaces.AsNoTracking().Where(w => w.Id == workspaceId)
            .Select(w => w.Settings.AllowPublicSharing).SingleAsync()).Should().Be(allow);
    }

    // ── composition -----------------------------------------------------------

    private static async Task<T> SendAsync<T>(ServiceProvider provider, object request)
        where T : class
    {
        using var scope = provider.CreateScope();
        // Idempotent commands (page creation) need the key a transport would bind.
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set(Guid.NewGuid().ToString("N"), IdempotencyExecutionSource.Internal);
        var response = await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        return (response as T)!;
    }

    private ServiceProvider CreateProvider(Guid accountId, Guid userId, Guid? selectedWorkspaceId = null)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);
        if (selectedWorkspaceId.HasValue)
        {
            tenant.SetWorkspace(accountId, selectedWorkspaceId.Value, userId);
        }

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.RequireWorkspaceId()).Returns(() => tenant.WorkspaceId ?? Guid.Empty);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(GrantResourcePermissionCommand).Assembly));
        services.AddTransient<IValidator<GrantResourcePermissionCommand>, GrantResourcePermissionCommandValidator>();
        services.AddTransient<IValidator<RevokeResourcePermissionCommand>, RevokeResourcePermissionCommandValidator>();
        services.AddTransient<IValidator<GetResourcePermissionsQuery>, GetResourcePermissionsQueryValidator>();
        services.AddTransient<IValidator<CreatePageCommand>, CreatePageCommandValidator>();
        services.AddTransient<IValidator<CreateCommentCommand>, CreateCommentCommandValidator>();
        services.AddTransient<IValidator<ArchivePageCommand>, ArchivePageCommandValidator>();

        // Canonical frozen pipeline, outermost-to-innermost order.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateOwnerContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Notrelix.Application.Common.Tenancy.IWorkspaceAccessResolver, WorkspaceAccessResolver>();
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<global::Notrelix.Application.Features.Integrations.Abstractions.IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<
            global::Notrelix.Application.Features.Documents.Public.PageAuthorization.IPageAuthorizationFacts,
            global::Notrelix.Infrastructure.Data.Authz.PostgresPageAuthorizationFacts>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<global::Notrelix.Application.Features.Documents.Public.PageAuthorization.IPageAuthorizationFacts>(),
                new FakeBillingSubscriptionFacts()));
        services.AddGovernanceInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<IClientMetadata>().Object);
        services.AddScoped<Notrelix.Application.Features.Governance.Public.Access.IResourceAccessChangeRecorder, Notrelix.Application.Features.Governance.AccessChanges.ResourceAccessChangeRecorder>();

        services.AddScoped<IResourceLocator, ResourceLocator>();
        // ADR-009 placement: the create handlers resolve Space placement targets
        // and the target authorizer even when the request is not placed.
        services.AddScoped<global::Notrelix.Application.Features.Workspaces.Public.SpacePlacement.ISpacePlacementTargets,
            global::Notrelix.Infrastructure.Workspaces.Spaces.SpacePlacementTargets>();
        services.AddScoped<global::Notrelix.Application.Features.WorkManagement.Boards.Services.IBoardSpacePlacement,
            global::Notrelix.Application.Features.WorkManagement.Boards.Services.BoardSpacePlacement>();
        services.AddScoped<global::Notrelix.Application.Features.Documents.Pages.Services.IPageSpacePlacement,
            global::Notrelix.Application.Features.Documents.Pages.Services.PageSpacePlacement>();
        services.AddScoped<global::Notrelix.Application.Features.Documents.Pages.Services.IPageSubtreeStore,
            global::Notrelix.Infrastructure.Documents.Pages.PageSubtreeStore>();
        services.AddScoped<global::Notrelix.Application.Features.Documents.Blocks.Services.IPageBlockWriteLock,
            global::Notrelix.Infrastructure.Documents.Blocks.PostgresPageBlockWriteLock>();
        services.AddScoped<global::Notrelix.Application.Features.Documents.Pages.Services.IPageSubtreeAuthorizer,
            global::Notrelix.Application.Features.Documents.Pages.Services.PageSubtreeAuthorizer>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();

        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();
        services.AddScoped<IIdempotencyStore>(sp =>
            new EfIdempotencyStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IOptions<IdempotencyOptions>>()));
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        // Handlers under test.
        services.AddScoped<
            IRequestHandler<GrantResourcePermissionCommand, Result<ResourcePermissionDto>>,
            GrantResourcePermissionCommandHandler>();
        services.AddScoped<
            IRequestHandler<RevokeResourcePermissionCommand, Result>,
            RevokeResourcePermissionCommandHandler>();
        services.AddScoped<
            IRequestHandler<GetResourcePermissionsQuery, Result<List<ResourcePermissionDto>>>,
            GetResourcePermissionsQueryHandler>();
        services.AddScoped<
            IRequestHandler<DisableShareLinkCommand, Result>,
            DisableShareLinkCommandHandler>();
        services.AddScoped<
            IRequestHandler<CreateShareLinkCommand, Result<CreateShareLinkResponse>>,
            CreateShareLinkCommandHandler>();
        services.AddScoped<
            IRequestHandler<CreatePageCommand, Result<Guid>>,
            CreatePageCommandHandler>();
        services.AddScoped<
            IRequestHandler<ArchivePageCommand, Result>,
            ArchivePageCommandHandler>();
        services.AddScoped<
            IRequestHandler<RestorePageCommand, Result>,
            RestorePageCommandHandler>();
        services.AddScoped<
            IRequestHandler<DeletePageCommand, Result>,
            DeletePageCommandHandler>();
        services.AddScoped<
            IRequestHandler<UpdatePageCommand, Result>,
            UpdatePageCommandHandler>();
        services.AddScoped<
            IRequestHandler<GetTrashedPagesQuery, Result<CursorPage<PageDto>>>,
            GetTrashedPagesQueryHandler>();
        services.AddScoped<
            IRequestHandler<CreateBlockCommand, Result<BlockDto>>,
            CreateBlockCommandHandler>();
        services.AddScoped<
            IRequestHandler<UpdateBlockCommand, Result<BlockDto>>,
            UpdateBlockCommandHandler>();
        services.AddScoped<
            IRequestHandler<GetPageBacklinksQuery, Result<PageBacklinksDto>>,
            GetPageBacklinksQueryHandler>();
        services.AddScoped<global::Notrelix.Application.Features.Documents.Pages.Services.IPageBacklinkReader,
            global::Notrelix.Infrastructure.Documents.Pages.PageBacklinkReader>();
        services.AddScoped<global::Notrelix.Application.Features.Governance.Public.Visibility.IResourceVisibility,
            global::Notrelix.Infrastructure.Data.Authz.PostgresResourceVisibility>();
        services.AddScoped<global::Notrelix.Application.Features.WorkManagement.Public.ItemNotifications.IWorkItemNotificationFacts,
            global::Notrelix.Infrastructure.CrossContext.Collaboration.WorkManagement.WorkItemNotificationFactsAdapter>();
        services.AddScoped<global::Notrelix.Application.Features.Governance.Public.Access.IResourceCreatorGrant,
                    global::Notrelix.Infrastructure.Data.Authz.ResourceCreatorGrantAdapter>();
        services.AddScoped<
            IRequestHandler<CreateCommentCommand, Result<Guid>>,
            CreateCommentCommandHandler>();
        services.AddScoped<
            IRequestHandler<UpdateCommentCommand, Result>,
            UpdateCommentCommandHandler>();
        services.AddScoped<
            IRequestHandler<ResolveCommentCommand, Result>,
            ResolveCommentCommandHandler>();
        services.AddScoped<
            IRequestHandler<DeletePageAttachmentCommand, Result>,
            DeletePageAttachmentCommandHandler>();
        services.AddScoped<Notrelix.Application.Features.Collaboration.Comments.Services.ICommentTargetAccess,
            Notrelix.Application.Features.Collaboration.Comments.Services.CommentTargetAccess>();
        services.AddScoped<MoveBoardItemUseCase>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Ordering.WorkPlacementResolver>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Ordering.IWorkOrderingScopeLock,
            Notrelix.Infrastructure.Data.WorkManagement.PostgresWorkOrderingScopeLock>();
        services.AddScoped<
            IRequestHandler<MoveBoardItemCommand, BoardItemSlimDto>,
            MoveBoardItemCommandHandler>();
        services.AddScoped<
            IRequestHandler<ListBoardRelationsQuery, Result<List<BoardRelationDto>>>,
            ListBoardRelationsQueryHandler>();
        services.AddScoped<
            IRequestHandler<GetBoardQuery, Result<BoardDto>>,
            GetBoardQueryHandler>();

        services.AddProductionCrossContextAdapters();

        return services.BuildServiceProvider();
    }

    // ── seeding ---------------------------------------------------------------

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId, Guid BoardId, Guid MemberId)> SeedBoardStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("Gov Board Account", $"gov-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "Gov Board WS", $"gov-board-{Guid.NewGuid():N}", now);
        var board = Board.Create(account.Id, workspace.Id, owner.Id, "Gov Board", null, now);
        var group = BoardGroup.Create(
            account.Id, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), owner.Id, now);
        var item = BoardItem.CreateRoot(
            account.Id, workspace.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), owner.Id, now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        await SyncAccessGrantsAsync(
            account.Id, workspace.Id,
            (owner.Id, WorkspaceRole.Owner), (member.Id, WorkspaceRole.Member));

        return (account.Id, owner.Id, workspace.Id, board.Id, member.Id);
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId, Guid PageId, Guid MemberId)> SeedPageStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("Gov Page Account", $"gov-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "Gov Page WS", $"gov-page-{Guid.NewGuid():N}", now);
        var page = Page.Create(account.Id, workspace.Id, "Gov Page", owner.Id, now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
        seed.Pages.Add(page);
        await seed.SaveChangesAsync();

        await SyncAccessGrantsAsync(
            account.Id, workspace.Id,
            (owner.Id, WorkspaceRole.Owner), (member.Id, WorkspaceRole.Member));

        return (account.Id, owner.Id, workspace.Id, page.Id, member.Id);
    }

    /// <summary>
    /// Production creates authz.access_grants rows through the membership
    /// command projection; seeding rows directly skips it, so this helper
    /// restores the projection the RLS policies authorize on.
    /// </summary>
    private async Task SyncAccessGrantsAsync(Guid accountId, Guid workspaceId, params (Guid UserId, WorkspaceRole Role)[] members)
    {
        var now = DateTimeOffset.UtcNow;
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        var projection = new AccessGrantProjectionService(seed);
        foreach (var (userId, role) in members)
        {
            await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, role, now, CancellationToken.None);
        }

        await seed.SaveChangesAsync();
    }

    private async Task SeedPermissionRuleAsync(
        Guid accountId,
        Guid workspaceId,
        Guid subjectId,
        PermissionAction action,
        string resourceKind,
        Guid resourceId,
        PermissionEffect effect,
        int priority = 100,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? expiresAt = null)
    {
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.PermissionRules.Add(PermissionRule.Create(
            accountId,
            workspaceId,
            PermissionScopeType.Page,
            ResourceKind.Create(resourceKind),
            resourceId,
            PermissionSubjectType.User,
            subjectId,
            null,
            action,
            effect,
            subjectId,
            DateTimeOffset.UtcNow,
            priority: priority,
            startsAt: startsAt,
            expiresAt: expiresAt));
        await seed.SaveChangesAsync();
    }

    private async Task SeedWorkspaceMemberAsync(Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(
            accountId, workspaceId, userId, role, userId, DateTimeOffset.UtcNow));
        await seed.SaveChangesAsync();
    }

    private async Task SeedResourcePermissionAsync(
        Guid accountId, Guid workspaceId, string resourceKind, Guid resourceId, Guid subjectId, PermissionLevel level)
    {
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ResourcePermissions.Add(ResourcePermission.Grant(
            accountId, workspaceId,
            ResourceKind.Create(resourceKind),
            resourceId,
            PermissionSubjectType.User,
            subjectId,
            level,
            subjectId,
            DateTimeOffset.UtcNow));
        await seed.SaveChangesAsync();
    }

    private async Task AssertAuditRecordedAsync(Guid workspaceId, Guid actorId, string action)
    {
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.EnterpriseAuditLogs.AnyAsync(a =>
            a.WorkspaceId == workspaceId && a.ActorUserId == actorId && a.Action == action))
            .Should().BeTrue($"an audit row for '{action}' must be committed");
    }

    private async Task<Guid> ResolveBoardItemIdAsync(Guid boardId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var id = await context.BoardItems
            .Where(i => i.BoardId == boardId)
            .Select(i => i.Id)
            .FirstOrDefaultAsync();
        id.Should().NotBeEmpty($"expected a seeded item on board {boardId}");
        return id;
    }

    private async Task<Guid> ResolveOtherGroupIdAsync(Guid boardId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var id = await context.BoardGroups
            .Where(g => g.BoardId == boardId)
            .Select(g => g.Id)
            .FirstOrDefaultAsync();
        id.Should().NotBeEmpty($"expected a seeded group on board {boardId}");
        return id;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}