using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Calls.Commands.StartCall;

/// <summary>
/// Starts a voice or video call in a space and returns the call (SPEC-CHAT-MEDIA-CALLS section 6). The result carries no media token: it is
/// stored for idempotent replay, and a token is a credential that is never persisted; the transport asks for the starter's token with
/// <c>GetCallJoinTokenQuery</c> after the start, and again on every replay.
/// Only an active member of the space starts one (an outsider is NotFound); a Guest is refused by the call-start action, and a
/// member a channel's overrides keep from posting is refused as well. A direct call to a person the caller blocked, or who blocked the
/// caller, is NotFound. A space holds one Ringing or Active call (409 call-already-active); the check runs under the space lock and
/// the unique index settles two starts that race. A caller already in another call is 409 call-busy. When the other person of a
/// direct call is in a call, the call is stored as Ended(Busy) and no ring is sent. If the media plane cannot be
/// reached the answer is 503 chat.calls.unavailable and nothing is stored.
/// </summary>
[IdempotencyOperation("chat.calls.start-call.v1")]
public record StartCallCommand(Guid WorkspaceId, Guid SpaceId, string Kind)
    : ICommand<Result<CallDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChatCallStart;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class StartCallCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IChatCallMediaGateway media,
    ChatCallEnding ending,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<StartCallCommand, Result<CallDto>>
{
    public async Task<Result<CallDto>> Handle(StartCallCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var kind = CallWire.CallKindFromWire(request.Kind);
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var (space, caller) = await ChatCallAccess.MemberSpaceAsync(db, members, request.WorkspaceId, request.SpaceId, userId, cancellationToken);

        // The lock comes first: the live-call check and the insert run one at a time per space.
        await sequences.LockAsync(space.Id, cancellationToken);
        var counterpart = await ChatCallAccess.RequireMayTakePartAsync(db, space, caller, userId, cancellationToken);

        if (await db.ChatCalls.AsNoTracking().AnyAsync(c => c.SpaceId == space.Id && c.State != CallState.Ended, cancellationToken))
            throw new ConflictException("This conversation already has a call.", ChatRuleCodes.CallAlreadyActive);
        if (await ChatCallAccess.IsBusyAsync(db, userId, null, cancellationToken))
            throw ChatCallAccess.Busy();

        var now = clock.UtcNow;
        var roomKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var region = options.Value.Region;

        if (counterpart is { } other && await ChatCallAccess.IsBusyAsync(db, other, null, cancellationToken))
        {
            var busy = Call.StartBusy(accountId, request.WorkspaceId, space.Id, space.Kind, userId, kind, roomKey, region, now);
            db.ChatCalls.Add(busy);
            await ending.FinishAsync(space, busy, userId, cancellationToken);
            return Result<CallDto>.Success(CallDto.From(busy));
        }

        try
        {
            await media.CreateRoomAsync(roomKey, region, Call.MaxParticipants(space.Kind, kind), cancellationToken);
        }
        catch (CallMediaUnavailableException)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(ChatRuleCodes.CallsUnavailable, "Calls are not available right now. Try again shortly.");
        }

        var call = Call.Start(accountId, request.WorkspaceId, space.Id, space.Kind, userId, kind, roomKey, region, now);
        db.ChatCalls.Add(call);
        return Result<CallDto>.Success(CallDto.From(call));
    }
}

public class StartCallCommandValidator : AbstractValidator<StartCallCommand>
{
    public StartCallCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Kind).Must(k => k is "voice" or "video").WithMessage("The call kind is voice or video.");
    }
}
