using Notrelix.API.Contracts.Documents.Versions.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;
using Notrelix.Application.Features.Documents.Versions.Commands.RestorePageVersion;
using Notrelix.Application.Features.Documents.Versions.Queries.GetPageVersion;
using Notrelix.Application.Features.Documents.Versions.Queries.ListPageVersions;

namespace Notrelix.API.Endpoints.Documents.Versions;

public static class MapVersionEndpoints
{
    public static IEndpointRouteBuilder AddVersionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/pages/{pageId:guid}/versions")
            .WithTags("Documents.Versions")
            .WithOpenApi();

        group.MapResourceGet("/", ListAsync)
            .WithName("Documents.Versions.ListPageVersions")
            .WithSummary("List the history entries of a page, newest first")
            .Produces<List<PageVersionDto>>(StatusCodes.Status200OK, "application/json");

        group.MapResourceGet("/{versionId:guid}", GetAsync)
            .WithName("Documents.Versions.GetPageVersion")
            .WithSummary("Read the captured content of one history entry")
            .Produces<PageVersionDetailDto>(StatusCodes.Status200OK, "application/json");

        group.MapResourcePost("/", CreateAsync)
            .WithName("Documents.Versions.CreatePageVersion")
            .WithSummary("Save the page as it is now as a named version")
            .Produces<PageVersionDto>(StatusCodes.Status201Created, "application/json");

        group.MapResourcePost("/{versionId:guid}/restore", RestoreAsync)
            .WithName("Documents.Versions.RestorePageVersion")
            .WithSummary("Restore the page's blocks to a version (adds a new version)")
            .Produces<RestorePageVersionResultDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> ListAsync(Guid pageId, ISender sender)
        => (await sender.Send(new ListPageVersionsQuery(pageId))).ToApiResult();

    private static async Task<IResult> GetAsync(Guid pageId, Guid versionId, ISender sender)
        => (await sender.Send(new GetPageVersionQuery(pageId, versionId))).ToApiResult();

    private static async Task<IResult> CreateAsync(Guid pageId, CreatePageVersionRequest body, ISender sender)
    {
        var result = await sender.Send(new CreatePageVersionCommand(pageId, body.Name));
        return result.ToCreatedResult(result.Succeeded ? $"/api/v1/pages/{pageId}/versions/{result.Data!.Id}" : null);
    }

    private static async Task<IResult> RestoreAsync(Guid pageId, Guid versionId, RestorePageVersionRequest body, ISender sender)
        => (await sender.Send(new RestorePageVersionCommand(
            pageId,
            versionId,
            body.CurrentBlocks.Select(block => new SeenBlockVersion(block.BlockId, block.Version)).ToList()))).ToApiResult();
}
