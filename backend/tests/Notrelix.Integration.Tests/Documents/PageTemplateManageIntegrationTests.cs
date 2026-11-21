using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Templates.Commands.ArchivePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.CreatePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.DeletePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.UpdatePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Queries.ListPageTemplates;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// Renaming, archiving and deleting a page template on PostgreSQL through the real pipeline: the creator or a
/// Workspace Admin may; another member is refused; a stale version is 412 and changes nothing.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageTemplateManageIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public PageTemplateManageIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<PageTemplateDto> SaveAsync(ServiceProvider provider, OrgStack stack, Guid pageId, string name) =>
        ((Result<PageTemplateDto>)(await OrgTreeTestHost.SendIdempotentAsync(provider, new CreatePageTemplateCommand(stack.WorkspaceId, pageId, name, null)))!).Data!;

    private static async Task<List<PageTemplateDto>> ListAsync(ServiceProvider provider, OrgStack stack) =>
        await OrgTreeTestHost.QueryAsync(provider, new ListPageTemplatesQuery(stack.WorkspaceId));

    [Fact]
    public async Task TheCreatorRenames_AStaleVersionIs412_AndAnotherMemberIsRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var memberId = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var member = _host.CreateProvider(stack.AccountId, memberId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Weekly sync");
        var template = await SaveAsync(owner, stack, pageId, "Weekly");

        var renamed = await OrgTreeTestHost.QueryAsync(owner, new UpdatePageTemplateCommand(stack.WorkspaceId, template.Id, "Weekly plan", "Mondays", template.Version));

        renamed.Name.Should().Be("Weekly plan");
        renamed.Version.Should().Be(template.Version + 1);
        var stale = () => OrgTreeTestHost.SendAsync(owner, new UpdatePageTemplateCommand(stack.WorkspaceId, template.Id, "Lost update", null, template.Version));
        await stale.Should().ThrowAsync<PreconditionFailedException>();
        var refused = () => OrgTreeTestHost.SendAsync(member, new DeletePageTemplateCommand(stack.WorkspaceId, template.Id, renamed.Version));
        await refused.Should().ThrowAsync<ForbiddenException>();
        (await ListAsync(member, stack)).Should().ContainSingle().Which.Should().Match<PageTemplateDto>(t => t.Name == "Weekly plan" && !t.CanManage);
    }

    [Fact]
    public async Task AnAdminArchivesAndDeletes_AndThePageMadeFromTheTemplateIsUnchanged()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var adminId = await _host.AddMemberAsync(stack, WorkspaceRole.Admin);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var admin = _host.CreateProvider(stack.AccountId, adminId);
        var pageId = await DocumentsFlows.PageAsync(owner, stack, "Plan");
        var keep = await SaveAsync(owner, stack, pageId, "Keep");
        var drop = await SaveAsync(owner, stack, pageId, "Drop");

        await OrgTreeTestHost.SendAsync(admin, new ArchivePageTemplateCommand(stack.WorkspaceId, keep.Id, keep.Version));
        await OrgTreeTestHost.SendAsync(admin, new DeletePageTemplateCommand(stack.WorkspaceId, drop.Id, drop.Version));

        (await ListAsync(owner, stack)).Should().BeEmpty();
        (await _host.ReadAsync(c => c.Pages.IgnoreQueryFilters().Where(p => p.Id == pageId).Select(p => p.Title).SingleAsync())).Should().Be("Plan");
        var remaining = await _host.ReadAsync(c => c.PageTemplates.IgnoreQueryFilters().Where(t => t.WorkspaceId == stack.WorkspaceId).Select(t => t.Id).ToListAsync());
        remaining.Should().Equal(keep.Id);
    }
}
