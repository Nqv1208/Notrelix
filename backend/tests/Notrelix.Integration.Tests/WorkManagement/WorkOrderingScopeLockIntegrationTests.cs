using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// ORDER-LOCK-001 (WM-V3-ORDER-001). The ordering scope lock is a PostgreSQL
/// transaction-scoped advisory lock: a second writer of the same scope cannot
/// proceed until the first transaction ends, writers of different scopes are not
/// serialized, and the lock cannot be taken outside a transaction.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkOrderingScopeLockIntegrationTests
{
    private static readonly TimeSpan BlockedProbe = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(10);

    private readonly PostgresTestContainer _db;

    public WorkOrderingScopeLockIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    [Fact]
    public async Task SameScope_SecondWriterWaitsUntilTheFirstTransactionEnds()
    {
        var scope = new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, Guid.NewGuid());
        await using var first = _db.CreateOwnerContext(SystemTenant());
        await using var second = _db.CreateOwnerContext(SystemTenant());
        await using var firstTx = await first.Database.BeginTransactionAsync();
        await using var secondTx = await second.Database.BeginTransactionAsync();

        await new PostgresWorkOrderingScopeLock(first).AcquireAsync(scope, CancellationToken.None);
        var secondAcquire = new PostgresWorkOrderingScopeLock(second).AcquireAsync(scope, CancellationToken.None);

        (await Task.WhenAny(secondAcquire, Task.Delay(BlockedProbe))).Should().NotBeSameAs(secondAcquire,
            "a second writer of the same scope must wait for the first transaction");

        await firstTx.CommitAsync();

        (await Task.WhenAny(secondAcquire, Task.Delay(ReleaseTimeout))).Should().BeSameAs(secondAcquire,
            "the lock is released when the holding transaction ends");
        await secondTx.CommitAsync();
    }

    [Fact]
    public async Task DifferentScopes_AreNotSerialized()
    {
        await using var first = _db.CreateOwnerContext(SystemTenant());
        await using var second = _db.CreateOwnerContext(SystemTenant());
        await using var firstTx = await first.Database.BeginTransactionAsync();
        await using var secondTx = await second.Database.BeginTransactionAsync();

        await new PostgresWorkOrderingScopeLock(first)
            .AcquireAsync(new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, Guid.NewGuid()), CancellationToken.None);
        var otherScope = new PostgresWorkOrderingScopeLock(second)
            .AcquireAsync(new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, Guid.NewGuid()), CancellationToken.None);

        (await Task.WhenAny(otherScope, Task.Delay(ReleaseTimeout))).Should().BeSameAs(otherScope,
            "a different parent scope must not wait for an unrelated lock");
    }

    [Fact]
    public async Task Acquire_OutsideATransaction_FailsInsteadOfLocking()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var act = () => new PostgresWorkOrderingScopeLock(context)
            .AcquireAsync(new WorkOrderingScope(WorkOrderingScopeKind.BoardGroups, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void LockKey_IsTheStableHashOfScopeKindAndParent()
    {
        var parent = Guid.Parse("00000000-0000-0000-0000-00000000000a");

        PostgresWorkOrderingScopeLock.ComputeKey(new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, parent))
            .Should().Be(4032662981963095738L, "SHA-256(\"group-items:<guid N>\") first 8 bytes big-endian (SPEC WM-V3-DEC-001)");
        PostgresWorkOrderingScopeLock.ComputeKey(new WorkOrderingScope(WorkOrderingScopeKind.BoardGroups, parent))
            .Should().NotBe(4032662981963095738L, "the scope kind is part of the lock identity");
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
