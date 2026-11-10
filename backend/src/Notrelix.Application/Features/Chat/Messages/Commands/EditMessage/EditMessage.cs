using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Messages.Commands.EditMessage;

/// <summary>
/// Replaces a message's text (UC-CHT-B04). Only the author edits. A stale expectedVersion is 412; a message the caller
/// cannot see is the same NotFound as a missing one. The sequence never changes.
/// </summary>
public record EditMessageCommand(Guid WorkspaceId, Guid MessageId, string Content, long ExpectedVersion)
    : ICommand<Result<MessageDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.message"), MessageId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class EditMessageCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<EditMessageCommand, Result<MessageDto>>
{
    public async Task<Result<MessageDto>> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        // Tracked on purpose: the data session binds the expected version to this loaded aggregate.
        var message = await db.ChatMessages.FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        if (message.AuthorId != userId)
            throw new ForbiddenException("Only the author can edit this message.");
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        message.Edit(request.Content, userId, clock.UtcNow);
        return Result<MessageDto>.Success(MessageDto.From(message));
    }
}

public class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Content)
            .NotNull()
            .Must(content => content is not null && content.Trim().Length is >= 1 and <= ChatLimits.MessageMaxLength)
            .WithMessage($"A message has 1 to {ChatLimits.MessageMaxLength} characters after trim.");
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
