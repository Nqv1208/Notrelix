using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>UC-DCT-L04 on PostgreSQL through the real pipeline: a create page repeated with its key makes one page.</summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CreatePageIdempotencyIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public CreatePageIdempotencyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TheSameCreateWithTheSameKey_AnswersTheFirstPage_AndStoresOne()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var key = $"page-{Guid.NewGuid():N}";
        var command = new CreatePageCommand(stack.WorkspaceId, "Kế hoạch", null);

        var first = ((Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(owner, command, key))!).Data;
        var replay = ((Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(owner, command, key))!).Data;

        replay.Should().Be(first, "a retry of one create answers the page it made");
        (await _host.ReadAsync(context => context.Pages.IgnoreQueryFilters().CountAsync(p => p.WorkspaceId == stack.WorkspaceId)))
            .Should().Be(1);
    }

    [Fact]
    public async Task AnotherKey_IsAnotherPage()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var command = new CreatePageCommand(stack.WorkspaceId, "Kế hoạch", null);

        await OrgTreeTestHost.SendIdempotentAsync(owner, command, $"page-{Guid.NewGuid():N}");
        await OrgTreeTestHost.SendIdempotentAsync(owner, command, $"page-{Guid.NewGuid():N}");

        (await _host.ReadAsync(context => context.Pages.IgnoreQueryFilters().CountAsync(p => p.WorkspaceId == stack.WorkspaceId)))
            .Should().Be(2);
    }
}
