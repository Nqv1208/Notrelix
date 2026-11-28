using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.Application.Features.Collaboration.RateLimits;

namespace Notrelix.Application.Tests.Features.Collaboration.RateLimits;

/// <summary>UC-COL-H05 / L15: comments, reactions and uploads are counted for one person in one Workspace on the shared limiter.</summary>
public class CollaborationRateLimiterTests
{
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();

    private readonly Mock<IRateLimitService> _store = new();
    private readonly List<(string Policy, string Partition, int Limit, TimeSpan Window)> _checks = [];
    private bool _denied;

    private CollaborationRateLimiter Limiter()
    {
        _store.Setup(s => s.CheckAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<RateLimitAlgorithm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string policy, string partition, int limit, TimeSpan window, RateLimitAlgorithm _, CancellationToken _) =>
            {
                _checks.Add((policy, partition, limit, window));
                return new RateLimitDecision(!_denied, limit, _denied ? 0 : 1, _denied ? TimeSpan.FromSeconds(17) : null, DateTimeOffset.UnixEpoch);
            });
        return new CollaborationRateLimiter(_store.Object, Options.Create(new CollaborationRateLimitOptions()), NullLogger<CollaborationRateLimiter>.Instance);
    }

    [Theory]
    [InlineData(CollaborationRateLimitPolicy.Comment, "collaboration.comment", 30)]
    [InlineData(CollaborationRateLimitPolicy.React, "collaboration.react", 60)]
    [InlineData(CollaborationRateLimitPolicy.Upload, "collaboration.upload", 20)]
    public async Task Each_policy_counts_one_person_in_one_workspace_per_minute_at_the_L15_limit(CollaborationRateLimitPolicy policy, string name, int limit)
    {
        await Limiter().RequireAsync(policy, Workspace, User, CancellationToken.None);

        _checks.Should().ContainSingle().Which.Should().Be((name, $"person:{User:N}:workspace:{Workspace:N}", limit, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Over_the_limit_is_refused_with_the_wait_the_store_gave()
    {
        _denied = true;

        var error = await FluentActions.Awaiting(() => Limiter().RequireAsync(CollaborationRateLimitPolicy.Comment, Workspace, User, CancellationToken.None))
            .Should().ThrowAsync<TooManyRequestsException>();

        error.Which.RuleCode.Should().Be("rate-limited");
        error.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(17));
        error.Which.Extensions["policy"].Should().Be("collaboration.comment");
    }

    [Fact]
    public async Task When_the_store_cannot_answer_the_write_goes_through()
    {
        _store.Setup(s => s.CheckAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<RateLimitAlgorithm>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store down"));
        var limiter = new CollaborationRateLimiter(_store.Object, Options.Create(new CollaborationRateLimitOptions()), NullLogger<CollaborationRateLimiter>.Instance);

        await limiter.Invoking(l => l.RequireAsync(CollaborationRateLimitPolicy.Upload, Workspace, User, CancellationToken.None)).Should().NotThrowAsync();
    }
}
