using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Commands.AnswerCall;

/// <summary>Answers a ringing call: Ringing becomes Active, the caller joins and receives a join token (SPEC-CHAT-MEDIA-CALLS section 6). The starter cannot answer their own call. A caller already in another call is 409 call-busy; a full call is 422 call-full. A repeat by someone already in the call changes nothing and returns the token again. A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record AnswerCallCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion)
    : ICommand<Result<CallJoinDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.call"), CallId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class AnswerCallCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatCallMediaGateway media,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<AnswerCallCommand, Result<CallJoinDto>>
{
    public async Task<Result<CallJoinDto>> Handle(AnswerCallCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        var (call, space, caller) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var now = clock.UtcNow;
        await ChatCallAccess.RequireMayTakePartAsync(db, space, caller, userId, cancellationToken);
        if (await ChatCallAccess.IsBusyAsync(db, userId, call.Id, cancellationToken))
            throw ChatCallAccess.Busy();

        call.Answer(userId, now);
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

public class AnswerCallCommandValidator : AbstractValidator<AnswerCallCommand>
{
    public AnswerCallCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
