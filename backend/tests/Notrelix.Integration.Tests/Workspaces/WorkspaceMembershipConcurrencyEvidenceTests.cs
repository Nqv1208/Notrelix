using Npgsql;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRole;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// WG-TST-CONC-MEM-001 (WG-TEST-GAP-001 / WG-CONC-001, P2B-CERT-002,
/// CERT-CONC-001): the workspace membership uniqueness invariant is enforced
/// at the database, not only by a pre-commit Domain/Application read. Two
/// concurrent add-member transactions that both observe "not a member" before
/// either commits must resolve to exactly one surviving membership — the loser
/// is rejected by a PostgreSQL unique constraint (member row or its grant
/// projection), never by EF-side validation, and no duplicate row survives.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceMembershipConcurrencyEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceMembershipConcurrencyEvidenceTests(PostgresTestContainer db)
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

    private async Task<(Guid AccountId, Guid WorkspaceId, Guid UserId)> SeedWorkspaceAsync()
    {
        var accountId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var owner = User.Create($"wg-race-{Guid.NewGuid():N}@example.com", "WG Race Owner", "hashed", Now, true);
        var workspace = Workspace.Create(accountId, ownerId, "WG Concurrency", $"wg-conc-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        await seed.SaveChangesAsync();

        return (accountId, workspace.Id, ownerId);
    }

    [Fact]
    public async Task ConcurrentDuplicateMembershipAdds_DatabaseAllowsOnlyOneMember()
    {
        var (accountId, workspaceId, _) = await SeedWorkspaceAsync();
        var userId = Guid.CreateVersion7();

        // Both transactions observe "not a member" before either commits; each
        // stages the member row and its authz grant projection just as the
        // production AddMember handler does.
        await using var contextA = _db.CreateContext(SystemTenant());
        await using var contextB = _db.CreateContext(SystemTenant());
        var addedBy = Guid.CreateVersion7();

        StageMembership(contextA, accountId, workspaceId, userId, addedBy);
        StageMembership(contextB, accountId, workspaceId, userId, addedBy);

        await contextA.SaveChangesAsync();

        var secondCommit = async () => await contextB.SaveChangesAsync();
        var thrown = await secondCommit.Should().ThrowAsync<DbUpdateException>(
            "PostgreSQL must reject the stale concurrent duplicate");

        var pg = thrown.Which.InnerException as PostgresException;
        pg.Should().NotBeNull(
            "the rejection must originate from PostgreSQL, not an EF-side save failure");

        // What is added atomically by the handler fails atomically: either the
        // authoritative WorkspaceMember uniqueness index or the workspace grant
        // projection's uniqueness constraint may surface first. Which constraint
        // names the conflict is persistence ordering, not part of the contract.
        pg!.ConstraintName.Should().BeOneOf(
            "idx_workspace_members_workspace_user",
            "ux_access_grants_account_workspace_user",
            "the concurrent duplicate must be rejected by WorkspaceMember uniqueness or " +
            "the workspace grant projection uniqueness — never an unrelated constraint");

        await using var verify = _db.CreateContext(SystemTenant());
        var members = await verify.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .ToListAsync();
        members.Should().ContainSingle(
            "concurrent duplicate adds must never persist two memberships");
        members.Single().Status.Should().Be(WorkspaceMemberStatus.Active);

        var grants = await verify.AccessGrants
            .Where(g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId)
            .ToListAsync();
        grants.Should().ContainSingle(
            "the surviving membership must project exactly one workspace grant");
        grants.Single().RevokedAt.Should().BeNull();
    }

    private static void StageMembership(
        ApplicationDbContext context,
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        Guid addedBy)
    {
        var member = WorkspaceMember.Create(
            accountId, workspaceId, userId, WorkspaceRole.Member, addedBy, Now);
        context.WorkspaceMembers.Add(member);

        var projection = new AccessGrantProjectionService(context);
        projection.SyncWorkspaceMemberGrantAsync(
            accountId, workspaceId, userId, WorkspaceRole.Member, Now, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// WG-TST-CONC-OWNER-001 (WG-TEST-GAP-002 / WG-CONC-001, P2B-CERT-002,
    /// CERT-CONC-001): two concurrent last-owner races — demote one owner while
    /// removing another — must never leave the workspace with zero active owners.
    /// Each transaction runs the production handler path, which takes a
    /// <c>SELECT ... FOR UPDATE</c> row lock on the workspace row that is held
    /// until commit (EfRequestDataSession owns the request transaction). The
    /// second transaction therefore blocks at the workspace read until the first
    /// commits, then re-reads the owner count and fails closed with the Domain
    /// last-owner rule instead of committing a stale count-based demotion/removal.
    /// </summary>
    [Fact]
    public async Task ConcurrentDemoteAndRemoveOwners_NeverLeavesZeroActiveOwners()
    {
        var accountId = Guid.CreateVersion7();
        var ownerAId = Guid.CreateVersion7();
        var ownerBId = Guid.CreateVersion7();
        var workspace = Workspace.Create(accountId, ownerAId, "WG Owner Race", $"wg-owner-{Guid.NewGuid():N}", Now);
        var ownerA = User.Create($"wg-owner-a-{Guid.NewGuid():N}@example.com", "Owner A", "hashed", Now, true);
        var ownerB = User.Create($"wg-owner-b-{Guid.NewGuid():N}@example.com", "Owner B", "hashed", Now, true);
        var memberA = WorkspaceMember.Create(accountId, workspace.Id, ownerAId, WorkspaceRole.Owner, ownerAId, Now);
        var memberB = WorkspaceMember.Create(accountId, workspace.Id, ownerBId, WorkspaceRole.Owner, ownerBId, Now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.AddRange(ownerA, ownerB);
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.AddRange(memberA, memberB);
            await seed.SaveChangesAsync();
        }

        // Each handler runs inside its own explicit transaction, mirroring how
        // EfRequestDataSession wraps the production pipeline. The workspace row
        // lock (SELECT ... FOR UPDATE) is held from the handler's first read until
        // the transaction commits, which is what serializes the two owner-affecting
        // operations.
        await using var contextA = _db.CreateContext(SystemTenant());
        await using var contextB = _db.CreateContext(SystemTenant());
        await using var txA = await contextA.Database.BeginTransactionAsync();
        await using var txB = await contextB.Database.BeginTransactionAsync();

        var handlerA = new UpdateMemberRoleCommandHandler(
            contextA, RequestContext(ownerAId), Clock(), GrantProjection(contextA), new WorkspaceOwnerUpdateLocker(contextA));
        var handlerB = new RemoveMemberCommandHandler(
            contextB, RequestContext(ownerBId), Clock(), GrantProjection(contextB), new WorkspaceOwnerUpdateLocker(contextB));

        // Handler A wins the race: it acquires the workspace row lock, observes
        // two owners, demotes ownerA, and holds the lock until we commit below.
        var demoteA = await handlerA.Handle(
            new UpdateMemberRoleCommand(workspace.Id, ownerAId, WorkspaceRole.Member),
            CancellationToken.None);
        demoteA.Succeeded.Should().BeTrue();

        // Handler B starts concurrently. Its workspace read (FOR UPDATE) must
        // block until transaction A commits, so its later owner count cannot be
        // stale. Run it on a separate task while A is still open.
        var removeBTask = handlerB.Handle(
            new RemoveMemberCommand(workspace.Id, ownerBId),
            CancellationToken.None);

        await contextA.SaveChangesAsync();
        await txA.CommitAsync();

        // With the workspace row locked and now committed (ownerA is a member),
        // handler B's count sees exactly one active owner and must fail closed.
        var removingOwnerB = async () => await removeBTask;
        (await removingOwnerB.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(
                WorkspaceRuleCodes.Workspaces_Owner_CannotRemoveLastOwner,
                "removing the last remaining owner must be rejected after the demote commits");

        var verify = await _db.CreateContext(SystemTenant()).WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active);

        verify.Should().Be(1,
            "the workspace must never end with zero active owners: the losing " +
            "last-owner operation must fail predictably, not commit and silently " +
            "orphan the workspace");
    }

    private static ICurrentRequestContext RequestContext(Guid userId)
    {
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(userId);
        return requestContext.Object;
    }

    private static IDateTimeProvider Clock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        return clock.Object;
    }

    private static IWorkspaceGrantProjectionService GrantProjection(ApplicationDbContext context)
        => new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));
}