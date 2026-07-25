using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateEmail;

namespace Notrelix.API.Endpoints.Identity.Profile.Commands;

public static class UpdateEmailEndpoint
{
    public static IEndpointRouteBuilder MapUpdateEmail(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/email", HandleAsync)
            .WithName("Identity.Profile.UpdateEmail")
            .WithTags("Identity.Profile")
            .WithSummary("Update the current user's email address");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        UpdateEmailCommand command,
        ISender sender)
        => (await sender.Send(command)).ToApiResult();
}
