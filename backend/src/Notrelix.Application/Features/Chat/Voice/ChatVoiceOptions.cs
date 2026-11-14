using Microsoft.Extensions.Options;

namespace Notrelix.Application.Features.Chat.Voice;

/// <summary>
/// The voice message limits (UC-CHT-I04, SYS-ADR-002 decision 4). <see cref="MaxDurationSeconds"/> is the length a voice message may
/// have (owner decision, proposed 300). <see cref="MaxBytesPerSecond"/> is the byte ceiling per second of that length: the stored file of a
/// voice message may not exceed <c>MaxDurationSeconds * MaxBytesPerSecond</c>. The ceiling is enforced on the stored bytes, so a header or a
/// client that understates the length cannot carry a longer recording past the limit.
/// </summary>
public sealed class ChatVoiceOptions
{
    public const string SectionName = "Chat:Voice";

    /// <summary>The longest voice message, in seconds. Default 300 (5 minutes).</summary>
    public int MaxDurationSeconds { get; init; } = 300;

    /// <summary>The byte ceiling per second of voice. Default 16000 (128 kbit/s), well above the bitrate the browser recorder asks for.</summary>
    public int MaxBytesPerSecond { get; init; } = 16_000;
}

public sealed class ChatVoiceOptionsValidator : IValidateOptions<ChatVoiceOptions>
{
    public const int MaxDurationSecondsLimit = 3600;

    public ValidateOptionsResult Validate(string? name, ChatVoiceOptions options)
    {
        var failures = new List<string>();
        if (options.MaxDurationSeconds is < 1 or > MaxDurationSecondsLimit)
            failures.Add($"{ChatVoiceOptions.SectionName}:MaxDurationSeconds must be between 1 and {MaxDurationSecondsLimit}.");
        if (options.MaxBytesPerSecond is < 1000 or > 64_000)
            failures.Add($"{ChatVoiceOptions.SectionName}:MaxBytesPerSecond must be between 1000 and 64000.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
