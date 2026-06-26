using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageHistory;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class GetPageHistoryEndpoint
{
    public static IEndpointRouteBuilder MapGetPageHistory(this IEndpointRouteBuilder group)
    {
        group.MapGet("/history", HandleAsync)
            .WithName("Documents.Pages.GetPageHistory")
            .WithTags("Documents.Pages")
            .WithSummary("Get page history");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageHistoryQuery(pageId));
        return result.ToApiResult();
    }
}
