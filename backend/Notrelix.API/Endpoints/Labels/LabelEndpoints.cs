using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;
using Notrelix.Application.Features.Boards.Queries;

namespace Notrelix.API.Endpoints.Labels;

public static class LabelEndpoints
{
    public static IEndpointRouteBuilder MapLabelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/boards/{boardId:guid}/labels")
            .WithTags("Labels")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", GetLabels)
            .WithName("GetLabels")
            .WithSummary("Get all labels for a board");

        group.MapPost("/", CreateLabel)
            .WithName("CreateLabel")
            .WithSummary("Create a new label");

        group.MapPatch("/{labelId:guid}", UpdateLabel)
            .WithName("UpdateLabel")
            .WithSummary("Update label name or color");

        group.MapDelete("/{labelId:guid}", DeleteLabel)
            .WithName("DeleteLabel")
            .WithSummary("Delete a label");

        return app;
    }

    private static async Task<IResult> GetLabels(Guid boardId, ISender sender)
    {
        var result = await sender.Send(new GetLabelsQuery(boardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateLabel(Guid boardId, CreateLabelRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateLabelCommand(boardId, body.Color, body.Name));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateLabel(Guid boardId, Guid labelId, UpdateLabelRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateLabelCommand(labelId, body.Name, body.Color));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteLabel(Guid boardId, Guid labelId, ISender sender)
    {
        var result = await sender.Send(new DeleteLabelCommand(labelId));
        return result.ToNoContentResult();
    }
}

public record CreateLabelRequest(string Color, string? Name = null);
public record UpdateLabelRequest(string? Name, string? Color);
