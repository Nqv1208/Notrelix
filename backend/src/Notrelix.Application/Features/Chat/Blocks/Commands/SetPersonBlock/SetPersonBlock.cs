using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Blocks.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Blocks.Commands.SetPersonBlock;

/// <summary>
/// The caller blocks or unblocks another person of the Workspace (UC-CHT-E06). A block stops a direct conversation between the two
/// from being started by either side: the start is NotFound (CHT-014). It does not hide channel messages, and it is idempotent.
/// Blocking yourself is 400; a person who is not in the Workspace is NotFound. Muting a direct conversation is the notification
/// level of that conversation (UC-CHT-D05, D06).
/// </summary>
public record SetPersonBlockCommand(Guid WorkspaceId, Guid UserId, bool Blocked)
    : ICommand<Result<PersonBlockDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SetPersonBlockCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<SetPersonBlockCommand, Result<PersonBlockDto>>
{
    public async Task<Result<PersonBlockDto>> Handle(SetPersonBlockCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        if (request.UserId == userId)
            throw new DomainBusinessRuleException(Notrelix.Domain.Chat.Rules.ChatRuleCodes.SelfTarget, "A person cannot block themselves.");
        var target = await members.FindAsync(request.WorkspaceId, request.UserId, cancellationToken);
        if (target is null)
            throw new NotFoundException(nameof(Space), request.UserId);

        var existing = await db.ChatBlocks.FirstOrDefaultAsync(
            b => b.WorkspaceId == request.WorkspaceId && b.BlockerId == userId && b.BlockedId == request.UserId, cancellationToken);
        if (request.Blocked && existing is null)
            db.ChatBlocks.Add(PersonBlock.Create(accountId, request.WorkspaceId, userId, request.UserId, clock.UtcNow));
        else if (!request.Blocked && existing is not null)
            db.ChatBlocks.Remove(existing);

        return Result<PersonBlockDto>.Success(new PersonBlockDto(request.UserId, request.Blocked));
    }
}

public class SetPersonBlockCommandValidator : AbstractValidator<SetPersonBlockCommand>
{
    public SetPersonBlockCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
