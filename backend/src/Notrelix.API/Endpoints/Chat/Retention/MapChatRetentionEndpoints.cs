using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Retention.Commands.SetRetentionPolicy;
using Notrelix.Application.Features.Chat.Retention.Queries.GetRetentionPolicy;

namespace Notrelix.API.Endpoints.Chat.Retention;

/// <summary>The Workspace's chat retention rule: read and set (UC-CHT-H05, CHT-021).</summary>
public static class MapChatRetentionEndpoints
{
    public static IEndpointRouteBuilder MapChatRetentionRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/retention")
            .WithTags("Chat.Retention")
            .WithOpenApi();

        group.MapWorkspaceGet("/", GetAsync)
            .WithName("Chat.Retention.Get")
            .WithSummary("Read the Workspace's chat retention rule")
            .WithDescription("Every member reads the rule and the version to send back; canChange says whether the caller is the Owner who can set it. A Workspace that never set a rule reads as no retention (retentionDays null) at version 1.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePut("/", SetAsync)
            .WithName("Chat.Retention.Set")
            .WithSummary("Set the Workspace's chat retention rule")
            .WithDescription("Only the Workspace Owner. Keep messages retentionDays days (7 to 3650, or null for no retention), then turn them into tombstones (mode tombstone) or delete them for good (mode purge). A stale expectedVersion is 412. The rule is applied in the background by the Chat.Retention.Apply job; this call removes nothing. The change is audited with the old and the new rule.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> GetAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetRetentionPolicyQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> SetAsync(Guid workspaceId, SetRetentionPolicyRequest body, ISender sender)
    {
        if (body.ExpectedVersion is not { } expectedVersion)
            return EndpointExtensions.InvalidInput("expectedVersion is required.");
        if (body.Mode is null)
            return EndpointExtensions.InvalidInput("mode is required.");

        var result = await sender.Send(new SetRetentionPolicyCommand(workspaceId, body.RetentionDays, body.Mode, expectedVersion));
        return result.ToApiResult();
    }
}
