using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Commands.DuplicatePage;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>Duplicate on PostgreSQL through the real pipeline: one transaction, one copy per key, no subpages.</summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class DuplicatePageIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public DuplicatePageIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TheCopy_HasNewBlocks_NoSubpages_AndOneCopyPerKey()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var source = await DocumentsFlows.PageAsync(owner, stack, "Plan");
        var first = await DocumentsFlows.BlockAsync(owner, source);
        await DocumentsFlows.BlockAsync(owner, source, first.Id);
        var child = await DocumentsFlows.PageAsync(owner, stack, "Child", source);

        var key = $"dup-{Guid.NewGuid():N}";
        var command = new DuplicatePageCommand(stack.WorkspaceId, source, null);
        var copy = ((Notrelix.Application.Common.Models.Result<DuplicatedPageDto>)(await OrgTreeTestHost.SendIdempotentAsync(owner, command, key))!).Data!;
        var replay = ((Notrelix.Application.Common.Models.Result<DuplicatedPageDto>)(await OrgTreeTestHost.SendIdempotentAsync(owner, command, key))!).Data!;

        replay.PageId.Should().Be(copy.PageId, "a retry with the same key returns the same copy");
        var (pages, blockIds, title, parent) = await _host.ReadAsync(async context => (
            await context.Pages.IgnoreQueryFilters().Where(p => p.WorkspaceId == stack.WorkspaceId).Select(p => p.Id).ToListAsync(),
            await context.Blocks.IgnoreQueryFilters().Where(b => b.PageId == copy.PageId).Select(b => b.Id).ToListAsync(),
            await context.Pages.IgnoreQueryFilters().Where(p => p.Id == copy.PageId).Select(p => p.Title).SingleAsync(),
            await context.Pages.IgnoreQueryFilters().Where(p => p.Id == copy.PageId).Select(p => p.ParentId).SingleAsync()));
        pages.Should().HaveCount(3, "the source, its subpage and one copy; the subpage is not duplicated");
        pages.Should().Contain([source, child, copy.PageId]);
        blockIds.Should().HaveCount(2);
        title.Should().Be("Copy of Plan");
        parent.Should().BeNull();
    }

    [Fact]
    public async Task APersonWhoCannotReadTheSource_GetsNotFound_AndNothingIsCreated()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var source = await DocumentsFlows.PageAsync(owner, stack, "Private plan");
        var outsider = await _host.AddOutsiderAsync(stack);
        using var provider = _host.CreateProvider(stack.AccountId, outsider);

        var act = () => OrgTreeTestHost.SendIdempotentAsync(provider, new DuplicatePageCommand(stack.WorkspaceId, source, null));

        await act.Should().ThrowAsync<AppNotFound>();
        (await _host.ReadAsync(context => context.Pages.IgnoreQueryFilters().CountAsync(p => p.WorkspaceId == stack.WorkspaceId))).Should().Be(1);
    }

    [Fact]
    public async Task AGuest_WhoCannotCreateAPageThere_IsRefused_AndNothingIsCreated()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var source = await DocumentsFlows.PageAsync(owner, stack, "Plan");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        await _host.SeedResourcePermissionAsync(stack, "documents.page", source,
            Notrelix.Domain.Governance.Permissions.PermissionSubjectType.User, guest, Notrelix.Domain.Governance.Permissions.PermissionLevel.Viewer);
        using var provider = _host.CreateProvider(stack.AccountId, guest);

        var act = () => OrgTreeTestHost.SendIdempotentAsync(provider, new DuplicatePageCommand(stack.WorkspaceId, source, null));

        await act.Should().ThrowAsync<AppForbidden>();
        (await _host.ReadAsync(context => context.Pages.IgnoreQueryFilters().CountAsync(p => p.WorkspaceId == stack.WorkspaceId))).Should().Be(1);
    }
}
