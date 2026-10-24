using Microsoft.Net.Http.Headers;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Profiles.Queries.GetUserAvatar;

namespace Notrelix.API.Endpoints.Identity.Users;

public static class MapUserAvatarEndpoints
{
    public static IEndpointRouteBuilder RegisterUserAvatarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/users")
            .WithTags("Identity.Profile")
            .WithOpenApi();

        group.MapAuthenticatedGet("/{userId:guid}/avatar", HandleAsync)
            .WithName("Identity.Profile.GetUserAvatar")
            .WithTags("Identity.Profile")
            .WithSummary("Read a user's profile photo (authorised, streamed)")
            .WithDescription("Visible to the person and to people who share an active workspace with them. Any other reader, or a user without an uploaded photo, gets 404. The address carries the object id in 'v', so a response may be cached privately as immutable.")
            .Produces<IFormFile>(StatusCodes.Status200OK, "image/png", "image/jpeg", "image/webp")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid userId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserAvatarQuery(userId), cancellationToken);
        if (!result.Succeeded)
        {
            return result.ToApiResult();
        }

        var content = result.Data!;
        var headers = httpContext.Response.Headers;
        headers[HeaderNames.XContentTypeOptions] = "nosniff";
        headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; sandbox";
        // The address is versioned by object id, so a given address never changes content; the photo
        // is only for the signed-in reader, so it may be cached privately (never by a shared cache).
        headers[HeaderNames.CacheControl] = "private, max-age=86400, immutable";

        // The result disposes the stream once the response has been written.
        return Results.Stream(content.Content, content.ContentType, enableRangeProcessing: false);
    }
}
