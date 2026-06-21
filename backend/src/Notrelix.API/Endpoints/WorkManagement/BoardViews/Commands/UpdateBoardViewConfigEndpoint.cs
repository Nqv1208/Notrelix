using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class UpdateBoardViewConfigEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardViewConfig(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/{viewId:guid}", HandleAsync)
            .WithName("WorkManagement.BoardViews.UpdateConfig")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Update configuration of a board view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        UpdateBoardViewConfigRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardViewConfigCommand(
            workspaceId,
            boardId,
            viewId,
            body.ConfigJson), cancellationToken);
        return Results.Ok(result);
    }
}

