using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Calls.Commands.UpdateCallMediaState;

/// <summary>Sets the caller's own microphone, camera and screen share in a live call (PUT participants/me). A voice call carries no camera (400 call-media-not-allowed); a caller who is not in the call is 403 call-not-participant. The same values change nothing. A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record UpdateCallMediaStateCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion, bool AudioOn, bool VideoOn, bool SharingScreen)
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
        var (call, space, _) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var now = clock.UtcNow;
        call.UpdateMediaState(userId, request.AudioOn, request.VideoOn, request.SharingScreen, now);
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
