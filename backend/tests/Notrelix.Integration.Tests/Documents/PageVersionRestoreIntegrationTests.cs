using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;
using Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;
using Notrelix.Application.Features.Documents.Versions.Queries.ListPageVersions;
using Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;
using Notrelix.Application.Features.Documents.Versions.Commands.RestorePageVersion;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// Restoring a page version on PostgreSQL through the real pipeline: the soft-delete filter hides deleted
/// blocks from a plain query, so a restore has to look past it to bring back a block deleted since the entry.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageVersionRestoreIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public PageVersionRestoreIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RestoringAVersion_BringsBackABlockDeletedSinceThen()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        var a = await DocumentsFlows.BlockAsync(owner, pageId);
        var b = await DocumentsFlows.BlockAsync(owner, pageId, afterBlockId: a.Id);
        var version = await OrgTreeTestHost.QueryAsync(owner, new CreatePageVersionCommand(pageId, "v1"));

        await OrgTreeTestHost.SendAsync(owner, new DeleteBlockCommand(pageId, b.Id, b.Version));
        var live = await OrgTreeTestHost.QueryAsync(owner, new GetPageBlocksQuery(pageId));
        live.Items.Select(x => x.Id).Should().Equal(a.Id);

        var restored = await OrgTreeTestHost.QueryAsync(owner,
            new RestorePageVersionCommand(pageId, version.Id, live.Items.Select(x => new SeenBlockVersion(x.Id, x.Version)).ToList()));

        restored.Blocks.Select(x => x.Id).Should().Equal(a.Id, b.Id);
        var after = await OrgTreeTestHost.QueryAsync(owner, new GetPageBlocksQuery(pageId));
        after.Items.Select(x => x.Id).Should().Equal(new[] { a.Id, b.Id });
    }

    [Fact]
    public async Task ARetryOfSavingANamedVersion_WithTheSameKey_AddsOneEntry_AndAnotherKeyAddsAnother()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        await DocumentsFlows.BlockAsync(owner, pageId);

        var first = Unwrap<PageVersionDto>(await OrgTreeTestHost.SendIdempotentAsync(owner, new CreatePageVersionCommand(pageId, "Before launch"), "save-key-0001"));
        var retry = Unwrap<PageVersionDto>(await OrgTreeTestHost.SendIdempotentAsync(owner, new CreatePageVersionCommand(pageId, "Before launch"), "save-key-0001"));

        retry.Id.Should().Be(first.Id);
        (await EntriesAsync(owner, pageId)).Should().ContainSingle();

        var other = Unwrap<PageVersionDto>(await OrgTreeTestHost.SendIdempotentAsync(owner, new CreatePageVersionCommand(pageId, "Before launch"), "save-key-0002"));

        other.Id.Should().NotBe(first.Id);
        (await EntriesAsync(owner, pageId)).Should().HaveCount(2);
    }

    [Fact]
    public async Task ARetryOfARestore_WithTheSameKey_RestoresOnce_AndAnotherKeyRestoresAgain()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Doc");
        var a = await DocumentsFlows.BlockAsync(owner, pageId);
        var version = await OrgTreeTestHost.QueryAsync(owner, new CreatePageVersionCommand(pageId, "v1"));
        await OrgTreeTestHost.SendAsync(owner, new UpdateBlockCommand(pageId, a.Id, null, JsonDocument.Parse("""{"v":1,"runs":[{"text":"edited","marks":[]}]}""").RootElement.Clone(), null, a.Version));
        var live = await OrgTreeTestHost.QueryAsync(owner, new GetPageBlocksQuery(pageId));
        var seen = live.Items.Select(x => new SeenBlockVersion(x.Id, x.Version)).ToList();

        var first = Unwrap<RestorePageVersionResultDto>(await OrgTreeTestHost.SendIdempotentAsync(owner, new RestorePageVersionCommand(pageId, version.Id, seen), "restore-key-0001"));
        var retry = Unwrap<RestorePageVersionResultDto>(await OrgTreeTestHost.SendIdempotentAsync(owner, new RestorePageVersionCommand(pageId, version.Id, seen), "restore-key-0001"));

        retry.Version.Id.Should().Be(first.Version.Id);
        var entries = await EntriesAsync(owner, pageId);
        entries.Count(e => e.Kind == "restore").Should().Be(1);
        entries.Count(e => e.Name == $"Before restoring version {version.VersionNumber}").Should().Be(1);
        var total = entries.Count;

        var restoredBlocks = await OrgTreeTestHost.QueryAsync(owner, new GetPageBlocksQuery(pageId));
        var seenAgain = restoredBlocks.Items.Select(x => new SeenBlockVersion(x.Id, x.Version)).ToList();
        await OrgTreeTestHost.SendIdempotentAsync(owner, new RestorePageVersionCommand(pageId, version.Id, seenAgain), "restore-key-0002");

        (await EntriesAsync(owner, pageId)).Count.Should().BeGreaterThan(total);
    }

    private static T Unwrap<T>(object? result) => ((Result<T>)result!).Data!;

    private static async Task<IReadOnlyList<PageVersionDto>> EntriesAsync(ServiceProvider provider, Guid pageId) =>
        (await OrgTreeTestHost.QueryAsync(provider, new ListPageVersionsQuery(pageId, null, null))).Items;
}
