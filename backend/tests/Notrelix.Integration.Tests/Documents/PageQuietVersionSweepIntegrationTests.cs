using Notrelix.Domain.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// The quiet-version sweep on PostgreSQL under the worker scope: a page with no block save for five minutes gets
/// one Automatic entry for its unsnapshotted edits, a page still being edited gets none, and a repeated pass adds nothing.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageQuietVersionSweepIntegrationTests
{
    private readonly OrgTreeTestHost _host;

    public PageQuietVersionSweepIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db);

    private async Task AddBlockAsync(OrgStack stack, Guid pageId, string text, DateTimeOffset savedAt) =>
        await _host.WithSystemContextAsync(seed =>
        {
            seed.Blocks.Add(Block.CreateRoot(
                stack.AccountId, stack.WorkspaceId, pageId, BlockType.Text,
                BlockContent.Create(JsonValue.Create($"{{\"v\":1,\"runs\":[{{\"text\":\"{text}\",\"marks\":[]}}]}}")),
                FractionalIndex.Create("a0"), stack.OwnerId, savedAt));
            return Task.CompletedTask;
        });

    private ServiceProvider ProviderFor(OrgStack stack) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddSingleton<IOptions<PageQuietVersionSweepOptions>>(Options.Create(new PageQuietVersionSweepOptions()));
            services.AddScoped<PageQuietVersionSweeper>();
        });

    private static async Task<int> RunAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PageQuietVersionSweeper>().RunOnceAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Pass_CapturesAQuietPageOnce_AndLeavesAPageThatIsStillBeingEdited()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var quiet = await _host.SeedPageAsync(stack, "Quiet");
        var typing = await _host.SeedPageAsync(stack, "Typing");
        await AddBlockAsync(stack, quiet, "one", DateTimeOffset.UtcNow.AddMinutes(-6));
        await AddBlockAsync(stack, typing, "two", DateTimeOffset.UtcNow.AddMinutes(-1));

        using var provider = ProviderFor(stack);
        (await RunAsync(provider)).Should().Be(1);
        (await RunAsync(provider)).Should().Be(0, "the content now equals the newest entry");

        var versions = await _host.ReadAsync(c => c.DocumentVersions.ToListAsync());
        versions.Should().ContainSingle();
        versions[0].PageId.Should().Be(quiet);
        versions[0].Kind.Should().Be(DocumentVersionKind.Automatic);
        versions[0].CreatedBy.Should().Be(stack.OwnerId);
    }
}
