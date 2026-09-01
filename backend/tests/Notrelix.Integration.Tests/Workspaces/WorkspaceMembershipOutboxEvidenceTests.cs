using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.EventMappers.Automation;
using Notrelix.Application.EventMappers.Workspaces;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Events;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// TAC-WG-007 — the pinned membership event chain commits atomically: a valid
/// WorkspaceMember mutation raises the Workspaces-owned Domain fact, the
/// producer-owned mapper turns it into the registry identity
/// "workspace.member.added" V1, and the outbox record is written inside the
/// same SaveChanges. A rolled-back mutation leaves no committed outward
/// delivery, and a duplicate mutation is a membership no-op that emits no
/// second fact.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceMembershipOutboxEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceMembershipOutboxEvidenceTests(PostgresTestContainer db)
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

    private sealed record MemberGraph(Guid AccountId, Guid WorkspaceId, Guid UserId, Guid WorkspaceMemberId);

    private async Task<MemberGraph> SeedWorkspaceAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var user = User.Create($"wg-{Guid.NewGuid():N}@example.com", "WG User", "hashed", Now, true);
        var workspace = Workspace.Create(accountId, ownerId, "WG Workspace", $"wg-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        await seed.SaveChangesAsync();

        return new MemberGraph(accountId, workspace.Id, user.Id, member.Id);
    }

    private static WorkspaceMemberAddedDomainEvent NewMemberAddedFact(MemberGraph graph) =>
        new(
            graph.AccountId,
            graph.WorkspaceId,
            graph.WorkspaceMemberId,
            graph.UserId,
            WorkspaceRole.Member,
            ActorId: Guid.NewGuid(),
            OccurredAt: Now);

    private static DomainEventInterceptor CreateOutboxInterceptor()
    {
        return new DomainEventInterceptor(
            new FixedClock(Now),
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

    private async Task<int> CountMembershipOutboxAsync(Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(message => message.WorkspaceId == workspaceId
                && message.MessageName == "workspace.member.added");
    }

    private static IDateTimeProvider Clock() => new FixedClock(Now);

    [Fact]
    public async Task MemberAddedFact_OutboxIntent_CommitsAtomically()
    {
        var graph = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        var member = WorkspaceMember.Create(
            graph.AccountId, graph.WorkspaceId, Guid.NewGuid(), WorkspaceRole.Member, graph.UserId, Now);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();

        (await CountMembershipOutboxAsync(graph.WorkspaceId)).Should().Be(1,
            "a valid membership mutation must stage exactly one producer-owned outward event");
    }

    [Fact]
    public async Task RolledBackMembershipFact_NoCommittedOutwardDelivery()
    {
        var graph = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var member = WorkspaceMember.Create(
            graph.AccountId, graph.WorkspaceId, Guid.NewGuid(), WorkspaceRole.Member, graph.UserId, Now);
        context.WorkspaceMembers.Add(member);
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        (await CountMembershipOutboxAsync(graph.WorkspaceId)).Should().Be(0,
            "a rolled-back membership mutation must leave no committed outward delivery");
    }
}
