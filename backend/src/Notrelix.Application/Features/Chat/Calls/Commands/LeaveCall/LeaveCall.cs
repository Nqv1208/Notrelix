using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Calls.Commands.LeaveCall;

/// <summary>Leaves an active call. Leaving a direct call ends it and writes the call message; leaving a group or channel call keeps it alive, and the worker ends it after 60 seconds with nobody in it. A person who is not in the call changes nothing. A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record LeaveCallCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion)
    : ICommand<Result<CallDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.call"), CallId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class LeaveCallCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ChatCallEnding ending,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<LeaveCallCommand, Result<CallDto>>
{
    public async Task<Result<CallDto>> Handle(LeaveCallCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        var (call, space, _) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var now = clock.UtcNow;
        call.Leave(userId, now);
        await ending.FinishAsync(space, call, userId, cancellationToken);
        return Result<CallDto>.Success(CallDto.From(call));
    }
}

public class LeaveCallCommandValidator : AbstractValidator<LeaveCallCommand>
{
    public LeaveCallCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
