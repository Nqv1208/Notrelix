using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.RateLimiting;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>One limit: this many invitation emails in this many seconds.</summary>
public sealed class InvitationRateLimit
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

/// <summary>The invitation email limits, bound from <c>RateLimiting:Invitations</c>: one counted per workspace, one per inviter.</summary>
public sealed class InvitationRateLimitOptions
{
    public const string SectionName = "RateLimiting:Invitations";

    public InvitationRateLimit PerWorkspace { get; set; } = new() { PermitLimit = 200, WindowSeconds = 3600 };
    public InvitationRateLimit PerInviter { get; set; } = new() { PermitLimit = 60, WindowSeconds = 3600 };
}

/// <summary>Counts each invitation email (single invite, resend, each address of a bulk invite) per workspace and per inviter; refuses with 429 when a limit is over.</summary>
public interface IInvitationRateLimiter
{
    /// <exception cref="TooManyRequestsException">A limit is over.</exception>
    Task RequireAsync(Guid workspaceId, Guid inviterId, CancellationToken cancellationToken);
}

/// <summary>On the shared <see cref="IRateLimitService"/>. When the store cannot answer the call is let through and a warning is logged, as the other open policies do.</summary>
public sealed class InvitationRateLimiter(
    IRateLimitService limiter,
    IOptions<InvitationRateLimitOptions> options,
    ILogger<InvitationRateLimiter> logger) : IInvitationRateLimiter
{
    public const string PolicyName = "workspaces.invitation-email";
    public const string RuleCode = "workspaces.invitation.rate-limited";

    public async Task RequireAsync(Guid workspaceId, Guid inviterId, CancellationToken cancellationToken)
    {
        await CheckAsync("workspace", $"workspace:{workspaceId:N}", options.Value.PerWorkspace, cancellationToken);
        await CheckAsync("inviter", $"inviter:{workspaceId:N}:{inviterId:N}", options.Value.PerInviter, cancellationToken);
    }

    private async Task CheckAsync(string scope, string partition, InvitationRateLimit limit, CancellationToken cancellationToken)
    {
        RateLimitDecision decision;
        try
        {
            decision = await limiter.CheckAsync(
                PolicyName, partition, limit.PermitLimit, TimeSpan.FromSeconds(limit.WindowSeconds), RateLimitAlgorithm.SlidingWindow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Rate limit store unavailable for policy {PolicyName}; failing open", PolicyName);
            return;
        }

        if (decision.IsAllowed) return;

        throw new TooManyRequestsException(
            "Too many invitations sent. Try again shortly.", RuleCode, decision.RetryAfter ?? TimeSpan.FromSeconds(limit.WindowSeconds))
        {
            Extensions = new Dictionary<string, object?>
            {
                ["policy"] = PolicyName,
                ["scope"] = scope,
                ["limit"] = decision.Limit,
            },
        };
    }
}
