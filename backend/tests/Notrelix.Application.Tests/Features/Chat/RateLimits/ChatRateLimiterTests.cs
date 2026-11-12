using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Tests.Features.Chat.RateLimits;

/// <summary>UC-CHT-H08 / CHT-020: the Chat limits ride on the shared limiter, per person and per space, with the policy names the use case names.</summary>
public class ChatRateLimiterTests
{
    private static readonly Guid Space = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();

    private readonly Mock<IRateLimitService> _store = new();
    private readonly List<(string Policy, string Partition, int Limit, TimeSpan Window)> _checks = [];
    private readonly HashSet<string> _denied = [];

    private ChatRateLimiter Limiter(ChatRateLimitOptions? options = null)
    {
        _store.Setup(s => s.CheckAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<RateLimitAlgorithm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string policy, string partition, int limit, TimeSpan window, RateLimitAlgorithm _, CancellationToken _) =>
            {
                _checks.Add((policy, partition, limit, window));
                var denied = _denied.Contains(partition.Split(':')[0]);
                return new RateLimitDecision(!denied, limit, denied ? 0 : 1, denied ? TimeSpan.FromSeconds(42) : null, DateTimeOffset.UnixEpoch);
            });
        return new ChatRateLimiter(_store.Object, Options.Create(options ?? new ChatRateLimitOptions()), NullLogger<ChatRateLimiter>.Instance);
    }

    [Theory]
    [InlineData(ChatRateLimitPolicy.Send, "chat.send")]
    [InlineData(ChatRateLimitPolicy.React, "chat.react")]
    [InlineData(ChatRateLimitPolicy.ThreadReply, "chat.thread-reply")]
    [InlineData(ChatRateLimitPolicy.Upload, "chat.upload")]
    [InlineData(ChatRateLimitPolicy.Broadcast, "chat.broadcast")]
    [InlineData(ChatRateLimitPolicy.Export, "chat.export")]
    public async Task Every_policy_counts_the_person_and_the_space_under_its_chat_name(ChatRateLimitPolicy policy, string name)
    {
        await Limiter().RequireAsync(policy, Space, User, CancellationToken.None);

        _checks.Select(c => c.Policy).Should().OnlyContain(p => p == name);
        _checks.Select(c => c.Partition).Should().Equal($"person:{User:N}", $"space:{Space:N}");
    }

    [Fact]
    public async Task A_person_over_the_limit_is_refused_with_the_wait_the_store_gave_and_the_scope()
    {
        _denied.Add("person");

        var error = await FluentActions.Awaiting(() => Limiter().RequireAsync(ChatRateLimitPolicy.Send, Space, User, CancellationToken.None))
            .Should().ThrowAsync<TooManyRequestsException>();

        error.Which.RuleCode.Should().Be(ChatRuleCodes.RateLimited);
        error.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(42));
        error.Which.Extensions["scope"].Should().Be("person");
        error.Which.Extensions["policy"].Should().Be("chat.send");
    }

    [Fact]
    public async Task A_space_over_the_limit_refuses_everyone_in_it()
    {
        _denied.Add("space");

        var error = await FluentActions.Awaiting(() => Limiter().RequireAsync(ChatRateLimitPolicy.React, Space, User, CancellationToken.None))
            .Should().ThrowAsync<TooManyRequestsException>();

        error.Which.Extensions["scope"].Should().Be("space");
    }

    [Fact]
    public async Task The_broadcast_limit_is_lower_than_the_send_limit_and_nobody_is_exempt()
    {
        var defaults = new ChatRateLimitOptions();

        defaults.Broadcast.PerPerson.PermitLimit.Should().BeLessThan(defaults.Send.PerPerson.PermitLimit);
        // The limiter takes no role: an Owner or Admin goes through the same two checks.
        await Limiter().RequireAsync(ChatRateLimitPolicy.Broadcast, Space, User, CancellationToken.None);
        _checks.Should().HaveCount(2);
    }

    [Fact]
    public async Task When_the_limiter_store_is_down_the_call_goes_through()
    {
        _store.Setup(s => s.CheckAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<RateLimitAlgorithm>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis is down"));
        var limiter = new ChatRateLimiter(_store.Object, Options.Create(new ChatRateLimitOptions()), NullLogger<ChatRateLimiter>.Instance);

        await FluentActions.Awaiting(() => limiter.RequireAsync(ChatRateLimitPolicy.Send, Space, User, CancellationToken.None)).Should().NotThrowAsync();
    }

    [Fact]
    public void The_options_validator_wants_a_positive_count_in_a_positive_window()
    {
        var options = new ChatRateLimitOptions { Upload = new ChatRateLimitPair { PerPerson = new ChatRateLimit { PermitLimit = 0, WindowSeconds = 60 }, PerSpace = new ChatRateLimit { PermitLimit = 5, WindowSeconds = 0 } } };

        var result = new ChatRateLimitOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(2);
        new ChatRateLimitOptionsValidator().Validate(null, new ChatRateLimitOptions()).Succeeded.Should().BeTrue();
    }
}
