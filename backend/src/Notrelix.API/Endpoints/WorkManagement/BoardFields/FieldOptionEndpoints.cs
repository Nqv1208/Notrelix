using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;

namespace Notrelix.API.Endpoints.WorkManagement.BoardFields;

public static class FieldOptionEndpoints
{
    public static IEndpointRouteBuilder MapFieldOptionEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{fieldId:guid}/options", HandleAddFieldOption)
            .WithIdempotencyKey()
            .Produces<AddFieldOptionResult>(StatusCodes.Status201Created, "application/json")
            .WithName("WorkManagement.BoardFields.AddFieldOption")
            .WithSummary("Add an option to a select/status field");
        group.MapResourceDelete("/{fieldId:guid}/options/{optionId:guid}", HandleRemoveFieldOption)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardFields.RemoveFieldOption")
            .WithSummary("Remove an option from a field and move the Items that use it to a replacement option");
        group.MapResourcePatch("/{fieldId:guid}/options/{optionId:guid}", HandleUpdateFieldOption)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardFields.UpdateFieldOption")
            .WithSummary("Update a field option's name and color");
        group.MapResourcePost("/{fieldId:guid}/options/{optionId:guid}/move", HandleMoveFieldOption)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardFields.MoveFieldOption")
            .WithSummary("Move a field option relative to its neighbors");
        return group;
    }

    private static async Task<IResult> HandleAddFieldOption(
        Guid boardId,
        Guid fieldId,
        AddFieldOptionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddFieldOptionCommand(
            boardId, fieldId, body.Name, body.Color, body.ExpectedVersion, body.PreviousSiblingId, body.NextSiblingId, body.Category), cancellationToken);
        return result.ToCreatedResult();
    }

    private static async Task<IResult> HandleRemoveFieldOption(
        Guid boardId,
        Guid fieldId,
        Guid optionId,
        [FromQuery] long expectedVersion,
        [FromQuery] Guid? replacementOptionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveFieldOptionCommand(boardId, fieldId, optionId, expectedVersion, replacementOptionId), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleUpdateFieldOption(
        Guid boardId,
        Guid fieldId,
        Guid optionId,
        UpdateFieldOptionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateFieldOptionCommand(
            boardId, fieldId, optionId, body.Name, body.Color, body.ExpectedVersion, body.Category), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> HandleMoveFieldOption(
        Guid boardId,
        Guid fieldId,
        Guid optionId,
        MoveFieldOptionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveFieldOptionCommand(
            boardId, fieldId, optionId, body.ExpectedVersion, body.PreviousSiblingId, body.NextSiblingId), cancellationToken);
        return result.ToNoContentResult();
    }
}

/// <param name="Category">Work-status category of an option of a Status field: "todo", "in_progress", "review" or "done". Omitted on a Status field it defaults to "in_progress"; other field types take none.</param>
public record AddFieldOptionRequest(string Name, string Color, long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null, string? Category = null);
/// <param name="Category">New work-status category of an option of a Status field ("todo", "in_progress", "review", "done"); omitted keeps the current one.</param>
public record UpdateFieldOptionRequest(string Name, string Color, long ExpectedVersion, string? Category = null);
public record MoveFieldOptionRequest(long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
