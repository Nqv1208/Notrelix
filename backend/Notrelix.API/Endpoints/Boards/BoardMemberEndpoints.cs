using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;
using Notrelix.Application.Features.Boards.Queries;

namespace Notrelix.API.Endpoints.Boards;

public static class BoardMemberEndpoints
{
    public static IEndpointRouteBuilder MapBoardMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/boards/{boardId:guid}/members")
            .WithTags("Board Members")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", GetBoardMembers)
            .WithName("GetBoardMembers")
            .WithSummary("Get board members");

        group.MapPost("/", AddBoardMember)
            .WithName("AddBoardMember")
            .WithSummary("Add a member to board");

        group.MapDelete("/{userId:guid}", RemoveBoardMember)
            .WithName("RemoveBoardMember")
            .WithSummary("Remove a member from board");

        return app;
    }

    private static async Task<IResult> GetBoardMembers(Guid boardId, ISender sender)
    {
        var result = await sender.Send(new GetBoardMembersQuery(boardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> AddBoardMember(Guid boardId, AddBoardMemberRequest body, ISender sender)
    {
        var result = await sender.Send(new AddBoardMemberCommand(boardId, body.UserId, body.Role));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> RemoveBoardMember(Guid boardId, Guid userId, ISender sender)
    {
        var result = await sender.Send(new RemoveBoardMemberCommand(boardId, userId));
        return result.ToNoContentResult();
    }
}

public record AddBoardMemberRequest(Guid UserId, string? Role = "Member");
