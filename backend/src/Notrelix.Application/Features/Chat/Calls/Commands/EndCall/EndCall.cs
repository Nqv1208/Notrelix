using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Calls.Commands.EndCall;

/// <summary>Ends an active call for everyone and writes the call message. In a direct call either person ends it; in a group or channel call only the starter does (the others leave). A ringing call is cancelled or declined instead (409 call-state-invalid). A stale expected version is 412; a person outside the space is NotFound; an ended call is 409 call-ended.</summary>
public record EndCallCommand(Guid WorkspaceId, Guid CallId, long ExpectedVersion)
    : ICommand<Result<CallDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.call"), CallId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class EndCallCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ChatCallEnding ending,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> options) : IRequestHandler<EndCallCommand, Result<CallDto>>
{
    public async Task<Result<CallDto>> Handle(EndCallCommand request, CancellationToken cancellationToken)
    {
        ChatCallAccess.RequireEnabled(options);
        var userId = requestContext.UserId;
        var (call, space, _) = await ChatCallAccess.MemberCallAsync(db, members, request.WorkspaceId, request.CallId, userId, cancellationToken);
        var now = clock.UtcNow;
        call.End(userId, now);
        await ending.FinishAsync(space, call, userId, cancellationToken);
        return Result<CallDto>.Success(CallDto.From(call));
    }
}

public class EndCallCommandValidator : AbstractValidator<EndCallCommand>
{
    public EndCallCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
