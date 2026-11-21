using Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The workspace activity feed on PostgreSQL through the real pipeline: an Owner, Admin or Member reads it (through the policy
/// engine, not a handler role check), a Guest is forbidden, a person outside the workspace or in another tenant is told it does not
/// exist, a fact about a Private Board or a Page the caller is denied is not shown (nor its actor named), and the feed walks by cursor.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceActivityIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceActivityIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private Task SeedFactAsync(
        OrgStack stack, string type, string subjectType, Guid subjectId, int minutesAgo, Guid? actor = null,
        string sourceContext = "work", string? targetType = null, Guid? targetId = null, string? resourceKind = null, Guid? resourceId = null) =>
        _host.WithSystemContextAsync(seed =>
        {
            seed.WorkspaceActivityLogs.Add(WorkspaceActivityLogRecord.Create(
                stack.WorkspaceId, sourceContext, type, subjectType, subjectId, OrgTreeTestHost.BaseTime.AddHours(-1).AddMinutes(-minutesAgo),
                sourceEventId: Guid.CreateVersion7(), actorUserId: actor ?? stack.OwnerId, subjectDisplayName: type,
                targetType: targetType, targetId: targetId, resourceType: resourceKind, resourceId: resourceId, title: type));
            return Task.CompletedTask;
        });

    private async Task<WorkspaceActivityPage> ReadAsync(OrgStack stack, Guid userId, string? cursor = null, int? limit = null)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        var page = await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceActivityQuery(stack.WorkspaceId, cursor, limit));
        return new WorkspaceActivityPage(page.Items.Select(i => i.Type).ToArray(), page.NextCursor);
    }

    private sealed record WorkspaceActivityPage(string[] Types, string? NextCursor);

    [Fact]
    public async Task OwnerAdminAndMember_ReadTheFeed_NewestFirst()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var board = await _host.SeedBoardAsync(stack, "Roadmap");
        await SeedFactAsync(stack, "work.board-created", "Board", board, 5);
        await SeedFactAsync(stack, "workspace.member-invited", "WorkspaceMember", member, 1, sourceContext: "workspace");

        foreach (var reader in new[] { stack.OwnerId, admin, member })
            (await ReadAsync(stack, reader)).Types.Should().Equal("workspace.member-invited", "work.board-created");
    }

    [Fact]
    public async Task AGuest_IsForbidden_AnOutsiderAndAnotherTenantAreNotFound()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var other = await _host.SeedWorkspaceAsync();
        await SeedFactAsync(stack, "workspace.member-invited", "WorkspaceMember", guest, 1, sourceContext: "workspace");

        var asGuest = () => ReadAsync(stack, guest);
        await asGuest.Should().ThrowAsync<AppForbidden>("the feed names people, and the member list is not guest-safe");

        var asOutsider = () => ReadAsync(stack, outsider);
        await asOutsider.Should().ThrowAsync<AppNotFound>();

        using var asOtherOwner = _host.CreateProvider(other.AccountId, other.OwnerId);
        var foreign = () => OrgTreeTestHost.QueryAsync(asOtherOwner, new GetWorkspaceActivityQuery(stack.WorkspaceId));
        await foreign.Should().ThrowAsync<AppNotFound>("another tenant's workspace answers like a made-up id");
    }

    [Fact]
    public async Task AFactAboutAPrivateBoardOrADeniedPage_IsNotShownToAMember_ButIsToTheOwner()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var shared = await _host.SeedBoardAsync(stack, "Shared");
        var secret = await _host.SeedBoardAsync(stack, "Secret", BoardVisibility.Private);
        var openPage = await _host.SeedPageAsync(stack, "Open page");
        var deniedPage = await _host.SeedPageAsync(stack, "Denied page");
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ViewPage, PermissionEffect.Deny, "documents.page", deniedPage);

        await SeedFactAsync(stack, "work.board-created", "Board", shared, 9);
        await SeedFactAsync(stack, "work.board-created", "Board", secret, 8);
        await SeedFactAsync(stack, "work.item-renamed", "BoardItem", Guid.CreateVersion7(), 7, targetType: "Board", targetId: secret,
            resourceKind: "work-management.board-item", resourceId: Guid.CreateVersion7());
        await SeedFactAsync(stack, "docs.page-renamed", "Page", openPage, 6, sourceContext: "documents",
            resourceKind: "documents.page", resourceId: openPage);
        await SeedFactAsync(stack, "docs.page-renamed", "Page", deniedPage, 5, sourceContext: "documents",
            resourceKind: "documents.page", resourceId: deniedPage);
        // A comment on an Item names no Board, so no reader is shown it.
        await SeedFactAsync(stack, "collab.comment-created", "Comment", Guid.CreateVersion7(), 4, sourceContext: "collaboration",
            targetType: "work-management.board-item", targetId: Guid.CreateVersion7());

        (await ReadAsync(stack, member)).Types.Should().Equal(
            ["docs.page-renamed", "work.board-created"],
            "the member reads the shared Board and the open Page only: nothing about the Private Board, its Items, the denied Page or the Item comment");
        (await ReadAsync(stack, stack.OwnerId)).Types.Should().HaveCount(5, "the Owner reads every Board and Page; the Item comment stays unresolved for everyone");
    }

    [Fact]
    public async Task TheFeed_WalksByCursor_AndAnArchivedWorkspaceStillAnswers()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        for (var i = 1; i <= 5; i++)
            await SeedFactAsync(stack, $"workspace.member-invited.{i}", "WorkspaceMember", member, i, sourceContext: "workspace");

        var first = await ReadAsync(stack, member, limit: 2);
        var second = await ReadAsync(stack, member, first.NextCursor, 2);
        var third = await ReadAsync(stack, member, second.NextCursor, 2);

        first.Types.Should().Equal("workspace.member-invited.1", "workspace.member-invited.2");
        second.Types.Should().Equal("workspace.member-invited.3", "workspace.member-invited.4");
        third.Types.Should().Equal("workspace.member-invited.5");
        third.NextCursor.Should().BeNull();

        await _host.WithSystemContextAsync(async seed =>
        {
            var ws = await seed.Workspaces.SingleAsync(w => w.Id == stack.WorkspaceId);
            ws.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime.AddDays(1));
        });
        (await ReadAsync(stack, member, limit: 2)).Types.Should().HaveCount(2, "a read changes nothing, so an archived workspace still answers");
    }
}
