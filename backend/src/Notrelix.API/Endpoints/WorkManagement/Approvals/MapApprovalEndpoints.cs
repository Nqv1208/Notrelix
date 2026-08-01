using Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;
using Notrelix.API.Endpoints.WorkManagement.Approvals.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals;

public static class MapApprovalEndpoints
{
    public static IEndpointRouteBuilder MapApprovals(this IEndpointRouteBuilder app)
    {
        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/approvals")
            .WithTags("WorkManagement.Approvals")
            .WithOpenApi();
        boardGroup.MapCreateApprovalRequest();
        boardGroup.MapListApprovalRequests();

        var group = app
            .MapGroup("/api/v1/approvals/{requestId:guid}")
            .WithTags("WorkManagement.Approvals")
            .WithOpenApi();
        group.MapApproveApprovalRequest();
        group.MapRejectApprovalRequest();
        group.MapCancelApprovalRequest();
        group.MapDeleteApprovalRequest();
        group.MapRestoreApprovalRequest();

        return app;
    }
}
