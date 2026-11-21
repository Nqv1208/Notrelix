using Microsoft.Extensions.Options;

namespace Notrelix.Application.Features.Chat.Calls;

/// <summary>
/// The call settings (SPEC-CHAT-MEDIA-CALLS section 8, <c>Chat:Calls</c>). <see cref="Region"/> is copied to every call the server
/// creates; a client never supplies it. <see cref="Enabled"/> switches every call operation off in an incident (503
/// <c>chat.calls.unavailable</c>). The media server address and keys arrive with the real media adapter.
/// </summary>
public sealed class ChatCallOptions
{
    public const string SectionName = "Chat:Calls";

    public bool Enabled { get; init; } = true;

    /// <summary>The address clients connect the media plane to. The fake gateway answers a placeholder.</summary>
    public string SfuUrl { get; init; } = "wss://media.invalid";

    /// <summary>How long a join token lives, in seconds. Default 60.</summary>
    public int TokenTtlSeconds { get; init; } = 60;

    /// <summary>How long a call rings before it is missed, in seconds. Default 45.</summary>
    public int RingSeconds { get; init; } = 45;

    /// <summary>How long an Active call may have nobody in it before it ends, in seconds. Default 60.</summary>
    public int EmptyCallSeconds { get; init; } = 60;

    /// <summary>The media host region (owner decision 2026-11-16: Singapore).</summary>
    public string Region { get; init; } = "ap-southeast-1";

    /// <summary>How long call logs are kept, in months. Default 12 (owner decision 2026-11-16).</summary>
    public int LogRetentionMonths { get; init; } = 12;
}

public sealed class ChatCallOptionsValidator : IValidateOptions<ChatCallOptions>
{
    public ValidateOptionsResult Validate(string? name, ChatCallOptions options)
    {
        var failures = new List<string>();
        var section = ChatCallOptions.SectionName;
        if (string.IsNullOrWhiteSpace(options.Region) || options.Region.Length > 32)
            failures.Add($"{section}:Region is required (at most 32 characters).");
        if (string.IsNullOrWhiteSpace(options.SfuUrl))
            failures.Add($"{section}:SfuUrl is required.");
        if (options.TokenTtlSeconds is < 10 or > 600)
            failures.Add($"{section}:TokenTtlSeconds must be between 10 and 600 (a missed kick must self-heal within ten minutes).");
        if (options.RingSeconds is < 5 or > 300)
            failures.Add($"{section}:RingSeconds must be between 5 and 300.");
        if (options.EmptyCallSeconds is < 5 or > 3600)
            failures.Add($"{section}:EmptyCallSeconds must be between 5 and 3600.");
        if (options.LogRetentionMonths is < 1 or > 120)
            failures.Add($"{section}:LogRetentionMonths must be between 1 and 120.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
