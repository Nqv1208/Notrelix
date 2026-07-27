using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class ClearFieldValueEndpoint
{
    public static IEndpointRouteBuilder MapClearFieldValue(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/values/{fieldId:guid}", HandleAsync)
            .WithName("WorkManagement.BoardItems.ClearFieldValue")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Clear a field value on a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid fieldId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ClearFieldValueCommand(itemId, fieldId), cancellationToken);
        return result.ToNoContentResult();
    }
}
