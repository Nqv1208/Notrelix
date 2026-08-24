using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;
using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.Data;

/// <summary>
/// IA-TST-EV-UNIT — fail-closed binding semantics of the data-session
/// expected-version primitive (file 02 §7/§9.2), exercised against the change
/// tracker without provider-specific transaction behavior.
/// </summary>
public sealed class EfRequestDataSessionExpectedVersionTests : IDisposable
{
    private readonly ApplicationDbContext _context = CreateContext();

    [Fact]
    public async Task ConstraintAbsent_NoBindingWork()
    {
        var session = CreateSession();
        var response = await session.ExecuteAsync(
            Options(constraint: null),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        response.Should().Be("ok");
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task RequestTypeMissingFromTargetMap_FailsClosed()
    {
        var workspace = TrackWorkspace(Guid.NewGuid());
        var constraint = new ExpectedVersionConstraint(
            typeof(ConstraintAbsentProbe),
            ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), workspace.Id),
            Value: 1);

        var act = () => RunAsync(constraint);

        var assertion = await act.Should().ThrowAsync<SecurityMisconfigurationException>();
        assertion.Which.Message.Should().Contain(nameof(ConstraintAbsentProbe));
    }

    [Fact]
    public async Task DeclaredKindDiffersFromMappedKind_FailsClosed()
    {
        var workspace = TrackWorkspace(Guid.NewGuid());
        var constraint = new ExpectedVersionConstraint(
            typeof(UpdateWorkspaceProfileCommand),
            ResourceRef.Create(ResourceKind.Create("work-management.board"), workspace.Id),
            Value: 1);

        var act = () => RunAsync(constraint);

        (await act.Should().ThrowAsync<SecurityMisconfigurationException>())
            .Which.Message.Should().Contain("work-management.board");
    }

    [Fact]
    public async Task MappedTargetNotTracked_FailsClosed()
    {
        var untrackedId = Guid.NewGuid();
        var constraint = ConstraintFor<UpdateWorkspaceProfileCommand>(untrackedId, 1);

        var act = () => RunAsync(constraint);

        (await act.Should().ThrowAsync<SecurityMisconfigurationException>())
            .Which.Message.Should().Contain(untrackedId.ToString());
    }

    [Fact]
    public async Task SameGuidTrackedUnderDifferentAggregateKind_FailsClosed()
    {
        var sharedId = Guid.NewGuid();
        // A Board tracked under the same Guid does NOT satisfy a Workspace target.
        _context.Boards.Add(Board.Create(sharedId, Guid.NewGuid(), Guid.NewGuid(), "Board", null, DateTimeOffset.UtcNow));
        _context.ChangeTracker.DetectChanges();

        var constraint = ConstraintFor<UpdateWorkspaceProfileCommand>(sharedId, 1);
        var act = () => RunAsync(constraint);

        (await act.Should().ThrowAsync<SecurityMisconfigurationException>())
            .Which.Message.Should().Contain(typeof(Workspace).Name);
    }

    [Fact]
    public async Task MoreThanOneMatchingTarget_EFIdentityMapPreventsDuplicateTracking()
    {
        var sharedId = Guid.NewGuid();
        var first = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "A", $"a-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        var second = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "B", $"b-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        SetId(first, sharedId);
        SetId(second, sharedId);
        _context.Workspaces.Add(first);
        _context.ChangeTracker.DetectChanges();

        var attachSecond = () =>
        {
            _context.Workspaces.Attach(second);
            _context.ChangeTracker.DetectChanges();
        };

        attachSecond.Should().Throw<InvalidOperationException>(
            "the change tracker must never hold two aggregates with one identity; " +
            "the >1-tracked-targets misconfiguration branch is therefore structurally unreachable");
    }

    [Fact]
    public async Task ExactMappedTarget_BindingAcceptedByStore()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "W", $"w-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        var entry = _context.ChangeTracker.Entries<Workspace>().Single();
        entry.State = EntityState.Unchanged;
        var constraint = ConstraintFor<UpdateWorkspaceProfileCommand>(workspace.Id, workspace.Version);

        await RunAsync(constraint);

        // No precondition failure: the bound original value was accepted by the store.
        entry.State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task UnchangedExactTarget_IssuesGuardedUpdate()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "W", $"w-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        var entry = _context.ChangeTracker.Entries<Workspace>().Single();
        entry.State = EntityState.Unchanged;
        var constraint = ConstraintFor<UpdateWorkspaceProfileCommand>(workspace.Id, workspace.Version);

        await RunAsync(constraint);

        // The forced-modified version guard was consumed by SaveChanges without a
        // precondition failure — a domain no-op still issued its version-guarded update.
        entry.State.Should().Be(EntityState.Unchanged);
        entry.Property(nameof(AggregateRoot.Version)).IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task StaleVersionAtDatabase_SurfacesPreconditionFailure()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "W", $"w-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        var entry = _context.ChangeTracker.Entries<Workspace>().Single();
        entry.State = EntityState.Unchanged;
        var staleConstraint = ConstraintFor<UpdateWorkspaceProfileCommand>(workspace.Id, workspace.Version + 5);

        var act = () => RunAsync(staleConstraint);

        (await act.Should().ThrowAsync<PreconditionFailedException>())
            .Which.ErrorCode.Should().Be("common.precondition-failed");
    }

    // --- helpers -----------------------------------------------------------

    private sealed record ConstraintAbsentProbe : Notrelix.Application.Common.Requests.IExpectedVersionRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), Guid.NewGuid());
        public long ExpectedVersion => 1;
    }

    private EfRequestDataSession CreateSession() =>
        new(_context, Mock.Of<IRlsSessionContext>(), Mock.Of<ILogger<EfRequestDataSession>>());

    private RequestDataSessionOptions Options(ExpectedVersionConstraint? constraint) =>
        new(RequestDataAccess.Transactional,
            ApplyTenantScope: false,
            ApplyResourceScope: false,
            ExpectedVersion: constraint);

    private static ExpectedVersionConstraint ConstraintFor<TRequest>(Guid resourceId, long Value)
        where TRequest : notnull =>
        new(typeof(TRequest),
            ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), resourceId),
            Value);

    private async Task<string> RunAsync(ExpectedVersionConstraint? constraint) =>
        await CreateSession().ExecuteAsync(
            Options(constraint),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

    private Workspace TrackWorkspace(Guid id)
    {
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "W", $"w-{id:N}", DateTimeOffset.UtcNow);
        SetId(workspace, id);
        _context.Workspaces.Add(workspace);
        _context.ChangeTracker.DetectChanges();
        return workspace;
    }

    private static void SetId(AggregateRoot aggregate, Guid id) =>
        aggregate.GetType()
            .GetProperty(nameof(AggregateRoot.Id),
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!
            .SetValue(aggregate, id);

    private static ApplicationDbContext CreateContext() =>
        new InMemoryApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    /// <summary>InMemory cannot map Npgsql-native search types; binding tests do not exercise them.</summary>
    private sealed class InMemoryApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }

    public void Dispose() => _context.Dispose();
}
