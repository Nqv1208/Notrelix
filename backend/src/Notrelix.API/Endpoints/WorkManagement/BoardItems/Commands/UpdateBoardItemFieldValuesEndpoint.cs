using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UpdateBoardItemFieldValuesEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardItemFieldValues(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/field-values", HandleAsync)
            .WithName("WorkManagement.BoardItems.UpdateFieldValues")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Update multiple field values for a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        UpdateBoardItemFieldValuesRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var values = body.Values ?? new Dictionary<Guid, object?>();
        if (body.FieldDefinitionId.HasValue)
            values[body.FieldDefinitionId.Value] = body.Value;

        var result = await sender.Send(new UpdateBoardItemFieldValuesCommand(itemId, values), cancellationToken);
        return result.ToApiResult();
    }
}

