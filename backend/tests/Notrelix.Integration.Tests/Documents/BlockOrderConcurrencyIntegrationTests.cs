using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// Block writes that compete on one page, on PostgreSQL through the real pipeline: two writers must never
/// leave two live siblings on one sort key, and a block must never be created under a parent that was
/// deleted at the same moment.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BlockOrderConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BlockOrderConcurrencyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private Task<List<(Guid Id, Guid? ParentId, string Position)>> LiveBlocksAsync(Guid pageId) =>
        _host.ReadAsync(async context => (await context.Blocks
                .Where(b => b.PageId == pageId)
                .Select(b => new { b.Id, b.ParentId, Position = b.Position.Value })
                .ToListAsync())
            .Select(b => (b.Id, b.ParentId, b.Position))
            .ToList());

    [Fact]
    public async Task ParallelAppends_NeverShareASortKey_AndNeverEndInAnUnmappedFailure()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        await DocumentsFlows.BlockAsync(owner, pageId);

        const int writers = 8;
        var providers = Enumerable.Range(0, writers).Select(_ => _host.CreateProvider(stack.AccountId, stack.OwnerId)).ToList();
        try
        {
            var outcomes = await Task.WhenAll(providers.Select(provider => Task.Run(async () =>
            {
                try
                {
                    await DocumentsFlows.BlockAsync(provider, pageId);
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })));

            outcomes.Where(ex => ex is not null).Should().AllSatisfy(ex => ex.Should().BeOfType<ConflictException>("a lost race is a 409, never an unmapped failure"));
            outcomes.Count(ex => ex is null).Should().BeGreaterThan(0, "one of the writers wins");

            var blocks = await LiveBlocksAsync(pageId);
            blocks.Select(b => (b.ParentId, b.Position)).Should().OnlyHaveUniqueItems("two live siblings never share a sort key");
            blocks.Should().HaveCount(1 + outcomes.Count(ex => ex is null));
        }
        finally
        {
            foreach (var provider in providers) provider.Dispose();
        }
    }

    [Fact]
    public async Task TwoSiblingsOnTheSameKey_AreRefusedByTheDatabase_AsAConflict()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        var key = FractionalIndex.Create("a0");
        var content = BlockContent.Create(JsonValue.EmptyObject());

        using var scope = owner.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<IRequestDataSession>();
        var context = scope.ServiceProvider.GetRequiredService<Notrelix.Infrastructure.Data.ApplicationDbContext>();

        var act = () => session.ExecuteAsync(
            new RequestDataSessionOptions(RequestDataAccess.Transactional, ApplyTenantScope: false, ApplyResourceScope: false),
            ct =>
            {
                context.Blocks.Add(Block.CreateRoot(stack.AccountId, stack.WorkspaceId, pageId, BlockType.Text, content, key, stack.OwnerId, OrgTreeTestHost.BaseTime));
                context.Blocks.Add(Block.CreateRoot(stack.AccountId, stack.WorkspaceId, pageId, BlockType.Text, content, key, stack.OwnerId, OrgTreeTestHost.BaseTime));
                return Task.FromResult(0);
            },
            CancellationToken.None);

        var thrown = (await act.Should().ThrowAsync<ConflictException>()).Which;
        thrown.ErrorCode.Should().Be("documents.block.position-conflict");
        (await LiveBlocksAsync(pageId)).Should().BeEmpty("the whole write rolled back");
    }

    [Fact]
    public async Task DeletingAParent_WhileAChildIsCreatedUnderIt_NeverLeavesALiveChildOfADeletedBlock()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        for (var round = 0; round < 6; round++)
        {
            var pageId = await DocumentsFlows.PageAsync(owner, stack, $"Doc {round}");
            var parent = await DocumentsFlows.BlockAsync(owner, pageId);
            using var deleter = _host.CreateProvider(stack.AccountId, stack.OwnerId);
            using var creator = _host.CreateProvider(stack.AccountId, stack.OwnerId);

            var outcomes = await Task.WhenAll(
                Task.Run(async () => await Try(() => OrgTreeTestHost.SendAsync(deleter, new DeleteBlockCommand(pageId, parent.Id, parent.Version)))),
                Task.Run(async () => await Try(() => DocumentsFlows.BlockAsync(creator, pageId, parentBlockId: parent.Id))));

            outcomes.Where(ex => ex is not null).Should().AllSatisfy(ex => ex.Should().Match(e => e is NotFoundException || e is ConflictException || e is PreconditionFailedException));
            var live = await LiveBlocksAsync(pageId);
            live.Where(b => b.ParentId == parent.Id).Should().BeEmpty("a live block never hangs under a deleted parent");
        }
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
