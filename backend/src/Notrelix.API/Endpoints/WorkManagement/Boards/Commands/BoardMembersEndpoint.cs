using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardMembers;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class BoardMembersEndpoint
{
    public static IEndpointRouteBuilder MapBoardMembers(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleGetBoardMembers)
            .WithName("WorkManagement.Boards.GetMembers");
        group.MapResourcePost("/", HandleAddBoardMember)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.AddMember");
        group.MapResourceDelete("/{userId:guid}", HandleRemoveBoardMember)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.RemoveMember");
        group.MapResourcePatch("/{userId:guid}/role", HandleUpdateBoardMemberRole)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.UpdateMemberRole");
        return group;
    }

    private static async Task<IResult> HandleGetBoardMembers(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardMembersQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> HandleAddBoardMember(
        Guid boardId,
        AddBoardMemberRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddBoardMemberCommand(boardId, body.UserId, body.Role is not null ? Enum.Parse<BoardRole>(body.Role, ignoreCase: true) : null), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleRemoveBoardMember(
        Guid boardId,
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveBoardMemberCommand(boardId, userId), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleUpdateBoardMemberRole(
        Guid boardId,
        Guid userId,
        UpdateBoardMemberRoleRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var role = Enum.Parse<BoardRole>(body.Role, ignoreCase: true);
        var result = await sender.Send(new UpdateBoardMemberRoleCommand(boardId, userId, role), cancellationToken);
        return result.ToNoContentResult();
    }
}
