using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderFieldOptions;

namespace Notrelix.API.Endpoints.WorkManagement.BoardFields;

public static class FieldOptionEndpoints
{
    public static IEndpointRouteBuilder MapFieldOptionEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{fieldId:guid}/options", HandleAddFieldOption)
            .WithName("WorkManagement.BoardFields.AddFieldOption")
            .WithSummary("Add an option to a select/status field");
        group.MapResourceDelete("/{fieldId:guid}/options/{optionId:guid}", HandleRemoveFieldOption)
            .WithName("WorkManagement.BoardFields.RemoveFieldOption")
            .WithSummary("Remove an option from a field");
        group.MapResourcePatch("/{fieldId:guid}/options/{optionId:guid}", HandleUpdateFieldOption)
            .WithName("WorkManagement.BoardFields.UpdateFieldOption")
            .WithSummary("Update a field option's name and color");
        group.MapResourcePost("/{fieldId:guid}/options/reorder", HandleReorderFieldOptions)
            .WithName("WorkManagement.BoardFields.ReorderFieldOptions")
            .WithSummary("Reorder field options");
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
            boardId, fieldId, body.Name, body.Color, body.Position), cancellationToken);
        return result.ToCreatedResult();
    }

    private static async Task<IResult> HandleRemoveFieldOption(
        Guid boardId,
        Guid fieldId,
        Guid optionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveFieldOptionCommand(boardId, fieldId, optionId), cancellationToken);
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
            boardId, fieldId, optionId, body.Name, body.Color), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> HandleReorderFieldOptions(
        Guid boardId,
        Guid fieldId,
        ReorderFieldOptionsRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReorderFieldOptionsCommand(
            boardId, fieldId, body.OrderedOptionIds), cancellationToken);
        return result.ToNoContentResult();
    }
}

public record AddFieldOptionRequest(string Name, string Color, string? Position);
public record UpdateFieldOptionRequest(string Name, string Color);
public record ReorderFieldOptionsRequest(List<Guid> OrderedOptionIds);
