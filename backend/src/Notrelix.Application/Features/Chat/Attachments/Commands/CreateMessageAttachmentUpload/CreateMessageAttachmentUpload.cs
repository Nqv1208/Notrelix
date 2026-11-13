using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Attachments.Services;
using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Application.Features.Chat.Voice;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Attachments.Commands.CreateMessageAttachmentUpload;

/// <summary>
/// Step one of the two-step upload of a file on the caller's own message (UC-CHT-G08, two-step). The same authorization as the streaming
/// upload (an active member, the author, a live unarchived channel, the Attach permission, the upload rate limit) runs first, then the
/// declared name, size and type are checked against the shared policy, and a pending upload is recorded with a signed PUT. No byte is
/// stored here and no attachment row is written. Refused (409 storage-direct-upload-unsupported) when the storage provider cannot
/// issue pre-signed URLs; the caller then uses the streaming upload.
/// </summary>
public record CreateMessageAttachmentUploadCommand(
    Guid WorkspaceId,
    Guid MessageId,
    string FileName,
    long SizeBytes,
    string ContentType,
    bool Voice = false,
    int? DurationHintMs = null,
    int[]? Waveform = null)
    : ICommand<Result<MessageAttachmentUploadDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateMessageAttachmentUploadCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatMessageAttachmentTransfers transfers,
    IChatRateLimiter rateLimits,
    IOptions<ChatVoiceOptions> voice) : IRequestHandler<CreateMessageAttachmentUploadCommand, Result<MessageAttachmentUploadDto>>
{
    public async Task<Result<MessageAttachmentUploadDto>> Handle(CreateMessageAttachmentUploadCommand request, CancellationToken cancellationToken)
    {
        if (!transfers.SupportsPresignedUrls)
            throw new ConflictException("The storage provider does not support direct uploads.", "storage-direct-upload-unsupported");

        var userId = requestContext.UserId;
        var (message, space, caller) = await MessageAttachmentAccess.RequireAuthorAsync(
            db, members, request.WorkspaceId, request.MessageId, userId, tracked: false, cancellationToken);

        // An upload is counted per person and per space before any URL is issued (UC-CHT-H08).
        await rateLimits.RequireAsync(ChatRateLimitPolicy.Upload, space.Id, userId, cancellationToken);

        var voiceOptions = voice.Value;
        var maxVoiceBytes = (long)voiceOptions.MaxDurationSeconds * voiceOptions.MaxBytesPerSecond;
        ChatMessageAudioUpload? audio = null;
        if (request.Voice)
        {
            // The declared facts are checked here; the commit checks the stored bytes again.
            if (!request.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(ChatRuleCodes.VoiceInvalid, "A voice message is one audio file.");
            if (request.SizeBytes > maxVoiceBytes)
                throw new PayloadTooLargeException(maxVoiceBytes, ChatRuleCodes.VoiceTooLong);

            audio = new ChatMessageAudioUpload(
                Math.Clamp(request.DurationHintMs ?? 0, 0, ChatLimits.VoiceDurationHintMaxMs),
                request.Waveform?.Select(peak => (byte)peak).ToArray(),
                maxVoiceBytes);
        }

        var ticket = await transfers.CreateUploadAsync(
            message.Id, request.FileName, request.SizeBytes, request.ContentType, audio, cancellationToken);

        var dto = new MessageAttachmentUploadDto(
            ticket.UploadId,
            ticket.Put.Method,
            ticket.Put.Url,
            ticket.Put.ContentType,
            ticket.Put.ContentLength,
            ticket.Put.ExpiresAt,
            MessageAttachments.CommitPath(request.WorkspaceId, message.Id, ticket.UploadId));
        return Result<MessageAttachmentUploadDto>.Success(dto);
    }
}

public class CreateMessageAttachmentUploadCommandValidator : AbstractValidator<CreateMessageAttachmentUploadCommand>
{
    public CreateMessageAttachmentUploadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Waveform)
            .Must(peaks => peaks is null || peaks.Length is >= 1 and <= FileMetadata.MaxWaveformPeaks && peaks.All(peak => peak is >= 0 and <= 255))
            .WithMessage($"A waveform has 1 to {FileMetadata.MaxWaveformPeaks} peaks, each 0 to 255.");
    }
}
