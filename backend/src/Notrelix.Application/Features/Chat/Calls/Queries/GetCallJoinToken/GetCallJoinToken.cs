using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Queries.GetCallJoinToken;

/// <summary>
/// The call with a fresh join token for the caller, who must be a joined participant of the live call. The start of a call returns no
/// token (a replay of an idempotent start would hand out a stored credential); the transport calls this right after it, so every start
/// and every replay mints a new short-lived token after checking that the caller still has access. A person outside the space is
/// NotFound; a Guest, a read-only member or a blocked pair is refused as for answer and join; a caller who is not in the call is 403
/// call-not-participant; an ended call returns the call and no token.
/// </summary>
public record GetCallJoinTokenQuery(Guid WorkspaceId, Guid CallId)
    : IQuery<Result<CallJoinDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetCallJoinTokenQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatCallMediaGateway media,
    IOptions<ChatCallOptions> options) : IRequestHandler<GetCallJoinTokenQuery, Result<CallJoinDto>>
{
    public async Task<Result<CallJoinDto>> Handle(GetCallJoinTokenQuery request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        var (call, space, caller) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        if (call.IsEnded)
            return Result<CallJoinDto>.Success(new CallJoinDto(CallDto.From(call), null));

        await ChatCallAccess.RequireMayTakePartAsync(db, space, caller, userId, cancellationToken);
        if (!call.IsJoined(userId))
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(Notrelix.Domain.Chat.Rules.ChatRuleCodes.CallNotParticipant, "You are not in this call.");

        try
        {
            var token = await media.IssueTokenAsync(
                call.RoomKey, userId, call.Kind == CallKind.Video, TimeSpan.FromSeconds(options.Value.TokenTtlSeconds), cancellationToken);
            return Result<CallJoinDto>.Success(new CallJoinDto(CallDto.From(call), ChatCallAccess.ToDto(token)));
        }
        catch (CallMediaUnavailableException)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(Notrelix.Domain.Chat.Rules.ChatRuleCodes.CallsUnavailable, "Calls are not available right now. Try again shortly.");
        }
    }
}

public class GetCallJoinTokenQueryValidator : AbstractValidator<GetCallJoinTokenQuery>
{
    public GetCallJoinTokenQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
    }
}
