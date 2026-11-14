using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Tests.Features.Chat.RateLimits;

/// <summary>A Chat limiter that lets everything through and records each call, or refuses the policies it was told to.</summary>
public sealed class FakeChatRateLimiter : IChatRateLimiter
{
    private readonly HashSet<ChatRateLimitPolicy> _refused = [];

    public List<(ChatRateLimitPolicy Policy, Guid SpaceId, Guid UserId)> Calls { get; } = [];

    public void Refuse(ChatRateLimitPolicy policy) => _refused.Add(policy);

    public Task RequireAsync(ChatRateLimitPolicy policy, Guid spaceId, Guid userId, CancellationToken cancellationToken)
    {
        Calls.Add((policy, spaceId, userId));
        return _refused.Contains(policy)
            ? throw new TooManyRequestsException("Too many requests.", ChatRuleCodes.RateLimited, TimeSpan.FromSeconds(7))
            : Task.CompletedTask;
    }
}
