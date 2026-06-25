using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using Notrelix.Domain.WorkManagement;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class SaveBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapSaveBoardView(this IEndpointRouteBuilder group)
    {
        group.MapPut("/", HandleAsync)
            .WithName("WorkManagement.BoardViews.Save")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Save board view preference");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        SaveBoardViewRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SaveBoardViewCommand(workspaceId, boardId, Enum.Parse<ViewMode>(body.ViewMode, ignoreCase: true), body.Config ?? body.Filters), cancellationToken);
        return result.ToApiResult();
    }
}

