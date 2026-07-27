using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.ListFormSubmissions;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Queries;

public static class ListFormSubmissionsEndpoint
{
    public static IEndpointRouteBuilder MapListFormSubmissions(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Forms.ListSubmissions")
            .WithTags("WorkManagement.Forms")
            .WithSummary("List submissions for a form");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListFormSubmissionsQuery(formId), cancellationToken);
        return result.ToApiResult();
    }
}
