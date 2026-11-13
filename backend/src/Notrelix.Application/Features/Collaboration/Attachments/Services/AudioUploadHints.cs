namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

using Notrelix.Application.Features.Collaboration.Attachments.DTOs;

/// <summary>
/// The voice facts a caller passes with an audio upload (UC-CHT-I04). The duration hint and the waveform are client data: the duration is
/// used only when the container header does not carry one, clamped; the waveform is validated by the caller. <see cref="MaxBytes"/> is the
/// server-side ceiling for the stored bytes of the upload, so a voice message of any declared length stays within the ceiling.
/// </summary>
public sealed record AudioUploadHints(int? DurationHintMs, byte[]? Waveform, long MaxBytes);

/// <summary>
/// What an upload stored, with the facts the caller needs. <see cref="VerifiedDurationMs"/> is the duration read from the stored header,
/// null when the header does not carry one; only it may be used for a policy decision.
/// </summary>
public sealed record UploadedFile(AttachmentDto Attachment, int? DurationMs, int? VerifiedDurationMs, byte[]? Waveform);
