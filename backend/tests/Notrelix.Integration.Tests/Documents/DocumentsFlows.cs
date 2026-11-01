using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>Small Documents flows the integration suites share, all through the real pipeline.</summary>
internal static class DocumentsFlows
{
    private static readonly JsonElement Text = JsonDocument.Parse("""{"v":1,"runs":[{"text":"t","marks":[]}]}""").RootElement.Clone();

    public static Task<Guid> PageAsync(ServiceProvider provider, OrgStack stack, string title, Guid? parentId = null, Guid? spaceId = null) =>
        OrgTreeTestHost.QueryAsync(provider, new CreatePageCommand(stack.WorkspaceId, title, parentId, spaceId));

    public static Task<BlockDto> BlockAsync(ServiceProvider provider, Guid pageId, Guid? afterBlockId = null, Guid? parentBlockId = null) =>
        OrgTreeTestHost.QueryAsync(provider, new CreateBlockCommand(pageId, null, "paragraph", Text, null, afterBlockId, parentBlockId));

    public static Task<long> PageVersionAsync(OrgTreeTestHost host, Guid pageId) =>
        host.ReadAsync(context => context.Pages.IgnoreQueryFilters().Where(p => p.Id == pageId).Select(p => p.Version).SingleAsync());

    public static Task<Guid?> ParentOfAsync(OrgTreeTestHost host, Guid pageId) =>
        host.ReadAsync(context => context.Pages.IgnoreQueryFilters().Where(p => p.Id == pageId).Select(p => p.ParentId).SingleAsync());
}
