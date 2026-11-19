using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Calls.Commands.UpdateCallMediaState;

/// <summary>Sets the caller's own microphone, camera and screen share in a live call (PUT participants/me). A voice call carries no camera (400 call-media-not-allowed); a caller who is not in the call is 403 call-not-participant. Starting a screen share needs the right to take part (a Guest or a member the channel keeps from posting is 403) and is refused in a voice call (400 call-media-not-allowed); only one person shares at a time, so a second share is 409 call-screen-share-taken unless ReplaceShare is set, and then the first share ends in the same change. The same values change nothing. A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record UpdateCallMediaStateCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion, bool AudioOn, bool VideoOn, bool SharingScreen, bool ReplaceShare = false)
    : ICommand<Result<CallDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.call"), CallId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UpdateCallMediaStateCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<UpdateCallMediaStateCommand, Result<CallDto>>
{
    public async Task<Result<CallDto>> Handle(UpdateCallMediaStateCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        var (call, space, caller) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        // Stopping a share is always allowed; starting one needs the same right as taking part (the media server gives a revoked person no source).
        if (request.SharingScreen)
            await ChatCallAccess.RequireMayTakePartAsync(db, space, caller, userId, cancellationToken);
        var now = clock.UtcNow;
        call.UpdateMediaState(userId, request.AudioOn, request.VideoOn, request.SharingScreen, now, request.ReplaceShare);
        return Result<CallDto>.Success(CallDto.From(call));
    }
}

public class UpdateCallMediaStateCommandValidator : AbstractValidator<UpdateCallMediaStateCommand>
{
    public UpdateCallMediaStateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
