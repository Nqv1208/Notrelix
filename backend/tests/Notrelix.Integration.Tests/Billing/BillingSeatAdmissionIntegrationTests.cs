using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Plans;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Billing;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Billing;

/// <summary>
/// Seat admission on the real production gate over PostgreSQL: the Free plan holds ten member seats;
/// at the limit a new person is refused, a person who already holds the seat in another workspace of
/// the account is admitted, and two people admitted at once cannot both take the last seat.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BillingSeatAdmissionIntegrationTests : IAsyncLifetime
{
    private const int FreeMemberSeats = 10;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BillingSeatAdmissionIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        await PlanCatalogSeed.EnsureAsync(seed);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A new person is refused at the member limit; a person who holds the seat in another workspace is admitted")]
    public async Task AtTheLimit_NewPersonRefused_ExistingSeatHolderAdmitted()
    {
        var stack = await SeedAccountAsync(seatedMembers: FreeMemberSeats);

        // The owner also runs a second workspace of the account; a person seated only in the first one asks to join it.
        var now = DateTimeOffset.UtcNow;
        var second = Workspace.Create(stack.AccountId, stack.OwnerId, "Second", $"second-{Guid.NewGuid():N}", now);
        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Workspaces.Add(second);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(stack.AccountId, second.Id, stack.OwnerId, WorkspaceRole.Owner, stack.OwnerId, now));
            await seed.SaveChangesAsync();
        }

        await GrantAsync(stack.AccountId, second.Id, stack.OwnerId, now);

        (await TryAdmitAsync(stack, second.Id, Guid.NewGuid(), insertAfterCheck: false))
            .Should().BeFalse("the account already holds ten member seats");
        (await TryAdmitAsync(stack, second.Id, stack.SeatedMemberId, insertAfterCheck: false))
            .Should().BeTrue("the person already holds a member seat in the account, so no new seat is taken");
    }

    [Fact(DisplayName = "Two people admitted at once cannot both take the last member seat")]
    public async Task TwoConcurrentAdmissions_OneSeatLeft_ExactlyOneIsAdmitted()
    {
        var stack = await SeedAccountAsync(seatedMembers: FreeMemberSeats - 1);

        var outcomes = await Task.WhenAll(
            TryAdmitAsync(stack, stack.WorkspaceId, Guid.NewGuid(), insertAfterCheck: true),
            TryAdmitAsync(stack, stack.WorkspaceId, Guid.NewGuid(), insertAfterCheck: true));

        outcomes.Count(admitted => admitted).Should().Be(1, "only one seat was free");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.WorkspaceMembers.IgnoreQueryFilters()
                .CountAsync(m => m.AccountId == stack.AccountId && m.Status == WorkspaceMemberStatus.Active))
            .Should().Be(FreeMemberSeats, "the account never exceeds its member limit");
    }

    /// <summary>
    /// One request as the account owner: begin a transaction, ask the real gate, and when admitted add the
    /// membership (on its own connection) before the request transaction commits. The pause between the check and
    /// the insert holds the window a racing request would use.
    /// </summary>
    private async Task<bool> TryAdmitAsync(Stack stack, Guid workspaceId, Guid userId, bool insertAfterCheck)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(stack.AccountId, workspaceId, stack.OwnerId);
        await using var context = _db.CreateAppContext(tenant);
        await using var transaction = await context.Database.BeginTransactionAsync();
        await new RlsSessionContext(
            context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant,
            NullLogger<RlsSessionContext>.Instance).ApplyAsync(CancellationToken.None);
        var clock = new DateTimeProvider();
        var facts = new BillingCapabilityFactsProvider(
            context, clock, new BillingUsageReader(context),
            new BillingPlanResolver(context, clock, new BillingPlanCatalogReader(context)));
        var gate = new BillingSeatGate(facts, new BillingUsageReader(context));

        var admitted = await gate.CanAdmitAsync(stack.AccountId, workspaceId, userId, asGuest: false, CancellationToken.None);
        if (admitted && insertAfterCheck)
        {
            await Task.Delay(400);
            var now = DateTimeOffset.UtcNow;
            var user = User.Create($"seat-{Guid.NewGuid():N}@example.com", "Seat", "hashed", now, true);
            await using var write = _db.CreateOwnerContext(SystemTenant());
            write.Users.Add(user);
            write.WorkspaceMembers.Add(WorkspaceMember.Create(stack.AccountId, workspaceId, user.Id, WorkspaceRole.Member, user.Id, now));
            await write.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return admitted;
    }

    private sealed record Stack(Guid AccountId, Guid OwnerId, Guid WorkspaceId, Guid SeatedMemberId);

    private async Task<Stack> SeedAccountAsync(int seatedMembers)
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"seat-owner-{Guid.NewGuid():N}@example.com", "Seat Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var account = Account.Create("Seat Account", $"seat-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "Seat WS", $"seat-ws-{Guid.NewGuid():N}", now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        var seatedMemberId = owner.Id;
        for (var i = 1; i < seatedMembers; i++)
        {
            var member = User.Create($"seat-{i}-{Guid.NewGuid():N}@example.com", $"Seat {i}", "hashed", now, true);
            seed.Users.Add(member);
            seatedMemberId = member.Id;
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
        }

        await seed.SaveChangesAsync();
        await GrantAsync(account.Id, workspace.Id, owner.Id, now);
        return new Stack(account.Id, owner.Id, workspace.Id, seatedMemberId);
    }

    /// <summary>The access grant row the row-level policies read, as the request that added the owner would write it.</summary>
    private async Task GrantAsync(Guid accountId, Guid workspaceId, Guid ownerId, DateTimeOffset now)
    {
        await using var grant = _db.CreateOwnerContext(SystemTenant());
        await new AccessGrantProjectionService(grant).SyncWorkspaceMemberGrantAsync(
            accountId, workspaceId, ownerId, WorkspaceRole.Owner, now, CancellationToken.None);
        await grant.SaveChangesAsync();
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
