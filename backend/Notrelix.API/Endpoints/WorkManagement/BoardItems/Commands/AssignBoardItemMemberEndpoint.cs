using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class AssignBoardItemMemberEndpoint
{
    public static IEndpointRouteBuilder MapAssignBoardItemMember(this IEndpointRouteBuilder group)
    {
        group.MapPost("/assignees", HandleAsync)
            .WithName("WorkManagement.BoardItems.AssignMember")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Assign a member to board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        AssignBoardItemMemberRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignCardMemberCommand(workspaceId, itemId, body.UserId), cancellationToken);
        return result.ToNoContentResult();
    }
}

