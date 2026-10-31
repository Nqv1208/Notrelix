using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.LeaveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardMembers;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class BoardMembersEndpoint
{
    public static IEndpointRouteBuilder MapBoardMembers(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleGetBoardMembers)
            .WithName("WorkManagement.Boards.GetMembers")
            .Produces<List<BoardMemberDto>>();
        group.MapResourcePost("/", HandleAddBoardMember)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.AddMember");
        group.MapResourcePost("/leave", HandleLeaveBoard)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.Leave")
            .WithSummary("Leave a board: end your own explicit membership");
        group.MapResourceDelete("/{userId:guid}", HandleRemoveBoardMember)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.RemoveMember");
        group.MapResourcePatch("/{userId:guid}/role", HandleUpdateBoardMemberRole)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.UpdateMemberRole");
        return group;
    }

    private const string UnknownRole = "role must be one of Guest, Member, Observer, Admin, Owner.";

    // A role name that names no role is a bad request, never a server error; numbers are not role names.
    private static bool TryParseRole(string value, out BoardRole role) =>
        Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role) && !string.IsNullOrWhiteSpace(value) && !char.IsDigit(value.TrimStart()[0]);

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
        BoardRole? role = null;
        if (body.Role is not null)
        {
            if (!TryParseRole(body.Role, out var parsed)) return EndpointExtensions.InvalidInput(UnknownRole);
            role = parsed;
        }
        var result = await sender.Send(new AddBoardMemberCommand(boardId, body.UserId, role), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleLeaveBoard(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LeaveBoardCommand(boardId), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleRemoveBoardMember(
        Guid boardId,
        Guid userId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveBoardMemberCommand(boardId, userId, expectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleUpdateBoardMemberRole(
        Guid boardId,
        Guid userId,
        UpdateBoardMemberRoleRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryParseRole(body.Role, out var role)) return EndpointExtensions.InvalidInput(UnknownRole);
        var result = await sender.Send(new UpdateBoardMemberRoleCommand(boardId, userId, role, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
