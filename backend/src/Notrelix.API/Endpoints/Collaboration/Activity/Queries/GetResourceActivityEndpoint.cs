using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.API.Endpoints.Collaboration.Activity.Queries;

public static class GetResourceActivityEndpoint
{
    public static IEndpointRouteBuilder MapGetCardActivity(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Collaboration.Activity.GetCardActivity")
            .WithTags("Collaboration.Activity")
            .WithSummary("Get activity log for a card");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid cardId, ISender sender, int page = 1, int pageSize = 20)
    {
        var result = await sender.Send(new GetResourceActivityQuery(Enum.Parse<ResourceType>("Card", ignoreCase: true), cardId, page, pageSize));
        return result.ToApiResult();
    }
}
