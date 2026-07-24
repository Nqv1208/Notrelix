using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel.Ordering;

public class FractionalIndexGeneratorTests
{
    // ── Official fixtures ─────────────────────────────────────────────────

    [Fact]
    public void GenerateKeyBetween_Null_Null_Returns_a0()
    {
        var key = FractionalIndexGenerator.GenerateKeyBetween(null, null);
        key.Value.Should().Be("a0");
    }

    [Fact]
    public void GenerateKeyBetween_a0_Null_Returns_a1()
    {
        var key = FractionalIndexGenerator.GenerateKeyBetween(FractionalIndex.Create("a0"), null);
        key.Value.Should().Be("a1");
    }

    [Fact]
    public void GenerateKeyBetween_a1_Null_Returns_a2()
    {
        var key = FractionalIndexGenerator.GenerateKeyBetween(FractionalIndex.Create("a1"), null);
        key.Value.Should().Be("a2");
    }

    [Fact]
    public void GenerateKeyBetween_Null_a0_Returns_Zz()
    {
        var key = FractionalIndexGenerator.GenerateKeyBetween(null, FractionalIndex.Create("a0"));
        key.Value.Should().Be("Zz");
    }

    [Fact]
    public void GenerateKeyBetween_a1_a2_Returns_a1V()
    {
        var key = FractionalIndexGenerator.GenerateKeyBetween(
            FractionalIndex.Create("a1"), FractionalIndex.Create("a2"));
        key.Value.Should().Be("a1V");
    }

    [Fact]
    public void GenerateNKeysBetween_Null_Null_2_Returns_a0_a1()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 2);
        keys.Should().HaveCount(2);
        keys[0].Value.Should().Be("a0");
        keys[1].Value.Should().Be("a1");
    }

    // ── Ordering invariants ───────────────────────────────────────────────

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

    // ── Prefix crash regression ───────────────────────────────────────────

    [Fact]
    public void PrefixBounds_DoNotCrash()
    {
        // This crashed the old algorithm: Between("a", "a0")
        // "a" is not a valid key, but the generator should handle valid prefix pairs.
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

    // ── Out-of-alphabet regression ────────────────────────────────────────

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

    // ── Validation ────────────────────────────────────────────────────────

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
    public void InvalidBounds_LowerEqualsUpper_IsRejected()
    {
        var key = FractionalIndex.Create("a0");
        var act = () => FractionalIndexGenerator.GenerateKeyBetween(key, key);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvalidBounds_LowerGreaterThanUpper_IsRejected()
    {
        var lower = FractionalIndex.Create("a1");
        var upper = FractionalIndex.Create("a0");
        var act = () => FractionalIndexGenerator.GenerateKeyBetween(lower, upper);
        act.Should().Throw<ArgumentException>();
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

    // ── Existing key compatibility ────────────────────────────────────────

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

    // ── Ordinal comparison ────────────────────────────────────────────────

    [Fact]
    public void OrdinalSorting_MatchesGenerationOrder()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 20);
        var sorted = keys.OrderBy(k => k.Value, StringComparer.Ordinal).ToList();

        for (var i = 0; i < keys.Count; i++)
            keys[i].Value.Should().Be(sorted[i].Value);
    }

    // ── No trimming ───────────────────────────────────────────────────────

    [Fact]
    public void Create_DoesNotTrim_WhitespaceIsRejected()
    {
        var act = () => FractionalIndex.Create(" a0");
        act.Should().Throw<ArgumentException>();
    }

    // ── Large batch ───────────────────────────────────────────────────────

    [Fact]
    public void GenerateNKeys_100_AllValidAndOrdered()
    {
        var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 100);

        keys.Should().HaveCount(100);
        keys.Select(k => k.Value).Distinct().Should().HaveCount(100);

        for (var i = 1; i < keys.Count; i++)
            keys[i - 1].CompareTo(keys[i]).Should().BeNegative();
    }
}
