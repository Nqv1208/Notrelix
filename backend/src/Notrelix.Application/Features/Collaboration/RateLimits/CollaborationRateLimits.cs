using Microsoft.Extensions.Options;

namespace Notrelix.Application.Features.Collaboration.RateLimits;

/// <summary>The Collaboration writes that are rate limited (UC-COL-H05, lead decision L15).</summary>
public enum CollaborationRateLimitPolicy
{
    Comment,
    React,
    Upload,
}

/// <summary>One limit: this many calls in this many seconds.</summary>
public sealed class CollaborationRateLimit
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

/// <summary>
/// The Collaboration limits, bound from <c>RateLimiting:Collaboration</c>. Each counts one person in one Workspace in a sliding
/// window; the defaults are L15 (30 comments, 60 reactions, 20 uploads a minute), so a deployment without the section is limited
/// all the same.
/// </summary>
public sealed class CollaborationRateLimitOptions
{
    public const string SectionName = "RateLimiting:Collaboration";

    public CollaborationRateLimit Comment { get; set; } = Limit(30, 60);
    public CollaborationRateLimit React { get; set; } = Limit(60, 60);
    public CollaborationRateLimit Upload { get; set; } = Limit(20, 60);

    public CollaborationRateLimit For(CollaborationRateLimitPolicy policy) => policy switch
    {
        CollaborationRateLimitPolicy.Comment => Comment,
        CollaborationRateLimitPolicy.React => React,
        CollaborationRateLimitPolicy.Upload => Upload,
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Collaboration rate limit policy."),
    };

    public static string NameOf(CollaborationRateLimitPolicy policy) => policy switch
    {
        CollaborationRateLimitPolicy.Comment => "collaboration.comment",
        CollaborationRateLimitPolicy.React => "collaboration.react",
        CollaborationRateLimitPolicy.Upload => "collaboration.upload",
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Collaboration rate limit policy."),
    };

    private static CollaborationRateLimit Limit(int permits, int seconds) => new() { PermitLimit = permits, WindowSeconds = seconds };
}

/// <summary>Counts a Collaboration write for one person in one Workspace and refuses it with 429 when the limit is over.</summary>
public interface ICollaborationRateLimiter
{
    /// <exception cref="Notrelix.Application.Common.Exceptions.TooManyRequestsException">The limit of the policy is over.</exception>
    Task RequireAsync(CollaborationRateLimitPolicy policy, Guid workspaceId, Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// The Collaboration limiter on the shared <see cref="IRateLimitService"/>, so there is no second limiter. Nobody is exempt. When the
/// limiter store cannot answer, the call is let through (fail open) and a warning is logged, as the Chat limits do. A replay of an
/// idempotent create is answered by the pipeline before the handler, so it is not counted again.
/// </summary>
public sealed class CollaborationRateLimiter(
    IRateLimitService limiter,
    IOptions<CollaborationRateLimitOptions> options,
    ILogger<CollaborationRateLimiter> logger) : ICollaborationRateLimiter
{
    public const string RuleCode = "rate-limited";

    public async Task RequireAsync(CollaborationRateLimitPolicy policy, Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var name = CollaborationRateLimitOptions.NameOf(policy);
        var limit = options.Value.For(policy);

        RateLimitDecision decision;
        try
        {
            decision = await limiter.CheckAsync(
                name, $"person:{userId:N}:workspace:{workspaceId:N}", limit.PermitLimit, TimeSpan.FromSeconds(limit.WindowSeconds),
                RateLimitAlgorithm.SlidingWindow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Rate limit store unavailable for policy {PolicyName}; failing open", name);
            return;
        }

        if (decision.IsAllowed) return;

        throw new Notrelix.Application.Common.Exceptions.TooManyRequestsException(
            "Too many requests. Try again shortly.", RuleCode, decision.RetryAfter ?? TimeSpan.FromSeconds(limit.WindowSeconds))
        {
            Extensions = new Dictionary<string, object?> { ["policy"] = name, ["limit"] = decision.Limit },
        };
    }
}
