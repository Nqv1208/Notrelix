using Notrelix.API.Contracts.WorkManagement.Approvals.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CreateApprovalRequest;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class CreateApprovalRequestEndpoint
{
    public static IEndpointRouteBuilder MapCreateApprovalRequest(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("WorkManagement.Approvals.Create")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Create a new approval request");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateApprovalRequestRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var steps = body.Steps?
            .Select(s => new ApprovalStepDto(s.ApproverUserId, s.ApproverTeamId))
            .ToList();

        var result = await sender.Send(
            new CreateApprovalRequestCommand(boardId, ResourceType.Board, body.Title, body.Description, steps),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
