using Notrelix.Application.Features.Identity.Sessions.DTOs;
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
            .Produces<SessionInfoDto[]>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Sessions")
            .WithSummary("List active sessions for the authenticated user")
            .WithMetadata(new RateLimitPolicyAttribute("GeneralAuthenticated"));

        group.MapAuthenticatedPost("/sessions/{sessionId:guid}/revoke", RevokeSessionAsync)
            .WithName("Identity.Sessions.Revoke")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Sessions")
            .WithSummary("Revoke a session")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

        group.MapAuthenticatedPost("/sessions/revoke-others", RevokeOtherSessionsAsync)
            .WithName("Identity.Sessions.RevokeOthers")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Sessions")
            .WithSummary("Revoke all other sessions")
            .WithDescription("Revokes every active session of the current user except the session that issued the request.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

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