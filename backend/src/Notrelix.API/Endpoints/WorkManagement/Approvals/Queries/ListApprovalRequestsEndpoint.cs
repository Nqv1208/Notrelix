using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListApprovalRequests;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Queries;

public static class ListApprovalRequestsEndpoint
{
    public static IEndpointRouteBuilder MapListApprovalRequests(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Approvals.List")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("List approval requests for a board");
        return group;
    }

    public static IEndpointRouteBuilder MapListApprovalRequestsPage(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/page", HandlePageAsync)
            .WithName("WorkManagement.Approvals.ListPage")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("List a page of the approval requests of a board")
            .WithDescription("Newest first, default 50 and at most 200 a page. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<ApprovalRequestDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandlePageAsync(
        Guid boardId,
        string? cursor,
        int? limit,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var take = TimeCursor.ClampLimit(limit);
        var result = await sender.Send(new ListApprovalRequestsQuery(boardId, TimeCursor.Decode(cursor), take + 1), cancellationToken);
        if (!result.Succeeded) return result.ToApiResult();
        return Results.Ok(TimeCursor.Page(result.Data!, take, request => (request.CreatedAt, request.Id)));
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListApprovalRequestsQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
