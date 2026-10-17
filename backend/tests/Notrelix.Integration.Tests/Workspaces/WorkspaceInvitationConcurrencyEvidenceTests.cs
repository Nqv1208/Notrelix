using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.EventMappers.Workspaces;
using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Security.Encryption;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// WG-CONC-002 / CERT-CONC-002 invitation concurrency evidence under real
/// PostgreSQL.
///
/// Three races are proven, because each resolves on a different owner:
///
/// <list type="bullet">
/// <item>two concurrent accepts of the same invitation — the membership and
/// grant uniqueness constraints must admit exactly one winner;</item>
/// <item>accept racing revoke — the invitation lifecycle status and the
/// granted membership must not diverge;</item>
/// <item>accept presenting a token from a superseded generation — token
/// rotation must make the stale credential unusable.</item>
/// </list>
///
/// Interleaving is deterministic: both participants read pre-state and stage
/// their mutation before either commits, so the commit order below fixes the
/// race outcome without timing dependence or sleeps.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceInvitationConcurrencyEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset FixedTime = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceInvitationConcurrencyEvidenceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed record InvitationGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid InvitationId,
        Guid UserId,
        Guid OwnerId,
        string RawToken,
        string TokenHash);

    private async Task<InvitationGraph> SeedPendingInvitationAsync()
    {
        var accountId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var account = Account.Create(
            "Invitation Concurrency Account",
            $"inv-conc-{Guid.CreateVersion7():N}",
            AccountType.Team,
            ownerId,
            FixedTime);
        var owner = User.Create($"owner-{Guid.NewGuid():N}@example.com", "Owner", "hashed", FixedTime, true);
        var user = User.Create("invitee@example.com", "Invitee", "hashed", FixedTime, true);
        user.ConfirmEmail(user.Id, FixedTime);
        var workspace = Workspace.Create(
            account.Id, ownerId, "Invitation Concurrency WS", $"inv-conc-{Guid.CreateVersion7():N}", FixedTime);

        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var invitation = WorkspaceInvitation.Create(
            account.Id,
            workspace.Id,
            "invitee@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create(issued.TokenHash),
            issued.HashVersion,
            ownerId,
            FixedTime.AddDays(-1));

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Accounts.Add(account);
        seed.Users.Add(owner);
        seed.Users.Add(user);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceInvitations.Add(invitation);
        await seed.SaveChangesAsync();

        return new InvitationGraph(
            account.Id,
            workspace.Id,
            invitation.Id,
            user.Id,
            ownerId,
            issued.RawToken,
            issued.TokenHash);
    }

    private (ApplicationDbContext Context, AcceptInvitationCommandHandler Handler) CreateAcceptGraph(Guid userId)
    {
        var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        var grantProjection = new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));
        var accountGrantProjection = new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));

        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(userId);
        requestContext.Setup(r => r.IsAuthenticated).Returns(true);

        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(d => d.UtcNow).Returns(FixedTime);

        var handler = new AcceptInvitationCommandHandler(
            context,
            new IdentityUserFactsProvider(context),
            new AccountMembershipActions(context, accountGrantProjection),
            new AccountMembershipFactsProvider(context),
            new OneTimeTokenService(),
            requestContext.Object,
            dateTime.Object,
            grantProjection);

        return (context, handler);
    }

    [Fact]
    public async Task ConcurrentAcceptsOfSameInvitation_DatabaseAllowsExactlyOneMembership()
    {
        var graph = await SeedPendingInvitationAsync();

        var (contextA, handlerA) = CreateAcceptGraph(graph.UserId);
        var (contextB, handlerB) = CreateAcceptGraph(graph.UserId);

        // Both transactions read the invitation as Pending and stage the full
        // accept graph (account membership + workspace member + grant) before
        // either commits, which is the interleaving a real race produces.
        (await handlerA.Handle(new AcceptInvitationCommand(graph.RawToken), CancellationToken.None))
            .Succeeded.Should().BeTrue();
        (await handlerB.Handle(new AcceptInvitationCommand(graph.RawToken), CancellationToken.None))
            .Succeeded.Should().BeTrue();

        await contextA.SaveChangesAsync();

        var secondCommit = async () => await contextB.SaveChangesAsync();
        var thrown = await secondCommit.Should().ThrowAsync<DbUpdateException>(
            "the losing concurrent accept must be rejected rather than persisting a duplicate membership");

        var pg = thrown.Which.InnerException as PostgresException;
        pg.Should().NotBeNull("the rejection must originate from PostgreSQL, not an EF-side validation failure");

        // What is added atomically by the handler fails atomically: the
        // authoritative WorkspaceMember uniqueness index, the workspace grant
        // projection's uniqueness constraint, or the account-membership grant
        // projection's uniqueness constraint may surface first. Which
        // constraint names the conflict is persistence ordering, not contract.
        pg!.ConstraintName.Should().BeOneOf(
            "idx_workspace_members_workspace_user",
            "ux_access_grants_account_workspace_user",
            "ux_access_grants_account_user_account_level",
            "the losing accept must be rejected by a uniqueness constraint owned by the atomic " +
            "accept graph (WorkspaceMember, workspace grant, or account-membership grant) — " +
            "never an unrelated constraint");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.WorkspaceMembers.CountAsync(m =>
            m.WorkspaceId == graph.WorkspaceId && m.UserId == graph.UserId))
            .Should().Be(1, "no duplicate membership may survive the losing accept");
        (await verify.AccessGrants.CountAsync(g =>
            g.WorkspaceId == graph.WorkspaceId && g.UserId == graph.UserId))
            .Should().Be(1, "the grant projection must not duplicate either");
        (await verify.AccountMembers.CountAsync(m =>
            m.AccountId == graph.AccountId && m.UserId == graph.UserId))
            .Should().Be(1, "the account-side membership must not duplicate either");
        (await verify.WorkspaceInvitations.SingleAsync(i => i.Id == graph.InvitationId)).Status
            .Should().Be(WorkspaceInvitationStatus.Accepted,
                "the surviving accept owns the invitation lifecycle");

        await contextA.DisposeAsync();
        await contextB.DisposeAsync();
    }

    [Fact]
    public async Task AcceptRacingRevoke_LeavesInvitationStatusAndMembershipConsistent()
    {
        var graph = await SeedPendingInvitationAsync();

        var (contextAccept, acceptHandler) = CreateAcceptGraph(graph.UserId);

        // The revoker reads the invitation as Pending and stages the lifecycle
        // transition on its own context.
        var revokeContext = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        var revokeInvitation = await revokeContext.WorkspaceInvitations
            .SingleAsync(i => i.Id == graph.InvitationId);
        revokeInvitation.Revoke(graph.OwnerId, FixedTime);

        // The acceptor also reads Pending and stages membership + grant.
        (await acceptHandler.Handle(new AcceptInvitationCommand(graph.RawToken), CancellationToken.None))
            .Succeeded.Should().BeTrue();

        // Accept commits first; the revoker's stale read is now guarded by the
        // aggregate's optimistic concurrency token (every AggregateRoot maps
        // Version as a concurrency token), so the revoke must be rejected
        // rather than silently overwriting the accepted lifecycle status.
        await contextAccept.SaveChangesAsync();

        var losingRevoke = async () => await revokeContext.SaveChangesAsync();
        await losingRevoke.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "a revoke committed against an invitation whose acceptance already committed must be " +
            "rejected by optimistic concurrency instead of overwriting the accepted status");

        await using var verify = _db.CreateContext(SystemTenant());
        var status = (await verify.WorkspaceInvitations.SingleAsync(i => i.Id == graph.InvitationId)).Status;
        status.Should().Be(WorkspaceInvitationStatus.Accepted,
            "the committed acceptance owns the invitation lifecycle status");
        (await verify.WorkspaceMembers.CountAsync(m =>
            m.WorkspaceId == graph.WorkspaceId && m.UserId == graph.UserId))
            .Should().Be(1,
                "the accepted invitation owns exactly one membership — the rejected revoke must " +
                "not leave the invitation status and the granted membership divergent");
        (await verify.AccessGrants.CountAsync(g =>
            g.WorkspaceId == graph.WorkspaceId && g.UserId == graph.UserId))
            .Should().Be(1, "the rejected revoke must not duplicate or drop the grant projection");

        await contextAccept.DisposeAsync();
        await revokeContext.DisposeAsync();
    }

    [Fact]
    public async Task AcceptWithTokenFromSupersededGeneration_IsRejectedAndGrantsNothing()
    {
        var graph = await SeedPendingInvitationAsync();

        var capturingTokens = new CapturingOneTimeTokenService();
        var resendContext = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        var resendRequestContext = new Mock<ICurrentRequestContext>();
        resendRequestContext.Setup(r => r.UserId).Returns(graph.OwnerId);
        resendRequestContext.Setup(r => r.IsAuthenticated).Returns(true);
        var resendClock = new Mock<IDateTimeProvider>();
        resendClock.Setup(d => d.UtcNow).Returns(FixedTime);

        var resendHandler = new ResendInvitationCommandHandler(
            resendContext,
            resendRequestContext.Object,
            capturingTokens,
            new SecretEncryptor(new EphemeralDataProtectionProvider()),
            new IntegrationEventCollector(),
            resendClock.Object);

        var resent = await resendHandler.Handle(
            new ResendInvitationCommand(graph.WorkspaceId, graph.InvitationId),
            CancellationToken.None);
        resent.Succeeded.Should().BeTrue();
        await resendContext.SaveChangesAsync();

        var rotatedRawToken = capturingTokens.LastGeneratedRawToken;
        rotatedRawToken.Should().NotBeNull("resend must issue a fresh raw credential");
        rotatedRawToken.Should().NotBe(graph.RawToken,
            "resend must not reuse the superseded credential");

        var (staleContext, staleHandler) = CreateAcceptGraph(graph.UserId);
        var staleAccept = async () => await staleHandler.Handle(
            new AcceptInvitationCommand(graph.RawToken), CancellationToken.None);

        await staleAccept.Should().ThrowAsync<NotFoundException>(
            "the token from a superseded generation must not resolve to any invitation");
        (await staleContext.WorkspaceMembers.CountAsync(m =>
            m.WorkspaceId == graph.WorkspaceId && m.UserId == graph.UserId))
            .Should().Be(0, "a rejected stale token must grant no membership");

        var (currentContext, currentHandler) = CreateAcceptGraph(graph.UserId);
        (await currentHandler.Handle(new AcceptInvitationCommand(rotatedRawToken!), CancellationToken.None))
            .Succeeded.Should().BeTrue("the current generation credential must still work after rotation");
        await currentContext.SaveChangesAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var invitation = await verify.WorkspaceInvitations.SingleAsync(i => i.Id == graph.InvitationId);
        invitation.TokenGeneration.Should().Be(2, "rotation must advance the token generation");
        invitation.Token.Value.Should().NotBe(graph.TokenHash,
            "the persisted credential must be replaced, not merely re-versioned");
        (await verify.WorkspaceMembers.CountAsync(m =>
            m.WorkspaceId == graph.WorkspaceId && m.UserId == graph.UserId))
            .Should().Be(1, "the current generation acceptance creates exactly one membership");

        await staleContext.DisposeAsync();
        await currentContext.DisposeAsync();
        await resendContext.DisposeAsync();
    }

    private static DomainEventInterceptor CreateOutboxInterceptor()
    {
        return new DomainEventInterceptor(
            new FixedClock(FixedTime),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(
                new ServiceCollection()
                    .AddScoped<IIntegrationEventMapper, WorkspaceEventMapper>()
                    .BuildServiceProvider()),
            new IntegrationEventCollector());
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class CapturingOneTimeTokenService : IOneTimeTokenService
    {
        private readonly OneTimeTokenService _inner = new();

        public string? LastGeneratedRawToken { get; private set; }

        public IssuedOneTimeToken Generate(TokenPurpose purpose)
        {
            var issued = _inner.Generate(purpose);
            LastGeneratedRawToken = issued.RawToken;
            return issued;
        }

        public ParsedOneTimeToken ParseAndHash(string presentedToken, TokenPurpose expectedPurpose)
            => _inner.ParseAndHash(presentedToken, expectedPurpose);
    }
}