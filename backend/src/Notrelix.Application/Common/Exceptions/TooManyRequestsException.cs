namespace Notrelix.Application.Common.Exceptions;

/// <summary>
/// A handler refused the call because a limit of the feature was reached (a Chat rate limit or a space's slow mode). The API answers
/// 429 with Retry-After and the stable rule code, so a client shows a countdown and keeps what the person typed.
/// </summary>
public class TooManyRequestsException : Exception
{
    /// <summary>The stable rule that refused the call, additive to the error category (for example <c>rate-limited</c> or <c>slow-mode</c>).</summary>
    public string RuleCode { get; }

    /// <summary>How long the caller waits before the same call can succeed.</summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>Extra members of the public problem (the policy and who the limit is counted for).</summary>
    public IReadOnlyDictionary<string, object?> Extensions { get; init; } = new Dictionary<string, object?>();

    public TooManyRequestsException(string message, string ruleCode, TimeSpan retryAfter) : base(message)
    {
        RuleCode = ruleCode;
        RetryAfter = retryAfter;
    }
}
