using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// The shape of the page tree on PostgreSQL through the real pipeline: moves that race, ancestors that are
/// deleted, and Spaces that are archived.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageTreeShapeIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public PageTreeShapeIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Two moves that race ───────────────────────────────────────────────

    /// <summary>Holds each request that reached its ancestry read until the other one has too (or a timeout).</summary>
    public sealed class Rendezvous
    {
        private readonly TaskCompletionSource _both = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public async Task ArriveAsync()
        {
            if (Interlocked.Increment(ref _arrived) >= 2) _both.TrySetResult();
            await Task.WhenAny(_both.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        }
    }

    public class HoldAfterAncestryRead : DispatchProxy
    {
        public IPageSubtreeStore Inner { get; set; } = null!;
        public Rendezvous Rendezvous { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            try
            {
                var result = targetMethod!.Invoke(Inner, args);
                return targetMethod.Name == nameof(IPageSubtreeStore.LoadAncestryAsync)
                    ? HoldAsync((Task<IReadOnlyDictionary<Guid, Guid?>>)result!)
                    : result;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        private async Task<IReadOnlyDictionary<Guid, Guid?>> HoldAsync(Task<IReadOnlyDictionary<Guid, Guid?>> read)
        {
            var ancestry = await read;
            await Rendezvous.ArriveAsync();
            return ancestry;
        }
    }

    private ServiceProvider ProviderHolding(OrgStack stack, Rendezvous rendezvous) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
            services.AddScoped(sp =>
            {
                var proxy = DispatchProxy.Create<IPageSubtreeStore, HoldAfterAncestryRead>();
                var hold = (HoldAfterAncestryRead)(object)proxy;
                hold.Inner = ActivatorUtilities.CreateInstance<Notrelix.Infrastructure.Documents.Pages.PageSubtreeStore>(sp);
                hold.Rendezvous = rendezvous;
                return proxy;
            }));

    [Fact]
    public async Task TwoMovesThatWouldFormACycle_OnlyOneSucceeds()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var a = await DocumentsFlows.PageAsync(owner, stack, "A");
        var b = await DocumentsFlows.PageAsync(owner, stack, "B");
        var versionA = await DocumentsFlows.PageVersionAsync(_host, a);
        var versionB = await DocumentsFlows.PageVersionAsync(_host, b);

        var rendezvous = new Rendezvous();
        using var first = ProviderHolding(stack, rendezvous);
        using var second = ProviderHolding(stack, rendezvous);
        var outcomes = await Task.WhenAll(
            Task.Run(async () => await Try(() => OrgTreeTestHost.SendAsync(first, new MovePageCommand(a, b, versionA)))),
            Task.Run(async () => await Try(() => OrgTreeTestHost.SendAsync(second, new MovePageCommand(b, a, versionB)))));

        outcomes.Count(ex => ex is null).Should().Be(1, "the second move reads the tree the first one left and is refused");
        outcomes.Single(ex => ex is not null).Should().BeOfType<DomainBusinessRuleException>()
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_PageTree_MoveWouldCreateCycle);
        var parentOfA = await DocumentsFlows.ParentOfAsync(_host, a);
        var parentOfB = await DocumentsFlows.ParentOfAsync(_host, b);
        (parentOfA == b && parentOfB == a).Should().BeFalse("the tree stays acyclic");
    }

    // ── Ancestors that are deleted ────────────────────────────────────────

    [Fact]
    public async Task AMoveUnderADescendantBehindADeletedPage_IsACycle()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var p = await _host.SeedPageAsync(stack, "P");
        var d = await _host.SeedPageAsync(stack, "D", p, page => page.Delete(stack.OwnerId, OrgTreeTestHost.BaseTime));
        var x = await _host.SeedPageAsync(stack, "X", d);

        var version = await DocumentsFlows.PageVersionAsync(_host, p);
        var act = () => OrgTreeTestHost.SendAsync(owner, new MovePageCommand(p, x, version));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode
            .Should().Be(DocumentRuleCodes.Documents_PageTree_MoveWouldCreateCycle);
    }

    [Fact]
    public async Task ADeletedAncestor_StillCountsTowardsTheNestingLimit()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        Guid? parent = null;
        for (var level = 1; level <= 10; level++)
        {
            var deleted = level == 5;
            parent = await _host.SeedPageAsync(stack, $"L{level}", parent, deleted ? page => page.Delete(stack.OwnerId, OrgTreeTestHost.BaseTime) : null);
        }

        var act = () => OrgTreeTestHost.QueryAsync(owner, new CreatePageCommand(stack.WorkspaceId, "too deep", parent));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode
            .Should().Be(DocumentRuleCodes.Documents_PageTree_MaxDepthExceeded);
    }

    // ── The nesting limit holds for a move inside one Space ───────────────

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task MovingASubtreeUnderADeepParent_KeepsTheNestingLimit_CountingTrashedPages(int subtreeLevels, bool allowed)
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        Guid? deep = null;
        for (var level = 1; level <= 8; level++) deep = await _host.SeedPageAsync(stack, $"T{level}", deep);
        var root = await _host.SeedPageAsync(stack, "S");
        var below = root;
        for (var level = 2; level <= subtreeLevels; level++)
        {
            // The deepest page of the subtree is in the trash: it keeps its place and still counts.
            var trashed = level == subtreeLevels;
            below = await _host.SeedPageAsync(stack, $"S{level}", below, trashed ? page => page.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime) : null);
        }

        var version = await DocumentsFlows.PageVersionAsync(_host, root);
        var act = () => OrgTreeTestHost.SendAsync(owner, new MovePageCommand(root, deep, version));

        if (allowed)
        {
            await act();
            (await DocumentsFlows.ParentOfAsync(_host, root)).Should().Be(deep);
        }
        else
        {
            (await act.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode
                .Should().Be(DocumentRuleCodes.Documents_PageTree_MaxDepthExceeded);
            (await DocumentsFlows.ParentOfAsync(_host, root)).Should().BeNull();
        }
    }

    // ── An archived Space takes no page ───────────────────────────────────

    [Fact]
    public async Task MovingAPageUnderAParentInAnArchivedSpace_IsRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var old = await _host.SeedSpaceAsync(stack, "Old", SpaceVisibility.Private, stack.OwnerId,
            transition: space => space.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime, SpaceContentFact.Empty));
        var parent = await _host.SeedPageAsync(stack, "Q", null, page => page.PlaceInSpace(old, stack.OwnerId, OrgTreeTestHost.BaseTime));
        var moving = await DocumentsFlows.PageAsync(owner, stack, "P");

        var version = await DocumentsFlows.PageVersionAsync(_host, moving);
        var act = () => OrgTreeTestHost.SendAsync(owner, new MovePageCommand(moving, parent, version));

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(PageSpacePlacement.InactiveSpaceErrorCode);
        (await DocumentsFlows.ParentOfAsync(_host, moving)).Should().BeNull();
    }

    private static async Task<Exception?> Try(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
