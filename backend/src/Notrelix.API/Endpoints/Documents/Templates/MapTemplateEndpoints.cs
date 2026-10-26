using Notrelix.API.Contracts.Documents.Templates.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Templates.Commands.CreatePageFromTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.CreatePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Queries.ListPageTemplates;

namespace Notrelix.API.Endpoints.Documents.Templates;

public static class MapTemplateEndpoints
{
    public static IEndpointRouteBuilder AddTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/page-templates")
            .WithTags("Documents.Templates")
            .WithOpenApi();

        group.MapWorkspaceGet("/", ListAsync)
            .WithName("Documents.Templates.ListPageTemplates")
            .WithSummary("List the page templates of a workspace")
            .Produces<List<PageTemplateDto>>(StatusCodes.Status200OK, "application/json");

        group.MapWorkspacePost("/", CreateAsync)
            .WithName("Documents.Templates.CreatePageTemplate")
            .WithSummary("Save a page as a workspace template")
            .Produces<PageTemplateDto>(StatusCodes.Status201Created, "application/json");

        group.MapWorkspacePost("/{templateId:guid}/pages", CreatePageAsync)
            .WithName("Documents.Templates.CreatePageFromTemplate")
            .WithSummary("Create a page from a template")
            .Produces<PageFromTemplateDto>(StatusCodes.Status201Created, "application/json");

        return app;
    }

    private static async Task<IResult> ListAsync(Guid workspaceId, ISender sender)
        => (await sender.Send(new ListPageTemplatesQuery(workspaceId))).ToApiResult();

    private static async Task<IResult> CreateAsync(Guid workspaceId, CreatePageTemplateRequest body, ISender sender)
    {
        var result = await sender.Send(new CreatePageTemplateCommand(workspaceId, body.PageId, body.Name, body.Description));
        return result.ToCreatedResult(result.Succeeded ? $"/api/v1/workspaces/{workspaceId}/page-templates/{result.Data!.Id}" : null);
    }

    private static async Task<IResult> CreatePageAsync(Guid workspaceId, Guid templateId, CreatePageFromTemplateRequest body, ISender sender)
    {
        var result = await sender.Send(new CreatePageFromTemplateCommand(workspaceId, templateId, body.Title, body.ParentId, body.SpaceId));
        return result.ToCreatedResult(result.Succeeded ? $"/api/v1/pages/{result.Data!.PageId}" : null);
    }
}
