using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Sessions.Commands.RevokeOtherSessions;
using Notrelix.Application.Features.Identity.Sessions.Commands.RevokeSession;
using Notrelix.Application.Features.Identity.Sessions.Queries.GetUserSessions;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/sessions", GetSessionsAsync)
            .WithName("Identity.Sessions.List")
            .WithTags("Identity.Sessions")
            .WithSummary("List active sessions for the authenticated user")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        group.MapAuthenticatedPost("/sessions/{sessionId:guid}/revoke", RevokeSessionAsync)
            .WithName("Identity.Sessions.Revoke")
            .WithTags("Identity.Sessions")
            .WithSummary("Revoke a session")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        group.MapAuthenticatedPost("/sessions/revoke-others", RevokeOtherSessionsAsync)
            .WithName("Identity.Sessions.RevokeOthers")
            .WithTags("Identity.Sessions")
            .WithSummary("Revoke all other sessions")
            .WithDescription("Revokes every active session of the current user except the session that issued the request.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        return group;
    }

    private static async Task<IResult> GetSessionsAsync(ISender sender)
    {
        var result = await sender.Send(new GetUserSessionsQuery());
        return result.ToApiResult();
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid sessionId,
        ISender sender)
    {
        var result = await sender.Send(new RevokeSessionCommand { SessionId = sessionId });
        return result.ToApiResult();
    }

    private static async Task<IResult> RevokeOtherSessionsAsync(ISender sender)
    {
        var result = await sender.Send(new RevokeOtherSessionsCommand());
        return result.ToApiResult();
    }
}