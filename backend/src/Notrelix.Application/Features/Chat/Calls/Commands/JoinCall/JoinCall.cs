using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Commands.JoinCall;

/// <summary>Joins a live group or channel call and receives a join token. A ringing call becomes Active, as an answer does. A caller already in another call is 409 call-busy; a full call (25 voice, 12 video) is 422 call-full, also for the person who reached the last seat second (the space lock runs seats one after the other). A repeat changes nothing and returns the token again. A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record JoinCallCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion)
    : ICommand<Result<CallJoinDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.call"), CallId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class JoinCallCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IChatCallMediaGateway media,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<JoinCallCommand, Result<CallJoinDto>>
{
    public async Task<Result<CallJoinDto>> Handle(JoinCallCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        await ChatCallAccess.LockSpaceForSeatAsync(db, sequences, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var (call, space, caller) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var now = clock.UtcNow;
        await ChatCallAccess.RequireMayTakePartAsync(db, space, caller, userId, cancellationToken);
        if (await ChatCallAccess.IsBusyAsync(db, userId, call.Id, cancellationToken))
            throw ChatCallAccess.Busy();

        call.Join(userId, now);
        CallJoinTokenDto? token = null;
        if (call.IsJoined(userId))
        {
            try
            {
                token = ChatCallAccess.ToDto(await media.IssueTokenAsync(
                    call.RoomKey, userId, call.Kind == CallKind.Video, TimeSpan.FromSeconds(options.Value.TokenTtlSeconds), cancellationToken));
            }
            catch (CallMediaUnavailableException)
            {
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(Notrelix.Domain.Chat.Rules.ChatRuleCodes.CallsUnavailable, "Calls are not available right now. Try again shortly.");
            }
        }

        return Result<CallJoinDto>.Success(new CallJoinDto(CallDto.From(call), token));
    }
}

public class JoinCallCommandValidator : AbstractValidator<JoinCallCommand>
{
    public JoinCallCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
