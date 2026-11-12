using Microsoft.Extensions.Options;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.RateLimits;

/// <summary>The Chat actions that are rate limited (UC-CHT-H08). The wire name is the policy name given to the shared limiter.</summary>
public enum ChatRateLimitPolicy
{
    Send,
    React,
    ThreadReply,
    Upload,
    Broadcast,
    Export,
}

public static class ChatRateLimitPolicies
{
    public static string NameOf(ChatRateLimitPolicy policy) => policy switch
    {
        ChatRateLimitPolicy.Send => "chat.send",
        ChatRateLimitPolicy.React => "chat.react",
        ChatRateLimitPolicy.ThreadReply => "chat.thread-reply",
        ChatRateLimitPolicy.Upload => "chat.upload",
        ChatRateLimitPolicy.Broadcast => "chat.broadcast",
        ChatRateLimitPolicy.Export => "chat.export",
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Chat rate limit policy."),
    };
}

/// <summary>One limit: this many calls in this many seconds.</summary>
public sealed class ChatRateLimit
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

/// <summary>The two limits of one policy: counted per person and counted per space.</summary>
public sealed class ChatRateLimitPair
{
    public ChatRateLimit PerPerson { get; set; } = new();
    public ChatRateLimit PerSpace { get; set; } = new();
}

/// <summary>
/// The Chat limits, bound from the <c>RateLimiting:Chat</c> section. Every policy has a default so a deployment without the section
/// is limited all the same. The limits count calls in a sliding window.
/// </summary>
public sealed class ChatRateLimitOptions
{
    public const string SectionName = "RateLimiting:Chat";

    public ChatRateLimitPair Send { get; set; } = Pair(30, 60, 300, 60);
    public ChatRateLimitPair React { get; set; } = Pair(60, 60, 600, 60);
    public ChatRateLimitPair ThreadReply { get; set; } = Pair(30, 60, 300, 60);
    public ChatRateLimitPair Upload { get; set; } = Pair(10, 60, 100, 60);
    public ChatRateLimitPair Broadcast { get; set; } = Pair(3, 60, 10, 60);

    /// <summary>An export of a person's chat data (UC-CHT-H06): the second limit counts the Workspace, not a space.</summary>
    public ChatRateLimitPair Export { get; set; } = Pair(5, 3600, 20, 3600);

    public ChatRateLimitPair For(ChatRateLimitPolicy policy) => policy switch
    {
        ChatRateLimitPolicy.Send => Send,
        ChatRateLimitPolicy.React => React,
        ChatRateLimitPolicy.ThreadReply => ThreadReply,
        ChatRateLimitPolicy.Upload => Upload,
        ChatRateLimitPolicy.Broadcast => Broadcast,
        ChatRateLimitPolicy.Export => Export,
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Chat rate limit policy."),
    };

    private static ChatRateLimitPair Pair(int person, int personWindow, int space, int spaceWindow) => new()
    {
        PerPerson = new ChatRateLimit { PermitLimit = person, WindowSeconds = personWindow },
        PerSpace = new ChatRateLimit { PermitLimit = space, WindowSeconds = spaceWindow },
    };
}

/// <summary>Counts a Chat action per person and per space and refuses it with 429 when a limit is over (UC-CHT-H08, CHT-020).</summary>
public interface IChatRateLimiter
{
    /// <exception cref="Notrelix.Application.Common.Exceptions.TooManyRequestsException">A limit of the policy is over.</exception>
    Task RequireAsync(ChatRateLimitPolicy policy, Guid spaceId, Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// The Chat limiter on the shared <see cref="IRateLimitService"/> (the one the rest of the API uses), so there is no second limiter.
/// Nobody is exempt: a Workspace Owner or Admin is counted like anyone, the broadcast limit included. When the limiter store cannot
/// answer the call is let through (fail open) and a warning is logged, as the other open policies do.
/// </summary>
public sealed class ChatRateLimiter(
    IRateLimitService limiter,
    IOptions<ChatRateLimitOptions> options,
    ILogger<ChatRateLimiter> logger) : IChatRateLimiter
{
    public async Task RequireAsync(ChatRateLimitPolicy policy, Guid spaceId, Guid userId, CancellationToken cancellationToken)
    {
        var name = ChatRateLimitPolicies.NameOf(policy);
        var limits = options.Value.For(policy);

        await CheckAsync(name, "person", $"person:{userId:N}", limits.PerPerson, cancellationToken);
        await CheckAsync(name, "space", $"space:{spaceId:N}", limits.PerSpace, cancellationToken);
    }

    private async Task CheckAsync(string policy, string scope, string partition, ChatRateLimit limit, CancellationToken cancellationToken)
    {
        RateLimitDecision decision;
        try
        {
            decision = await limiter.CheckAsync(
                policy, partition, limit.PermitLimit, TimeSpan.FromSeconds(limit.WindowSeconds), RateLimitAlgorithm.SlidingWindow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Rate limit store unavailable for policy {PolicyName}; failing open", policy);
            return;
        }

        if (decision.IsAllowed) return;

        var retryAfter = decision.RetryAfter ?? TimeSpan.FromSeconds(limit.WindowSeconds);
        throw new Notrelix.Application.Common.Exceptions.TooManyRequestsException(
            "Too many requests. Try again shortly.", ChatRuleCodes.RateLimited, retryAfter)
        {
            Extensions = new Dictionary<string, object?>
            {
                ["policy"] = policy,
                ["scope"] = scope,
                ["limit"] = decision.Limit,
            },
        };
    }
}

/// <summary>Every limit is a positive count in a positive window of at most a day.</summary>
public sealed class ChatRateLimitOptionsValidator : IValidateOptions<ChatRateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, ChatRateLimitOptions options)
    {
        var failures = new List<string>();
        foreach (var policy in Enum.GetValues<ChatRateLimitPolicy>())
        {
            var pair = options.For(policy);
            Check(failures, $"{ChatRateLimitOptions.SectionName}:{policy}:PerPerson", pair.PerPerson);
            Check(failures, $"{ChatRateLimitOptions.SectionName}:{policy}:PerSpace", pair.PerSpace);
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Check(List<string> failures, string path, ChatRateLimit limit)
    {
        if (limit.PermitLimit < 1)
            failures.Add($"{path}:PermitLimit must be at least 1.");
        if (limit.WindowSeconds is < 1 or > 86_400)
            failures.Add($"{path}:WindowSeconds must be between 1 and 86400.");
    }
}
