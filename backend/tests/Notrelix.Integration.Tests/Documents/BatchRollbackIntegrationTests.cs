using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// A batch is all or nothing on PostgreSQL through the real pipeline: when a later operation of the request fails,
/// the earlier operations of the same request are not stored either (the request's transaction rolls back).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BatchRollbackIntegrationTests : IAsyncLifetime
{
    private static readonly JsonElement Text = JsonDocument.Parse("""{"v":1,"runs":[{"text":"new","marks":[]}]}""").RootElement.Clone();

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BatchRollbackIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ABatchWithAStaleLaterEdit_StoresNeitherOperation()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        var existing = await DocumentsFlows.BlockAsync(owner, pageId);
        var created = Guid.CreateVersion7();

        var act = () => SendAsync(owner, new BatchUpdateBlocksCommand(pageId,
        [
            new BatchUpdateBlockItem("upsert", created, "paragraph", Text, null, null, existing.Id, null, null),
            new BatchUpdateBlockItem("upsert", existing.Id, null, Text, null, null, null, null, existing.Version + 40),
        ]));

        await act.Should().ThrowAsync<PreconditionFailedException>();
        (await LiveBlockIdsAsync(pageId)).Should().BeEquivalentTo([existing.Id], "the create ahead of the stale edit rolled back with it");
        (await BlockVersionAsync(existing.Id)).Should().Be(existing.Version, "the refused edit did not advance the block");
    }

    [Fact]
    public async Task ABatchWithAnUnknownDeleteAfterACreate_StoresNeitherOperation()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        var existing = await DocumentsFlows.BlockAsync(owner, pageId);

        var act = () => SendAsync(owner, new BatchUpdateBlocksCommand(pageId,
        [
            new BatchUpdateBlockItem("upsert", Guid.CreateVersion7(), "paragraph", Text, null, null, existing.Id, null, null),
            new BatchUpdateBlockItem("delete", Guid.CreateVersion7(), null, null, null, null, null, null, 1),
        ]));

        await act.Should().ThrowAsync<NotFoundException>();
        (await LiveBlockIdsAsync(pageId)).Should().BeEquivalentTo([existing.Id]);
    }

    /// <summary>Sends through the pipeline and lets the failure reach the caller (the helpers in the host fail the test instead).</summary>
    private static async Task SendAsync(ServiceProvider provider, object request)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private Task<List<Guid>> LiveBlockIdsAsync(Guid pageId) =>
        _host.ReadAsync(context => context.Blocks.Where(b => b.PageId == pageId).Select(b => b.Id).ToListAsync());

    private Task<long> BlockVersionAsync(Guid blockId) =>
        _host.ReadAsync(context => context.Blocks.IgnoreQueryFilters().Where(b => b.Id == blockId).Select(b => b.Version).SingleAsync());
}
