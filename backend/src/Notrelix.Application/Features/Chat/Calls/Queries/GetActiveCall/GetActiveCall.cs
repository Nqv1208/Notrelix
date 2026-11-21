using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Queries.GetActiveCall;

/// <summary>The Ringing or Active call of a space, for the banner and re-join; null when there is none. An outsider is NotFound.</summary>
public record GetActiveCallQuery(Guid WorkspaceId, Guid SpaceId)
    : IQuery<Result<CallDto?>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetActiveCallQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<GetActiveCallQuery, Result<CallDto?>>
{
    public async Task<Result<CallDto?>> Handle(GetActiveCallQuery request, CancellationToken cancellationToken)
    {
        await ChatCallAccess.MemberSpaceAsync(db, members, request.WorkspaceId, request.SpaceId, requestContext.UserId, cancellationToken);
        var call = await db.ChatCalls.AsNoTracking().Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.SpaceId == request.SpaceId && c.State != CallState.Ended, cancellationToken);
        return Result<CallDto?>.Success(call is null ? null : CallDto.From(call));
    }
}

public class GetActiveCallQueryValidator : AbstractValidator<GetActiveCallQuery>
{
    public GetActiveCallQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
