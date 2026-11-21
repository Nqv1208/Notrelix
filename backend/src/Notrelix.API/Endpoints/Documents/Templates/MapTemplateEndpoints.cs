using Notrelix.API.Contracts.Documents.Templates.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Templates.Commands.ArchivePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.CreatePageFromTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.CreatePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.DeletePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.UpdatePageTemplate;
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
            .WithIdempotencyKey()
            .WithName("Documents.Templates.CreatePageTemplate")
            .WithSummary("Save a page as a workspace template")
            .Produces<PageTemplateDto>(StatusCodes.Status201Created, "application/json");

        group.MapWorkspacePost("/{templateId:guid}/pages", CreatePageAsync)
            .WithIdempotencyKey()
            .WithName("Documents.Templates.CreatePageFromTemplate")
            .WithSummary("Create a page from a template")
            .Produces<PageFromTemplateDto>(StatusCodes.Status201Created, "application/json");

        group.MapWorkspacePatch("/{templateId:guid}", UpdateAsync)
            .WithName("Documents.Templates.UpdatePageTemplate")
            .WithSummary("Rename a template or change its description")
            .WithDescription("The creator or a Workspace Admin. A stale expectedVersion is 412.")
            .Produces<PageTemplateDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspacePost("/{templateId:guid}/archive", ArchiveAsync)
            .WithName("Documents.Templates.ArchivePageTemplate")
            .WithSummary("Archive a template")
            .WithDescription("It leaves the gallery; pages already made from it are unchanged. Archiving an archived template is a no-op. A stale expectedVersion is 412.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspaceDelete("/{templateId:guid}", DeleteAsync)
            .WithName("Documents.Templates.DeletePageTemplate")
            .WithSummary("Delete a template")
            .WithDescription("Pages made from it stay. Pass expectedVersion as a query parameter; 412 when it is stale.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> UpdateAsync(Guid workspaceId, Guid templateId, UpdatePageTemplateRequest body, ISender sender)
        => (await sender.Send(new UpdatePageTemplateCommand(workspaceId, templateId, body.Name, body.Description, body.ExpectedVersion))).ToApiResult();

    private static async Task<IResult> ArchiveAsync(Guid workspaceId, Guid templateId, PageTemplateVersionRequest body, ISender sender)
        => (await sender.Send(new ArchivePageTemplateCommand(workspaceId, templateId, body.ExpectedVersion))).ToNoContentResult();

    private static async Task<IResult> DeleteAsync(Guid workspaceId, Guid templateId, [FromQuery] long? expectedVersion, ISender sender)
    {
        if (expectedVersion is not { } version)
            return EndpointExtensions.InvalidInput("expectedVersion is required as a query parameter.");
        return (await sender.Send(new DeletePageTemplateCommand(workspaceId, templateId, version))).ToNoContentResult();
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
