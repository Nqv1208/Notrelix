using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;

namespace Notrelix.API.Endpoints.Identity.Auth.Queries;

public static class GetCurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapGetCurrentUser(this IEndpointRouteBuilder group)
    {
        group.MapGet("/me", HandleAsync)
            .RequireAuthorization()
            .WithName("Identity.Auth.GetCurrentUser")
            .WithTags("Identity.Auth")
            .WithSummary("Get current authenticated user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ISender sender,
        ICurrentUser currentUser)
    {
        var query = new GetCurrentUserQuery { UserId = currentUser.UserId };
        var result = await sender.Send(query);
        return result.ToApiResult();
    }
}
