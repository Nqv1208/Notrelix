using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// The purge of deleted Spaces on PostgreSQL, as the worker login with the RLS policy applied: a Space deleted more
/// than 30 days ago is removed with its member and closure rows, one inside the window, a live one and
/// another workspace's live one stay, and a rerun changes nothing.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class DeletedSpacePurgeIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset PurgeTime = OrgTreeTestHost.BaseTime.AddDays(32);

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public DeletedSpacePurgeIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Purge_RemovesASpaceDeletedMoreThanThirtyDaysAgo_WithItsRows_AndKeepsTheRest()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var expired = await SeedAsync(stack, "Expired", OrgTreeTestHost.BaseTime.AddDays(1)); // 31 days before PurgeTime
        var recent = await SeedAsync(stack, "Recent", OrgTreeTestHost.BaseTime.AddDays(3)); // 29 days
        var live = await SeedAsync(stack, "Live", null);
        var otherLive = await SeedAsync(other, "Other live", null);

        (await PurgeAsync()).Should().Be(1);

        (await CountAsync(expired)).Should().Be(new SpaceRows(0, 0, 0));
        (await CountAsync(recent)).Should().Be(new SpaceRows(1, 1, 1));
        (await CountAsync(live)).Should().Be(new SpaceRows(1, 1, 1));
        (await CountAsync(otherLive)).Should().Be(new SpaceRows(1, 1, 1));
        (await _host.ReadAsync(c => c.EnterpriseAuditLogs
                .CountAsync(a => a.Action == "PurgeSpace" && a.ResourceId == expired && a.WorkspaceId == stack.WorkspaceId)))
            .Should().Be(1);

        (await PurgeAsync()).Should().Be(0, "a purged Space is gone, so a second pass changes nothing");
    }

    [Fact]
    public async Task Purge_RemovesAnExpiredChildAndItsExpiredParentInOnePass()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var parent = await SeedAsync(stack, "Parent", OrgTreeTestHost.BaseTime.AddDays(1));
        var child = await SeedAsync(stack, "Child", OrgTreeTestHost.BaseTime, parentId: parent);

        (await PurgeAsync()).Should().Be(2);
        (await CountAsync(parent)).Should().Be(new SpaceRows(0, 0, 0));
        (await CountAsync(child)).Should().Be(new SpaceRows(0, 0, 0));
    }

    [Fact]
    public void Options_AreValidated()
    {
        var validator = new DeletedSpacePurgeOptionsValidator();
        validator.Validate(null, new DeletedSpacePurgeOptions()).Succeeded.Should().BeTrue();
        validator.Validate(null, new DeletedSpacePurgeOptions { Interval = TimeSpan.FromSeconds(1) }).Failed.Should().BeTrue();
        validator.Validate(null, new DeletedSpacePurgeOptions { BatchSize = 0 }).Failed.Should().BeTrue();
    }

    private Task<Guid> SeedAsync(OrgStack stack, string name, DateTimeOffset? deletedAt, Guid? parentId = null) =>
        _host.SeedSpaceAsync(stack, name, SpaceVisibility.Workspace, stack.OwnerId, parentId,
            transition: deletedAt is { } at
                ? s => s.Delete(stack.OwnerId, at, SpaceContentFact.Empty)
                : null);

    private Task<SpaceRows> CountAsync(Guid space) =>
        _host.ReadAsync(async c => new SpaceRows(
            await c.Spaces.IgnoreQueryFilters().CountAsync(s => s.Id == space),
            await c.SpaceMembers.IgnoreQueryFilters().CountAsync(m => m.SpaceId == space),
            await c.SpacePaths.CountAsync(p => p.DescendantId == space || p.AncestorId == space)));

    private async Task<int> PurgeAsync()
    {
        var tenant = new CurrentTenantContext();
        await using var context = PostgresTestContainer.CreateContextFor(_db.WorkerConnectionString, tenant);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(PurgeTime);
        var purger = new DeletedSpacePurger(
            context, tenant, rls, clock.Object,
            Options.Create(new DeletedSpacePurgeOptions { BatchSize = 10 }),
            NullLogger<DeletedSpacePurger>.Instance);
        return await purger.RunOnceAsync(default);
    }

    private sealed record SpaceRows(int Spaces, int Members, int Paths);
}
