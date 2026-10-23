using Notrelix.API.Contracts.Workspaces.Members.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberJobTitle;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMyJobTitle;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class UpdateMemberJobTitleEndpoints
{
    public static IEndpointRouteBuilder MapUpdateMemberJobTitle(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/me/job-title", UpdateMineAsync)
            .WithName("Workspaces.Members.UpdateMyJobTitle")
            .WithTags("Workspaces.Members")
            .WithSummary("Set or clear your own job title in this workspace")
            .WithDescription("Any member of the workspace may change their own title. A blank title clears it. Answers 412 when expectedVersion is stale.")
            .Produces<WorkspaceMemberProfileDto>(StatusCodes.Status200OK, "application/json");

        group.MapWorkspacePut("/{userId:guid}/job-title", UpdateMemberAsync)
            .WithName("Workspaces.Members.UpdateMemberJobTitle")
            .WithTags("Workspaces.Members")
            .WithSummary("Set or clear another member's job title")
            .WithDescription("Workspace Owner and Admin only. A blank title clears it. Answers 412 when expectedVersion is stale.")
            .Produces<WorkspaceMemberProfileDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> UpdateMineAsync(
        Guid workspaceId,
        UpdateMemberJobTitleRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateMyJobTitleCommand(workspaceId, body.JobTitle, body.ExpectedVersion));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateMemberAsync(
        Guid workspaceId,
        Guid userId,
        UpdateMemberJobTitleRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateMemberJobTitleCommand(workspaceId, userId, body.JobTitle, body.ExpectedVersion));
        return result.ToApiResult();
    }
}
