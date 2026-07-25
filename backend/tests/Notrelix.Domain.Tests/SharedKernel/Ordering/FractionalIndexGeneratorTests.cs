using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel.Ordering;

/// <summary>
/// Tests ported from upstream rocicorp/fractional-indexing v4.0.0 test.js.
/// Only default-alphabet tests (BASE_62 digits, A-Z/a-z heads) are included.
/// Custom digits/intDigits tests are omitted since our API does not expose them.
/// </summary>
public class FractionalIndexGeneratorTests
{
    // ── Official upstream fixtures (test.js) ──────────────────────────────

    [Fact] public void Upstream_Null_Null() => AssertGenerate(null, null, "a0");
    [Fact] public void Upstream_Null_a0() => AssertGenerate(null, "a0", "Zz");
    [Fact] public void Upstream_Null_Zz() => AssertGenerate(null, "Zz", "Zy");
    [Fact] public void Upstream_a0_Null() => AssertGenerate("a0", null, "a1");
    [Fact] public void Upstream_a1_Null() => AssertGenerate("a1", null, "a2");
    [Fact] public void Upstream_a0_a1() => AssertGenerate("a0", "a1", "a0V");
    [Fact] public void Upstream_a1_a2() => AssertGenerate("a1", "a2", "a1V");
    [Fact] public void Upstream_a0V_a1() => AssertGenerate("a0V", "a1", "a0l");
    [Fact] public void Upstream_Zz_a0() => AssertGenerate("Zz", "a0", "ZzV");
    [Fact] public void Upstream_Zz_a1() => AssertGenerate("Zz", "a1", "a0");
    [Fact] public void Upstream_Null_Y00() => AssertGenerate(null, "Y00", "Xzzz");
    [Fact] public void Upstream_bzz_Null() => AssertGenerate("bzz", null, "c000");
    [Fact] public void Upstream_a0_a0V() => AssertGenerate("a0", "a0V", "a0G");
    [Fact] public void Upstream_a0_a0G() => AssertGenerate("a0", "a0G", "a08");
    [Fact] public void Upstream_b125_b129() => AssertGenerate("b125", "b129", "b127");
    [Fact] public void Upstream_a0_a1V() => AssertGenerate("a0", "a1V", "a1");
    [Fact] public void Upstream_Zz_a01() => AssertGenerate("Zz", "a01", "a0");
    [Fact] public void Upstream_Null_a0V() => AssertGenerate(null, "a0V", "a0");
    [Fact] public void Upstream_Null_b999() => AssertGenerate(null, "b999", "b99");
    [Fact] public void Upstream_Reversed_a1_a0() => AssertGenerate("a1", "a0", "a0V");

    [Fact]
    public void Upstream_Null_A00000000000000000000000000_Throws()
    {
        var act = () => FractionalIndexGenerator.GenerateKeyBetween(null, FractionalIndex.Create("A00000000000000000000000000"));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Upstream_Null_A000000000000000000000000001()
    {
        AssertGenerate(null, "A000000000000000000000000001", "A000000000000000000000000000V");
    }

    [Fact]
    public void Upstream_zzzzzzzzzzzzzzzzzzzzzzzzz_y()
    {
        AssertGenerate(
            "zzzzzzzzzzzzzzzzzzzzzzzzzzy",
            null,
            "zzzzzzzzzzzzzzzzzzzzzzzzzzz");
    }

    [Fact]
    public void Upstream_zzzzzzzzzzzzzzzzzzzzzzzzz_z()
    {
        AssertGenerate(
            "zzzzzzzzzzzzzzzzzzzzzzzzzzz",
            null,
            "zzzzzzzzzzzzzzzzzzzzzzzzzzzV");
    }

    [Fact]
    public void Upstream_a00_Null_Throws()
    {
        var act = () => FractionalIndexGenerator.GenerateKeyBetween(FractionalIndex.Create("a00"), (FractionalIndex?)null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Upstream_a00_a1_Throws()
    {
        var act = () => FractionalIndexGenerator.GenerateKeyBetween(FractionalIndex.Create("a00"), FractionalIndex.Create("a1"));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Upstream_InvalidHead_0_Rejected()
    {
        // "0" is not a valid head character in the A-Z/a-z alphabet
        var act = () => FractionalIndex.Create("0");
        act.Should().Throw<ArgumentException>();
    }

    // ── Existing custom fixtures ─────────────────────────────────────────

    [Fact]
    public void GenerateNKeysBetween_Null_Null_2()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 2);
        keys.Should().HaveCount(2);
        keys[0].Value.Should().Be("a0");
        keys[1].Value.Should().Be("a1");
    }

    // ── Ordering invariants ──────────────────────────────────────────────

    [Fact]
    public void GeneratedKey_IsStrictlyBetweenBounds()
    {
        var lower = FractionalIndex.Create("a0");
        var upper = FractionalIndex.Create("a2");
        var key = FractionalIndexGenerator.GenerateKeyBetween(lower, upper);

        key.CompareTo(lower).Should().BePositive();
        key.CompareTo(upper).Should().BeNegative();
    }

    [Fact]
    public void GenerateNKeys_AreStrictlyOrdered()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 10);

        for (var i = 1; i < keys.Count; i++)
            keys[i - 1].CompareTo(keys[i]).Should().BeNegative();
    }

    [Fact]
    public void GenerateNKeys_AreUnique()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 20);
        keys.Select(k => k.Value).Distinct().Should().HaveCount(20);
    }

    // ── Prefix crash regression ──────────────────────────────────────────

    [Fact]
    public void PrefixBounds_DoNotCrash()
    {
        var lower = FractionalIndex.Create("a0");
        var upper = FractionalIndex.Create("a0V");
        var key = FractionalIndexGenerator.GenerateKeyBetween(lower, upper);

        key.CompareTo(lower).Should().BePositive();
        key.CompareTo(upper).Should().BeNegative();
    }

    [Fact]
    public void RepeatedInsertion_InSameGap_RemainsValid()
    {
        var lower = FractionalIndex.Create("a0");
        var upper = FractionalIndex.Create("a1");

        var current = lower;
        for (var i = 0; i < 50; i++)
        {
            var key = FractionalIndexGenerator.GenerateKeyBetween(current, upper);
            key.CompareTo(current).Should().BePositive();
            key.CompareTo(upper).Should().BeNegative();
            current = key;
        }
    }

    // ── Out-of-alphabet regression ───────────────────────────────────────

    [Fact]
    public void GeneratedKeys_OnlyContainValidCharacters()
    {
        const string validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 100);

        foreach (var key in keys)
        {
            foreach (var c in key.Value)
                validChars.Should().Contain(c.ToString(), $"key '{key.Value}' contains invalid character '{c}'");
        }
    }

    // ── Validation ───────────────────────────────────────────────────────

    [Fact]
    public void InvalidKey_IsRejected()
    {
        var act = () => FractionalIndex.Create("!!!");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvalidKey_EmptyString_IsRejected()
    {
        var act = () => FractionalIndex.Create("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvalidKey_Whitespace_IsRejected()
    {
        var act = () => FractionalIndex.Create(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReversedBounds_AreAutoSwapped()
    {
        var lower = FractionalIndex.Create("a1");
        var upper = FractionalIndex.Create("a0");
        var key = FractionalIndexGenerator.GenerateKeyBetween(lower, upper);
        key.Value.Should().Be("a0V");
    }

    [Fact]
    public void Count_Zero_IsRejected()
    {
        var act = () => FractionalIndexGenerator.GenerateNKeysBetween(null, null, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Count_Negative_IsRejected()
    {
        var act = () => FractionalIndexGenerator.GenerateNKeysBetween(null, null, -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Existing key compatibility ───────────────────────────────────────

    [Theory]
    [InlineData("a0")]
    [InlineData("a1")]
    [InlineData("a2")]
    [InlineData("a3")]
    [InlineData("Zz")]
    [InlineData("a0V")]
    [InlineData("a1V")]
    public void ExistingKeys_MustBeValid(string value)
    {
        var index = FractionalIndex.Create(value);
        index.Value.Should().Be(value);
    }

    // ── Ordinal comparison ───────────────────────────────────────────────

    [Fact]
    public void OrdinalSorting_MatchesGenerationOrder()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 20);
        var sorted = keys.OrderBy(k => k.Value, StringComparer.Ordinal).ToList();

        for (var i = 0; i < keys.Count; i++)
            keys[i].Value.Should().Be(sorted[i].Value);
    }

    // ── No trimming ──────────────────────────────────────────────────────

    [Fact]
    public void Create_DoesNotTrim_WhitespaceIsRejected()
    {
        var act = () => FractionalIndex.Create(" a0");
        act.Should().Throw<ArgumentException>();
    }

    // ── Large batch ──────────────────────────────────────────────────────

    [Fact]
    public void GenerateNKeys_100_AllValidAndOrdered()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 100);

        keys.Should().HaveCount(100);
        keys.Select(k => k.Value).Distinct().Should().HaveCount(100);

        for (var i = 1; i < keys.Count; i++)
            keys[i - 1].CompareTo(keys[i]).Should().BeNegative();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void AssertGenerate(string? a, string? b, string expected)
    {
        FractionalIndex? fa = a != null ? FractionalIndex.Create(a) : null;
        FractionalIndex? fb = b != null ? FractionalIndex.Create(b) : null;
        var key = FractionalIndexGenerator.GenerateKeyBetween(fa, fb);
        key.Value.Should().Be(expected);
    }
}
