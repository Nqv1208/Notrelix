namespace Notrelix.Application.Tests.Common.Idempotency;

using Microsoft.Extensions.Options;

/// <summary>
/// Spec 3.7: replay eligibility. Sensitive response types fail before the
/// store Begin; oversized results fail before Complete so the request
/// transaction rolls back instead of leaving a non-replayable Started row.
/// </summary>
public class DefaultIdempotencyReplayPolicyTests
{
    private sealed record SensitivePayload(Guid Token);

    private static DefaultIdempotencyReplayPolicy CreatePolicy(
        IReadOnlyList<string>? sensitiveResultTypes = null,
        int maxResultBytes = 1_048_576)
    {
        return new DefaultIdempotencyReplayPolicy(Options.Create(new IdempotencyOptions
        {
            SensitiveResultTypes = sensitiveResultTypes ?? [],
            MaxResultBytes = maxResultBytes,
        }));
    }

    [Fact]
    public void EnsureResponseTypeAllowed_SensitiveType_ThrowsBeforeBegin()
    {
        var policy = CreatePolicy(sensitiveResultTypes: [typeof(SensitivePayload).FullName!]);

        var act = () => policy.EnsureResponseTypeAllowed<SensitivePayload>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{typeof(SensitivePayload).FullName}'*marked as sensitive*");
    }

    [Fact]
    public void EnsureResponseTypeAllowed_NonSensitiveType_DoesNotThrow()
    {
        var policy = CreatePolicy();

        var act = () => policy.EnsureResponseTypeAllowed<string>();

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureSerializedResultAllowed_ExceedingMaxResultBytes_Throws()
    {
        var policy = CreatePolicy(maxResultBytes: 64);
        var oversized = new string('x', 128);

        var act = () => policy.EnsureSerializedResultAllowed(oversized, oversized);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds the maximum*");
    }

    [Fact]
    public void EnsureSerializedResultAllowed_AtLimit_DoesNotThrow()
    {
        var policy = CreatePolicy(maxResultBytes: 64);
        var payload = new string('x', 64);

        var act = () => policy.EnsureSerializedResultAllowed(payload, payload);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureSerializedResultAllowed_WithinLimit_DoesNotThrow()
    {
        var policy = CreatePolicy(maxResultBytes: 1_048_576);
        var payload = "small result";

        var act = () => policy.EnsureSerializedResultAllowed(payload, payload);

        act.Should().NotThrow();
    }
}
