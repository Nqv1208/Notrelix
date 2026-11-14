using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Attachments.Services;
using Notrelix.Application.Features.Chat.Voice;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Attachments.Commands.CommitMessageAttachmentUpload;

/// <summary>
/// Step two of the two-step upload (UC-CHT-G08, two-step). It verifies that the bytes arrived, reads their first bytes to sniff the type
/// and read the image size and audio duration, applies the shared file policy and the voice rules, and records the file on the message.
/// Idempotent: a repeated commit of a completed upload answers with the same file and records nothing again. A policy refusal deletes the
/// stored object and refuses the request. An upload of another message, an expired upload or one that was never uploaded is refused.
/// </summary>
public record CommitMessageAttachmentUploadCommand(Guid WorkspaceId, Guid MessageId, Guid UploadId)
    : ICommand<Result<MessageAttachmentDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CommitMessageAttachmentUploadCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IChatMessageAttachmentTransfers transfers,
    IDateTimeProvider clock,
    IOptions<ChatVoiceOptions> voice) : IRequestHandler<CommitMessageAttachmentUploadCommand, Result<MessageAttachmentDto>>
{
    public async Task<Result<MessageAttachmentDto>> Handle(CommitMessageAttachmentUploadCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var (message, space, _) = await MessageAttachmentAccess.RequireAuthorAsync(
            db, members, request.WorkspaceId, request.MessageId, userId, tracked: true, cancellationToken);

        // The object is verified and its policy applied in the Collaboration boundary; the row is added to the unit of work, not committed.
        var commit = await transfers.CommitUploadAsync(message.Id, request.UploadId, cancellationToken);
        var stored = commit.Attachment;
        if (commit.Replayed)
            return Result<MessageAttachmentDto>.Success(MessageAttachments.ToDto(request.WorkspaceId, stored));

        if (commit.IsVoice)
        {
            // A voice refusal throws before the unit of work commits; the compensation the Collaboration commit registered removes the object.
            var voiceOptions = voice.Value;
            var maxVoiceSeconds = voiceOptions.MaxDurationSeconds;
            var maxVoiceBytes = (long)maxVoiceSeconds * voiceOptions.MaxBytesPerSecond;
            if (!stored.ContentType.StartsWith("audio/", StringComparison.Ordinal))
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(ChatRuleCodes.VoiceInvalid, "A voice message is one audio file.");
            if (commit.VerifiedDurationMs is { } verified && verified > maxVoiceSeconds * 1000L || stored.SizeBytes > maxVoiceBytes)
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(ChatRuleCodes.VoiceTooLong, $"A voice message is at most {maxVoiceSeconds} seconds long.");
        }

        // Standing is read again under the space lock: a leave, an archive or a delete that committed during the commit fails this request.
        await sequences.LockAsync(space.Id, cancellationToken);
        var live = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.Id == space.Id && s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null)
            .Select(s => new { Archived = s.ArchivedAt != null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Space), message.SpaceId);
        var stillMember = await db.ChatMemberships.AsNoTracking()
            .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId && m.LeftAt == null, cancellationToken);
        if (!stillMember)
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (live.Archived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        var current = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);
        current.EnsureAttachable();

        message.RecordAttachmentAdded(
            userId, stored.Id, stored.FileName, stored.SizeBytes, stored.ContentType, stored.UploadedBy, stored.CreatedAt, clock.UtcNow);

        return Result<MessageAttachmentDto>.Success(MessageAttachments.ToDto(request.WorkspaceId, stored));
    }
}

public class CommitMessageAttachmentUploadCommandValidator : AbstractValidator<CommitMessageAttachmentUploadCommand>
{
    public CommitMessageAttachmentUploadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UploadId).NotEmpty();
    }
}
