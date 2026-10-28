using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Queries;

public static class GetFormEndpoint
{
    public static IEndpointRouteBuilder MapGetForm(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Forms.Get")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Get a form with its questions");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid formId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFormQuery(formId), cancellationToken);
        return result.ToApiResult();
    }
}
