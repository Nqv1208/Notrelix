namespace Notrelix.Application.Tests.Common.Idempotency;

/// <summary>
/// The idempotency fingerprint must be business-only: it is normalized
/// (property order, ignored ambient properties) and stable, and it changes
/// exactly when business data changes.
/// </summary>
public class JsonIdempotencyRequestFingerprintTests
{
    private static readonly JsonIdempotencyRequestFingerprint Fingerprint = new();

    private sealed record SampleRequest(
        string Name,
        int Amount,
        [property: IdempotencyFingerprintIgnore] string? Ambient) : IIdempotentRequest;

    private sealed record ReorderedRequest(int Amount, string Name) : IIdempotentRequest;

    private sealed record DifferentRequest(string Name, int Amount) : IIdempotentRequest;

    [Fact]
    public void Compute_SameBusinessPayload_PropertyOrder_ReturnsSameHash()
    {
        var sample = new SampleRequest("board-1", 42, "ignored-ambient");
        var reordered = new ReorderedRequest(42, "board-1");

        Fingerprint.Compute(sample, typeof(SampleRequest))
            .Should().Be(Fingerprint.Compute(reordered, typeof(ReorderedRequest)));
    }

    [Fact]
    public void Compute_AmbientIgnoredProperty_DoesNotAffectHash()
    {
        var withAmbient = new SampleRequest("board-1", 42, "ambient-a");
        var withoutAmbient = new SampleRequest("board-1", 42, null);

        Fingerprint.Compute(withAmbient, typeof(SampleRequest))
            .Should().Be(Fingerprint.Compute(withoutAmbient, typeof(SampleRequest)));
    }

    [Fact]
    public void Compute_DifferentBusinessPayload_ReturnsDifferentHash()
    {
        var first = new SampleRequest("board-1", 42, null);
        var second = new DifferentRequest("board-2", 42);

        Fingerprint.Compute(first, typeof(SampleRequest))
            .Should().NotBe(Fingerprint.Compute(second, typeof(DifferentRequest)));
    }

    [Fact]
    public void Compute_DifferentBusinessValue_SameShape_ReturnsDifferentHash()
    {
        var first = new SampleRequest("board-1", 42, null);
        var second = new DifferentRequest("board-1", 43);

        Fingerprint.Compute(first, typeof(SampleRequest))
            .Should().NotBe(Fingerprint.Compute(second, typeof(DifferentRequest)));
    }

    [Fact]
    public void Compute_WhitespaceInBusinessValue_ChangesHash()
    {
        var first = new SampleRequest("board-1", 42, null);
        var second = new DifferentRequest("board-1 ", 42);

        Fingerprint.Compute(first, typeof(SampleRequest))
            .Should().NotBe(Fingerprint.Compute(second, typeof(DifferentRequest)));
    }

    [Fact]
    public void Compute_SamePayload_Twice_IsStable()
    {
        var request = new SampleRequest("board-1", 42, null);

        Fingerprint.Compute(request, typeof(SampleRequest))
            .Should().Be(Fingerprint.Compute(request, typeof(SampleRequest)));
    }
}
