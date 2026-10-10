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
            .WithName("WorkManagement.BoardFields.AddFieldOption")
            .WithSummary("Add an option to a select/status field");
        group.MapResourceDelete("/{fieldId:guid}/options/{optionId:guid}", HandleRemoveFieldOption)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardFields.RemoveFieldOption")
            .WithSummary("Remove an option from a field");
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
            boardId, fieldId, body.Name, body.Color, body.ExpectedVersion, body.PreviousSiblingId, body.NextSiblingId), cancellationToken);
        return result.ToCreatedResult();
    }

    private static async Task<IResult> HandleRemoveFieldOption(
        Guid boardId,
        Guid fieldId,
        Guid optionId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveFieldOptionCommand(boardId, fieldId, optionId, expectedVersion), cancellationToken);
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
            boardId, fieldId, optionId, body.Name, body.Color, body.ExpectedVersion), cancellationToken);
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

public record AddFieldOptionRequest(string Name, string Color, long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
public record UpdateFieldOptionRequest(string Name, string Color, long ExpectedVersion);
public record MoveFieldOptionRequest(long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
