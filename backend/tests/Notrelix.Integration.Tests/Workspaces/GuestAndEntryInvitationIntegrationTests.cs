using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Infrastructure.Workspaces.Spaces;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// WSP-026 / GOV-030 evidence on real Postgres: a Workspace invitation applies
/// its initial Team/Space entries atomically with the membership, and a
/// resource invitation creates (or reuses) a Guest membership plus one explicit
/// User-subject entry, view-only (GOV-030). Negative cases leave state unchanged.
/// </summary>
[Collection("Database")]
public sealed class GuestAndEntryInvitationIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private const string InviteeEmail = "guest@example.com";

    private readonly PostgresTestContainer _db;

    public GuestAndEntryInvitationIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed record World(
        Guid AccountId, Guid WorkspaceId, Guid OwnerId, Guid UserId,
        Guid BoardId, Guid TeamId, Guid SpaceId, Guid OtherWorkspaceBoardId);

    private async Task<World> SeedWorldAsync(string userEmail = InviteeEmail, bool guestInvites = true)
    {
        var ownerId = Guid.CreateVersion7();
        var account = Account.Create("Guest Account", $"guest-{Guid.CreateVersion7():N}", AccountType.Team, ownerId, Now);
        var user = User.Create(userEmail, "Guest User", "hashed", Now, true);
        user.ConfirmEmail(user.Id, Now);
        var ws = Workspace.Create(account.Id, ownerId, "Guest WS", $"gws-{Guid.CreateVersion7():N}", Now);
        if (guestInvites)
            ws.UpdateSettings(WorkspaceSettings.Create(allowGuestInvites: true), ownerId, Now);
        var other = Workspace.Create(account.Id, ownerId, "Other WS", $"ows-{Guid.CreateVersion7():N}", Now);
        var board = Board.Create(account.Id, ws.Id, ownerId, "Shared board", null, Now);
        var otherBoard = Board.Create(account.Id, other.Id, ownerId, "Other board", null, Now);
        var team = Team.Create(account.Id, ws.Id, "Design", ownerId, Now);
        var space = Space.Create(account.Id, ws.Id, "Client space", SpaceVisibility.Workspace, ownerId, Now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Accounts.Add(account);
        seed.Users.Add(user);
        seed.Workspaces.AddRange(ws, other);
        seed.Boards.AddRange(board, otherBoard);
        seed.Teams.Add(team);
        seed.Spaces.Add(space);
        await seed.SaveChangesAsync();
        return new World(account.Id, ws.Id, ownerId, user.Id, board.Id, team.Id, space.Id, otherBoard.Id);
    }

    private async Task<string> SeedInvitationAsync(
        World w,
        Func<InvitationTokenHash, int, WorkspaceInvitation> factory)
    {
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var invitation = factory(InvitationTokenHash.Create(issued.TokenHash), issued.HashVersion);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.WorkspaceInvitations.Add(invitation);
        await seed.SaveChangesAsync();
        return issued.RawToken;
    }

    private Task<string> SeedWorkspaceInvitationAsync(
        World w, WorkspaceRole role, IReadOnlyList<WorkspaceInvitationInitialEntry>? entries = null,
        DateTimeOffset? createdAt = null) =>
        SeedInvitationAsync(w, (hash, v) => WorkspaceInvitation.Create(
            w.AccountId, w.WorkspaceId, InviteeEmail, role, hash, v, w.OwnerId,
            createdAt ?? Now.AddHours(-1), initialEntries: entries));

    /// <summary>
    /// A Guest invitation that names Team entries was possible before a Guest invitation could not name a Team: the domain
    /// now refuses to create one, so an invitation already in flight is stored as a Member invitation and its role set to
    /// Guest directly.
    /// </summary>
    private async Task<string> SeedInFlightGuestInvitationAsync(World w, IReadOnlyList<WorkspaceInvitationInitialEntry> entries)
    {
        var token = await SeedWorkspaceInvitationAsync(w, WorkspaceRole.Member, entries);
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        await ctx.Database.ExecuteSqlRawAsync("UPDATE workspace.workspace_invitations SET role = 'Guest' WHERE workspace_id = {0}", w.WorkspaceId);
        return token;
    }

    private Task<string> SeedResourceInvitationAsync(World w, Guid boardId, string level) =>
        SeedInvitationAsync(w, (hash, v) => WorkspaceInvitation.CreateForResource(
            w.AccountId, w.WorkspaceId, InviteeEmail,
            new WorkspaceInvitationResourceGrant(WorkspaceInvitationResourceGrant.BoardKind, boardId, level),
            hash, v, w.OwnerId, Now.AddHours(-1)));

    private async Task<(Result<AcceptInvitationResultDto> Result, bool Threw)> AcceptAsync(
        Guid userId, string rawToken, bool throwAfterHandler = false)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), SystemTenant()),
            NullLogger<EfRequestDataSession>.Instance);
        var projection = new AccessGrantProjectionService(context);
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(userId);
        requestContext.Setup(r => r.IsAuthenticated).Returns(true);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(d => d.UtcNow).Returns(Now);
        var handler = new AcceptInvitationCommandHandler(
            context,
            new IdentityUserFactsProvider(context),
            new AccountMembershipActions(context, new AccountGrantProjectionServiceAdapter(projection)),
            new AccountMembershipFactsProvider(context),
            new OneTimeTokenService(),
            requestContext.Object,
            clock.Object,
            new WorkspaceGrantProjectionServiceAdapter(projection),
            new InvitationInitialEntries(context, new SpaceStore(context)),
            new GuestResourceAccessActionsAdapter(context),
            new Mock<IAuditService>().Object,
            new Notrelix.Application.Features.Workspaces.Invitations.Services.InvitationTokenScope(
                context, SystemTenant(), new Mock<IRlsSessionContext>().Object),
            SeatGates.AlwaysAdmit,
            new Mock<Notrelix.Application.Features.Identity.Public.InvitedRegistration.IInvitedUserRegistrationActions>().Object);

        try
        {
            var result = await session.ExecuteAsync(
                new RequestDataSessionOptions(RequestDataAccess.Transactional, false, false, null),
                async ct =>
                {
                    var r = await handler.Handle(new AcceptInvitationCommand(rawToken), ct);
                    if (throwAfterHandler) throw new InvalidOperationException("forced failure after handler");
                    return r;
                },
                CancellationToken.None);
            return (result, false);
        }
        catch (InvalidOperationException) when (throwAfterHandler)
        {
            return (Result<AcceptInvitationResultDto>.Failure("threw"), true);
        }
    }

    private static WorkspaceInvitationInitialEntry[] Entries(World w) =>
    [
        new(WorkspaceInvitationEntryKind.Team, w.TeamId, "Member"),
        new(WorkspaceInvitationEntryKind.Space, w.SpaceId, "Viewer"),
    ];

    // ── Workspace invitation with initial entries ─────────────────────────

    [Fact]
    public async Task WorkspaceInvitation_AppliesMembershipAndInitialEntriesAtomically()
    {
        var w = await SeedWorldAsync();
        var token = await SeedInFlightGuestInvitationAsync(w, Entries(w));

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.WorkspaceMembers.SingleAsync(m => m.WorkspaceId == w.WorkspaceId && m.UserId == w.UserId))
            .Role.Should().Be(WorkspaceRole.Guest, "guest is the membership fact the invitation names");
        // A Guest is not a Team member (Workspaces_Team_GuestCannotJoin, workspaces.md section 46): the Team entry of an
        // invitation already in flight is skipped, the Space entry is applied.
        (await probe.TeamMembers.CountAsync(m => m.TeamId == w.TeamId && m.UserId == w.UserId))
            .Should().Be(0);
        (await probe.SpaceMembers.CountAsync(m => m.SpaceId == w.SpaceId && m.SubjectId == w.UserId && m.Role == SpaceRole.Viewer))
            .Should().Be(1);
    }

    [Fact]
    public async Task WorkspaceInvitation_WhenEntriesApplyThenFailureOccurs_RollsBackMembershipAndEntries()
    {
        var w = await SeedWorldAsync();
        var token = await SeedWorkspaceInvitationAsync(w, WorkspaceRole.Member, Entries(w));

        var (_, threw) = await AcceptAsync(w.UserId, token, throwAfterHandler: true);

        threw.Should().BeTrue();
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.WorkspaceMembers.CountAsync(m => m.WorkspaceId == w.WorkspaceId && m.UserId == w.UserId)).Should().Be(0);
        (await probe.TeamMembers.CountAsync(m => m.TeamId == w.TeamId && m.UserId == w.UserId)).Should().Be(0);
        (await probe.SpaceMembers.CountAsync(m => m.SpaceId == w.SpaceId && m.SubjectId == w.UserId)).Should().Be(0);
        (await probe.WorkspaceInvitations.SingleAsync(i => i.WorkspaceId == w.WorkspaceId)).Status
            .Should().Be(WorkspaceInvitationStatus.Pending, "a rolled-back accept leaves the invitation usable");
    }

    [Fact]
    public async Task WorkspaceInvitation_WhenNamedTeamIsGone_FailsClosedWithoutMutation()
    {
        var w = await SeedWorldAsync();
        var token = await SeedWorkspaceInvitationAsync(w, WorkspaceRole.Member, Entries(w));
        await using (var ctx = _db.CreateOwnerContext(SystemTenant()))
        {
            var team = await ctx.Teams.SingleAsync(t => t.Id == w.TeamId);
            team.Delete(w.OwnerId, Now);
            await ctx.SaveChangesAsync();
        }

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainMatch("*no longer available*");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.WorkspaceMembers.CountAsync(m => m.WorkspaceId == w.WorkspaceId && m.UserId == w.UserId)).Should().Be(0);
        (await probe.SpaceMembers.CountAsync(m => m.SpaceId == w.SpaceId && m.SubjectId == w.UserId)).Should().Be(0);
        (await probe.WorkspaceInvitations.SingleAsync(i => i.WorkspaceId == w.WorkspaceId)).Status
            .Should().Be(WorkspaceInvitationStatus.Pending);
    }

    // ── Resource invitation ────────────────────────────────────────────────

    [Fact]
    public async Task ResourceInvitation_CreatesGuestMembershipAndAViewerUserSubjectEntry()
    {
        var w = await SeedWorldAsync();
        var token = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.WorkspaceMembers.SingleAsync(m => m.WorkspaceId == w.WorkspaceId && m.UserId == w.UserId))
            .Role.Should().Be(WorkspaceRole.Guest, "a resource invitation never creates a member");
        var entry = await probe.ResourcePermissions.SingleAsync(p => p.ResourceId == w.BoardId);
        entry.SubjectType.Should().Be(PermissionSubjectType.User, "ExternalEmail subjects are never created");
        entry.SubjectId.Should().Be(w.UserId);
        entry.Level.Should().Be(PermissionLevel.Viewer);
        entry.WorkspaceId.Should().Be(w.WorkspaceId);
        (await probe.TeamMembers.CountAsync(m => m.UserId == w.UserId)).Should().Be(0, "no Team entries");
        (await probe.SpaceMembers.CountAsync(m => m.SubjectId == w.UserId)).Should().Be(0, "no Space entries");
    }

    [Fact]
    public async Task ResourceInvitation_ReusesExistingMembershipAndNeverLowersAnExistingEntry()
    {
        var w = await SeedWorldAsync();
        var first = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");
        (await AcceptAsync(w.UserId, first)).Result.Errors.Should().BeEmpty();
        await using (var raise = _db.CreateOwnerContext(SystemTenant()))
            await raise.Database.ExecuteSqlRawAsync("UPDATE governance.resource_permissions SET permission_level = 'Editor'");
        var second = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");

        var (result, _) = await AcceptAsync(w.UserId, second);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.WorkspaceMembers.CountAsync(m => m.WorkspaceId == w.WorkspaceId && m.UserId == w.UserId)).Should().Be(1);
        (await probe.ResourcePermissions.Where(p => p.ResourceId == w.BoardId).ToListAsync())
            .Should().ContainSingle().Which.Level.Should().Be(PermissionLevel.Editor, "an existing entry is never lowered by a later invitation");
    }

    [Fact]
    public async Task AnInvitationStoredBeforeTheViewerCap_StillWritesAViewerEntry()
    {
        var w = await SeedWorldAsync();
        var token = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");
        await using (var older = _db.CreateOwnerContext(SystemTenant()))
            await older.Database.ExecuteSqlRawAsync("UPDATE workspace.workspace_invitations SET resource_level = 'Editor'");

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ResourcePermissions.SingleAsync(p => p.ResourceId == w.BoardId))
            .Level.Should().Be(PermissionLevel.Viewer, "a guest never gets more than view");
    }

    [Fact]
    public async Task ResourceInvitation_ForResourceOfAnotherWorkspace_FailsClosedWithoutMutation()
    {
        var w = await SeedWorldAsync();
        var token = await SeedResourceInvitationAsync(w, w.OtherWorkspaceBoardId, "Viewer");

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeFalse("cross-tenant resource is unavailable");
        result.Errors.Should().ContainMatch("*no longer available*");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ResourcePermissions.CountAsync()).Should().Be(0);
        (await probe.WorkspaceMembers.CountAsync(m => m.UserId == w.UserId)).Should().Be(0);
    }

    [Fact]
    public async Task ResourceInvitation_WhenAcceptFailsAfterEntryCreated_RollsBackEntryAndMembership()
    {
        var w = await SeedWorldAsync();
        var token = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");

        var (_, threw) = await AcceptAsync(w.UserId, token, throwAfterHandler: true);

        threw.Should().BeTrue();
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ResourcePermissions.CountAsync()).Should().Be(0);
        (await probe.WorkspaceMembers.CountAsync(m => m.UserId == w.UserId)).Should().Be(0);
    }

    [Fact]
    public async Task ResourceInvitation_DatabaseRejectsLevelAboveEditorAndNonGuestRole()
    {
        var w = await SeedWorldAsync();
        await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");
        await using var ctx = _db.CreateOwnerContext(SystemTenant());

        var tooHigh = async () => await ctx.Database.ExecuteSqlRawAsync(
            "UPDATE workspace.workspace_invitations SET resource_level = 'Manager'");
        await tooHigh.Should().ThrowAsync<Exception>("ck_workspace_invitations_resource_grant caps the level at Editor");

        var notGuest = async () => await ctx.Database.ExecuteSqlRawAsync(
            "UPDATE workspace.workspace_invitations SET role = 'Member'");
        await notGuest.Should().ThrowAsync<Exception>("a resource invitation never creates a member");
    }

    // ── AllowGuestInvites off: an invitation issued earlier cannot be accepted ─────────

    [Fact]
    public async Task ResourceInvitation_IssuedBeforeGuestInvitesWereTurnedOff_IsRefusedAtAccept_AndStaysPending()
    {
        var w = await SeedWorldAsync(guestInvites: false);
        var token = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");

        var accept = () => AcceptAsync(w.UserId, token);

        (await accept.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be("workspace.guest_invites_off");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.WorkspaceMembers.CountAsync(m => m.WorkspaceId == w.WorkspaceId && m.UserId == w.UserId))
            .Should().Be(0, "the refused Guest gets no membership");
        (await probe.ResourcePermissions.CountAsync()).Should().Be(0, "and no entry on the resource");
        (await probe.WorkspaceInvitations.SingleAsync(i => i.WorkspaceId == w.WorkspaceId)).Status
            .Should().Be(WorkspaceInvitationStatus.Pending, "the invitation stays usable if the setting comes back on");
    }

    [Fact]
    public async Task ResourceInvitation_IsAcceptedAgain_WhenGuestInvitesAreTurnedBackOn()
    {
        var w = await SeedWorldAsync(guestInvites: false);
        var token = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");
        var refused = () => AcceptAsync(w.UserId, token);
        await refused.Should().ThrowAsync<AppForbidden>();
        await using (var ctx = _db.CreateOwnerContext(SystemTenant()))
        {
            var ws = await ctx.Workspaces.SingleAsync(x => x.Id == w.WorkspaceId);
            ws.UpdateSettings(WorkspaceSettings.Create(allowGuestInvites: true), w.OwnerId, Now);
            await ctx.SaveChangesAsync();
        }

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ResourcePermissions.CountAsync(p => p.ResourceId == w.BoardId && p.SubjectId == w.UserId)).Should().Be(1);
    }

    [Fact]
    public async Task WorkspaceGuestInvitation_IssuedBeforeGuestInvitesWereTurnedOff_IsRefusedAtAccept_AndAMemberInvitationIsNot()
    {
        var w = await SeedWorldAsync(guestInvites: false);
        var guestToken = await SeedWorkspaceInvitationAsync(w, WorkspaceRole.Guest);

        var accept = () => AcceptAsync(w.UserId, guestToken);

        (await accept.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be("workspace.guest_invites_off");
        await using (var probe = _db.CreateOwnerContext(SystemTenant()))
        {
            (await probe.WorkspaceMembers.CountAsync(m => m.WorkspaceId == w.WorkspaceId && m.UserId == w.UserId)).Should().Be(0);
            (await probe.WorkspaceInvitations.SingleAsync(i => i.WorkspaceId == w.WorkspaceId)).Status
                .Should().Be(WorkspaceInvitationStatus.Pending);
        }

        await using (var clear = _db.CreateOwnerContext(SystemTenant()))
            await clear.WorkspaceInvitations.ExecuteDeleteAsync();
        var memberToken = await SeedWorkspaceInvitationAsync(w, WorkspaceRole.Member);
        var (result, _) = await AcceptAsync(w.UserId, memberToken);
        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
    }

    // ── Shared negative cases ──────────────────────────────────────────────

    [Fact]
    public async Task Accept_WithDifferentVerifiedEmail_IsRejectedWithoutMutation()
    {
        var w = await SeedWorldAsync(userEmail: "someone-else@example.com");
        var token = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainMatch("*different email*");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ResourcePermissions.CountAsync()).Should().Be(0);
        (await probe.WorkspaceMembers.CountAsync(m => m.UserId == w.UserId)).Should().Be(0);
    }

    [Fact]
    public async Task Accept_ExpiredInvitation_IsRejectedWithoutMutation()
    {
        var w = await SeedWorldAsync();
        var token = await SeedWorkspaceInvitationAsync(w, WorkspaceRole.Member, Entries(w), createdAt: Now.AddDays(-30));

        var (result, _) = await AcceptAsync(w.UserId, token);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainMatch("*expired*");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.WorkspaceMembers.CountAsync(m => m.UserId == w.UserId)).Should().Be(0);
        (await probe.TeamMembers.CountAsync(m => m.UserId == w.UserId)).Should().Be(0);
    }

    [Fact]
    public async Task Accept_SecondUseOfTheSameToken_IsRejected()
    {
        var w = await SeedWorldAsync();
        var token = await SeedResourceInvitationAsync(w, w.BoardId, "Viewer");
        (await AcceptAsync(w.UserId, token)).Result.Errors.Should().BeEmpty();

        var (second, _) = await AcceptAsync(w.UserId, token);

        second.Succeeded.Should().BeFalse("an invitation token is single use");
        second.Errors.Should().ContainMatch("*no longer valid*");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ResourcePermissions.CountAsync()).Should().Be(1);
    }
}
