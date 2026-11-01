using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.ListArchivedBoards;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.ListDeletedBoards;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class ListRetiredBoardsEndpoints
{
    public static IEndpointRouteBuilder MapListRetiredBoards(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/archived", async (Guid workspaceId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListArchivedBoardsQuery(workspaceId), ct)).ToApiResult())
            .WithName("WorkManagement.Boards.ListArchived")
            .Produces<List<RetiredBoardDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Get the archived boards of a workspace that the caller may see");
        group.MapWorkspaceGet("/deleted", async (Guid workspaceId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListDeletedBoardsQuery(workspaceId), ct)).ToApiResult())
            .WithName("WorkManagement.Boards.ListDeleted")
            .Produces<List<RetiredBoardDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Get the boards deleted in the last 30 days that the caller can restore");
        return group;
    }
}
