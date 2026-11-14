namespace Notrelix.API.Contracts.Chat.Requests;

/// <summary>
/// The declared facts of a two-step upload (UC-CHT-G08). <c>sizeBytes</c> and <c>contentType</c> are the client's declaration: the signed
/// PUT is bound to them, and the commit verifies the stored object against them and sniffs the real type. The voice fields mirror the
/// streaming upload (UC-CHT-I04).
/// </summary>
public sealed record CreateChatAttachmentUploadRequest(
    string FileName,
    long SizeBytes,
    string ContentType,
    bool? Voice = null,
    int? DurationHintMs = null,
    int[]? Waveform = null);
