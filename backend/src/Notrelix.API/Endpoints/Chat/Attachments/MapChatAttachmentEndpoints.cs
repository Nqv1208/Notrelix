using Microsoft.Extensions.Options;
using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Endpoints.Collaboration.Attachments;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;
using Notrelix.API.Uploads;
using Notrelix.Application.Features.Chat.Attachments.Commands.CommitMessageAttachmentUpload;
using Notrelix.Application.Features.Chat.Attachments.Commands.CreateMessageAttachmentUpload;
using Notrelix.Application.Features.Chat.Attachments.Commands.DeleteMessageAttachment;
using Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentUrl;
using Notrelix.Application.Features.Chat.Attachments.Commands.UploadMessageAttachment;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentContent;
using Notrelix.Application.Features.Chat.Attachments.Queries.ListMessageAttachments;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;

namespace Notrelix.API.Endpoints.Chat.Attachments;

/// <summary>
/// Files on a message (UC-CHT-G08, CHT-012). The upload, the file policy and the download shape are the shared attachment flow's, under
/// the target kind chat.message; Chat decides who may attach (an active member, the author) and who may read (a reader of the space).
/// There is no public or unauthenticated address: every download is permission checked.
/// </summary>
public static class MapChatAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapChatAttachmentRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Attachments")
            .WithOpenApi();

        group.MapWorkspaceGet("/messages/{messageId:guid}/attachments", ListAsync)
            .WithName("Chat.Attachments.List")
            .WithSummary("List the files of a message")
            .WithDescription("Oldest first. Each file carries its name, size, content type and a downloadPath that streams the bytes with a permission check on every request. A message the caller cannot see, or a private channel the caller is not a member of, is 404. A deleted message has no files.")
            .Produces<List<MessageAttachmentDto>>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePost("/messages/{messageId:guid}/attachments", UploadAsync)
            .WithIdempotencyKey()
            .WithName("Chat.Attachments.Upload")
            .WithSummary("Attach a file to your own message (multipart/form-data, streamed)")
            .WithDescription("Only the author of the message, as an active member of its space, attaches. Optional small fields before the file part make it a voice message (voice=true, durationMs hint, waveform as comma-separated peaks; UC-CHT-I04): one audio file within the Workspace voice limit, else 413 voice-too-long or 400 voice-invalid. The shared file policy applies: a blocked extension or an executable file is 415 before any byte is stored, a file over the size cap is 413, a message with the maximum number of files is 409 attachment.limit_exceeded; an empty file is 400. A system or deleted message is 409; an archived channel is 409 channel-archived; another person's message is 403; a space the caller is not a member of is 404. An upload is counted per person and per space (chat.upload): over the limit the answer is 429 with Retry-After and ruleCode rate-limited, before any byte is stored.")
            .Accepts<UploadChatAttachmentForm>("multipart/form-data")
            .Produces<MessageAttachmentDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict,
                StatusCodes.Status413PayloadTooLarge,
                StatusCodes.Status415UnsupportedMediaType,
                StatusCodes.Status429TooManyRequests);

        group.MapWorkspaceGet("/messages/{messageId:guid}/attachments/{attachmentId:guid}/content", GetContentAsync)
            .WithName("Chat.Attachments.GetContent")
            .WithSummary("Download a file of a message (authorised, streamed)")
            .WithDescription("Permission checked on every request: only a reader of the space opens it. The file must belong to this message; any other attachment id is 404. Inline only for png, jpeg, gif and webp; everything else downloads as an attachment. Never cacheable.")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePost("/messages/{messageId:guid}/attachments/uploads", CreateUploadAsync)
            .WithName("Chat.Attachments.CreateUpload")
            .WithSummary("Start a two-step upload of a file to your own message (pre-signed PUT)")
            .WithDescription("Step one of the direct upload. The same authorization as the streaming upload applies, and the declared name, size and type are checked against the shared file policy. The answer (201) holds a pending upload id, a short-lived pre-signed PUT for the bytes (bound to contentType and contentLength) and the commit path. No file exists until the bytes are uploaded and the commit succeeds. When the storage provider cannot issue pre-signed URLs the answer is 409 with ruleCode storage-direct-upload-unsupported: the client then uses the streaming upload. Voice fields follow the streaming upload. A blocked type is 415, an oversize declaration 413, a message at its file limit 400, and a rate-limited sender 429.")
            .Accepts<CreateChatAttachmentUploadRequest>("application/json")
            .Produces<MessageAttachmentUploadDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict,
                StatusCodes.Status413PayloadTooLarge,
                StatusCodes.Status415UnsupportedMediaType,
                StatusCodes.Status429TooManyRequests);

        group.MapWorkspacePost("/messages/{messageId:guid}/attachments/uploads/{uploadId:guid}/commit", CommitUploadAsync)
            .WithName("Chat.Attachments.CommitUpload")
            .WithSummary("Commit a two-step upload: verify the stored bytes and attach the file")
            .WithDescription("Step two of the direct upload. Verifies the object the client uploaded, sniffs its type and reads the image size and audio duration from its first bytes, then applies the full file policy and the voice rules, and records the file on the message (201). Idempotent: a repeated commit of the same upload returns the same file and records nothing again. A policy refusal deletes the uploaded object and answers with the streaming upload's code (415, 413, 400). An upload that was never uploaded, a size that does not match its declaration, or an upload of another message is 409 or 404; an expired upload is 409 with ruleCode attachment-upload-expired.")
            .Produces<MessageAttachmentDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict,
                StatusCodes.Status413PayloadTooLarge,
                StatusCodes.Status415UnsupportedMediaType);

        group.MapWorkspaceGet("/messages/{messageId:guid}/attachments/{attachmentId:guid}/url", GetUrlAsync)
            .WithName("Chat.Attachments.GetUrl")
            .WithSummary("Get a short-lived pre-signed download URL for a file of a message")
            .WithDescription("Authorised like the content read: only a reader of the space gets a URL, and the file must belong to this message (any other id is 404). The answer (200) holds the URL and its expiry (five minutes by default). The URL is a secret for that window and must not be stored or shared. When the storage provider cannot issue pre-signed URLs the answer is 409 with ruleCode storage-direct-download-unsupported and the client uses the content endpoint instead.")
            .Produces<MessageAttachmentUrlDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        group.MapWorkspaceDelete("/messages/{messageId:guid}/attachments/{attachmentId:guid}", DeleteAsync)
            .WithName("Chat.Attachments.Delete")
            .WithSummary("Remove a file from your own message")
            .WithDescription("Only the author removes a file, while the channel is not archived. A file that is not on this message is 404.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(Guid workspaceId, Guid messageId, ISender sender)
    {
        var result = await sender.Send(new ListMessageAttachmentsQuery(workspaceId, messageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> UploadAsync(
        Guid workspaceId,
        Guid messageId,
        HttpContext httpContext,
        ISender sender,
        IOptions<AttachmentUploadOptions> options,
        CancellationToken cancellationToken)
    {
        var part = await MultipartUpload.ReadFilePartAsync(
            httpContext,
            options.Value.MaxFileSizeBytes,
            ErrorCodes.AttachmentUnsupportedMediaType,
            ErrorCodes.AttachmentPayloadTooLarge,
            cancellationToken,
            ChatVoiceUploadFields.Names);
        if (part.Failure is not null)
            return part.Failure;

        if (!ChatVoiceUploadFields.TryParse(part.Fields, out var voice, out var voiceError))
            return EndpointExtensions.InvalidInput(voiceError!);

        var result = await sender.Send(
            new UploadMessageAttachmentCommand(workspaceId, messageId, part.FileName, part.Body!, voice.Voice, voice.DurationHintMs, voice.Waveform),
            cancellationToken);
        if (!result.Succeeded)
            return result.ToApiResult();

        return Results.Created(result.Data!.DownloadPath, result.Data);
    }

    private static async Task<IResult> GetContentAsync(
        Guid workspaceId,
        Guid messageId,
        Guid attachmentId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMessageAttachmentContentQuery(workspaceId, messageId, attachmentId), cancellationToken);
        if (!result.Succeeded)
            return result.ToApiResult();

        var content = result.Data!;
        return AttachmentContentResponse.Create(
            httpContext, new AttachmentContentDto(content.Content, content.FileName, content.ContentType, content.SizeBytes));
    }

    private static async Task<IResult> CreateUploadAsync(
        Guid workspaceId, Guid messageId, CreateChatAttachmentUploadRequest body, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateMessageAttachmentUploadCommand(
            workspaceId, messageId, body.FileName, body.SizeBytes, body.ContentType, body.Voice ?? false, body.DurationHintMs, body.Waveform),
            cancellationToken);
        if (!result.Succeeded)
            return result.ToApiResult();

        return Results.Created(result.Data!.CommitPath, result.Data);
    }

    private static async Task<IResult> CommitUploadAsync(
        Guid workspaceId, Guid messageId, Guid uploadId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CommitMessageAttachmentUploadCommand(workspaceId, messageId, uploadId), cancellationToken);
        if (!result.Succeeded)
            return result.ToApiResult();

        return Results.Created(result.Data!.DownloadPath, result.Data);
    }

    private static async Task<IResult> GetUrlAsync(
        Guid workspaceId, Guid messageId, Guid attachmentId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMessageAttachmentUrlQuery(workspaceId, messageId, attachmentId), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(Guid workspaceId, Guid messageId, Guid attachmentId, ISender sender)
    {
        var result = await sender.Send(new DeleteMessageAttachmentCommand(workspaceId, messageId, attachmentId));
        return result.ToNoContentResult();
    }
}
