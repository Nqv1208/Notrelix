using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations;

/// <summary>Invitation emails are limited per workspace and per inviter on the shared limiter, and a denied permit is a 429 with the wait the store gave.</summary>
public class InvitationRateLimiterTests
{
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Inviter = Guid.NewGuid();

    private readonly Mock<IRateLimitService> _store = new();
    private readonly List<string> _partitions = [];
    private readonly HashSet<string> _denied = [];

    private InvitationRateLimiter Limiter()
    {
        _store.Setup(s => s.CheckAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<RateLimitAlgorithm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string partition, int limit, TimeSpan _, RateLimitAlgorithm _, CancellationToken _) =>
            {
                _partitions.Add(partition);
                var denied = _denied.Contains(partition.Split(':')[0]);
                return new RateLimitDecision(!denied, limit, denied ? 0 : 1, denied ? TimeSpan.FromSeconds(42) : null, DateTimeOffset.UnixEpoch);
            });
        return new InvitationRateLimiter(_store.Object, Options.Create(new InvitationRateLimitOptions()), NullLogger<InvitationRateLimiter>.Instance);
    }

    [Fact]
    public async Task Counts_the_workspace_and_the_inviter_inside_that_workspace()
    {
        await Limiter().RequireAsync(Workspace, Inviter, CancellationToken.None);

        _partitions.Should().Equal($"workspace:{Workspace:N}", $"inviter:{Workspace:N}:{Inviter:N}");
    }

    [Theory]
    [InlineData("workspace")]
    [InlineData("inviter")]
    public async Task A_limit_that_is_over_refuses_with_the_scope_and_the_wait(string scope)
    {
        _denied.Add(scope);

        var error = await FluentActions.Awaiting(() => Limiter().RequireAsync(Workspace, Inviter, CancellationToken.None))
            .Should().ThrowAsync<TooManyRequestsException>();

        error.Which.RuleCode.Should().Be(InvitationRateLimiter.RuleCode);
        error.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(42));
        error.Which.Extensions["scope"].Should().Be(scope);
    }

    [Fact]
    public async Task A_store_that_cannot_answer_lets_the_call_through()
    {
        _store.Setup(s => s.CheckAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<RateLimitAlgorithm>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));
        var limiter = new InvitationRateLimiter(_store.Object, Options.Create(new InvitationRateLimitOptions()), NullLogger<InvitationRateLimiter>.Instance);

        await FluentActions.Awaiting(() => limiter.RequireAsync(Workspace, Inviter, CancellationToken.None)).Should().NotThrowAsync();
    }
}
