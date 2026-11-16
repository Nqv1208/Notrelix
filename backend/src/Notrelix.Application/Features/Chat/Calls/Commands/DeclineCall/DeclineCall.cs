using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Calls.Commands.DeclineCall;

/// <summary>Declines a ringing call. A direct call ends as Declined and writes the call message; in a group or channel call the call keeps ringing for the others and nothing changes. The starter cancels instead (403 call-not-participant). A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record DeclineCallCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion)
    : ICommand<Result<CallDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.call"), CallId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DeclineCallCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ChatCallEnding ending,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<DeclineCallCommand, Result<CallDto>>
{
    public async Task<Result<CallDto>> Handle(DeclineCallCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        var (call, space, _) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var now = clock.UtcNow;
        call.Decline(userId, now);
        await ending.FinishAsync(space, call, userId, cancellationToken);
        return Result<CallDto>.Success(CallDto.From(call));
    }
}

public class DeclineCallCommandValidator : AbstractValidator<DeclineCallCommand>
{
    public DeclineCallCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
