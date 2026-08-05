using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.ListForms;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Queries;

public static class ListFormsEndpoint
{
    public static IEndpointRouteBuilder MapListForms(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Forms.List")
            .WithTags("WorkManagement.Forms")
            .WithSummary("List forms for a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListFormsQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
