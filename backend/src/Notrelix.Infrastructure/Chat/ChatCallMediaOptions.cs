namespace Notrelix.Infrastructure.Chat;

public enum ChatCallMediaProvider
{
    /// <summary>The in-memory stand-in: tests and the mock stack. It signs nothing real.</summary>
    InMemory,

    /// <summary>A LiveKit media server, self-hosted or LiveKit Cloud (the same adapter; only the address and keys differ).</summary>
    LiveKit,
}

/// <summary>
/// Which media adapter serves <c>IChatCallMediaGateway</c> and how to reach it (<c>Chat:Calls:Media</c>, SYS-ADR-002 decision 7).
/// The client-facing address stays <c>Chat:Calls:SfuUrl</c>; <see cref="ApiUrl"/> is the server-to-server address of the same server.
/// </summary>
public sealed class ChatCallMediaOptions
{
    public const string SectionName = "Chat:Calls:Media";

    /// <summary>The key pair the dev compose profile starts LiveKit with. A staging or production host refuses it.</summary>
    public const string DevApiKey = "devkey-notrelix-local";
    public const string DevApiSecret = "dev-only-livekit-secret-never-use-outside-local-0123456789";

    public ChatCallMediaProvider Provider { get; init; } = ChatCallMediaProvider.InMemory;

    /// <summary>The media server's HTTP address for the server API (Twirp), for example <c>http://livekit:7880</c>.</summary>
    public string ApiUrl { get; init; } = "";

    public string ApiKey { get; init; } = "";

    public string ApiSecret { get; init; } = "";

    /// <summary>How long one server API call may take before the media plane counts as unreachable. Default 5 seconds.</summary>
    public int RequestTimeoutSeconds { get; init; } = 5;

    /// <summary>How long a room with nobody in it lives on the media server before it closes by itself. Default 120 seconds.</summary>
    public int RoomEmptyTimeoutSeconds { get; init; } = 120;
}

public sealed class ChatCallMediaOptionsValidator(IHostEnvironment environment) : IValidateOptions<ChatCallMediaOptions>
{
    public const int MinSecretLength = 32;

    public ValidateOptionsResult Validate(string? name, ChatCallMediaOptions options)
    {
        if (options.Provider != ChatCallMediaProvider.LiveKit) return ValidateOptionsResult.Success;

        var failures = new List<string>();
        var section = ChatCallMediaOptions.SectionName;
        if (!Uri.TryCreate(options.ApiUrl, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))
            failures.Add($"{section}:ApiUrl must be an absolute http or https address when the provider is LiveKit.");
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            failures.Add($"{section}:ApiKey is required when the provider is LiveKit.");
        if (options.ApiSecret.Length < MinSecretLength)
            failures.Add($"{section}:ApiSecret must be at least {MinSecretLength} characters when the provider is LiveKit.");
        if (options.RequestTimeoutSeconds is < 1 or > 60)
            failures.Add($"{section}:RequestTimeoutSeconds must be between 1 and 60.");
        if (options.RoomEmptyTimeoutSeconds is < 10 or > 3600)
            failures.Add($"{section}:RoomEmptyTimeoutSeconds must be between 10 and 3600.");
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing")
            && (options.ApiKey == ChatCallMediaOptions.DevApiKey || options.ApiSecret == ChatCallMediaOptions.DevApiSecret))
            failures.Add($"{section}: the local development key pair must never be used outside Development.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
