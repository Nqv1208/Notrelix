using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Calls.Commands.CancelCall;

/// <summary>The starter hangs up a ringing call before anyone answered: it ends as Cancelled and writes the call message. Anyone else is 403 call-not-participant. A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record CancelCallCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion)
    : ICommand<Result<CallDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.call"), CallId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CancelCallCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ChatCallEnding ending,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<CancelCallCommand, Result<CallDto>>
{
    public async Task<Result<CallDto>> Handle(CancelCallCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        var (call, space, _) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var now = clock.UtcNow;
        call.Cancel(userId, now);
        await ending.FinishAsync(space, call, userId, cancellationToken);
        return Result<CallDto>.Success(CallDto.From(call));
    }
}

public class CancelCallCommandValidator : AbstractValidator<CancelCallCommand>
{
    public CancelCallCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
