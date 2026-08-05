using Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.CreateSavedFilter;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class CreateSavedFilterEndpoint
{
    public static IEndpointRouteBuilder MapCreateSavedFilter(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.SavedFilters.Create")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Create a new saved filter");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateSavedFilterRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSavedFilterCommand(boardId, body.Name, body.Rules), cancellationToken);
        return result.ToCreatedResult();
    }
}
