using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;
using Notrelix.Application.Features.Boards.Queries;

namespace Notrelix.API.Endpoints.Boards;

public static class BoardViewEndpoints
{
    public static IEndpointRouteBuilder MapBoardViewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/boards/{boardId:guid}/view")
            .WithTags("Board Views")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", GetBoardView)
            .WithName("GetBoardView")
            .WithSummary("Get current user's board view preference");

        group.MapPut("/", SaveBoardView)
            .WithName("SaveBoardView")
            .WithSummary("Save board view preference");

        return app;
    }

    private static async Task<IResult> GetBoardView(Guid boardId, ISender sender)
    {
        var result = await sender.Send(new GetBoardViewQuery(boardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> SaveBoardView(Guid boardId, SaveBoardViewRequest body, ISender sender)
    {
        var result = await sender.Send(new SaveBoardViewCommand(boardId, body.ViewMode, body.Filters));
        return result.ToApiResult();
    }
}

public record SaveBoardViewRequest(string ViewMode, string? Filters);
