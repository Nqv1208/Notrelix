using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel.Ordering;

/// <summary>
/// Property-based tests for FractionalIndexGenerator.
/// Uses deterministic seeds to verify ordering invariants at scale.
/// </summary>
public class FractionalIndexPropertyTests
{
    private const int SeedCount = 100;
    private const int InsertionsPerSeed = 1000;

    [Fact]
    public void PropertyTest_AllGeneratedKeysAreValid()
    {
        for (var seed = 0; seed < SeedCount; seed++)
        {
            var rng = new Random(seed);
            var keys = GenerateRandomSequence(rng, InsertionsPerSeed);

            foreach (var key in keys)
            {
                key.Value.Should().NotBeNullOrEmpty();
                foreach (var c in key.Value)
                    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
                        .Should().Contain(c.ToString(),
                            $"seed {seed}: key '{key.Value}' contains invalid character '{c}'");
            }
        }
    }

    [Fact]
    public void PropertyTest_StrictOrdinalOrdering()
    {
        for (var seed = 0; seed < SeedCount; seed++)
        {
            var rng = new Random(seed);
            var keys = GenerateRandomSequence(rng, InsertionsPerSeed);

            for (var i = 1; i < keys.Count; i++)
                keys[i - 1].CompareTo(keys[i]).Should().BeNegative(
                    $"seed {seed}: key[{i - 1}] '{keys[i - 1].Value}' should be < key[{i}] '{keys[i].Value}'");
        }
    }

    [Fact]
    public void PropertyTest_NoDuplicates()
    {
        for (var seed = 0; seed < SeedCount; seed++)
        {
            var rng = new Random(seed);
            var keys = GenerateRandomSequence(rng, InsertionsPerSeed);

            var values = keys.Select(k => k.Value).ToList();
            values.Distinct().Should().HaveCount(values.Count,
                $"seed {seed}: duplicate keys generated");
        }
    }

    [Fact]
    public void PropertyTest_AllKeysInsideBounds()
    {
        var lower = FractionalIndex.Create("a0");
        var upper = FractionalIndex.Create("a1");

        for (var seed = 0; seed < SeedCount; seed++)
        {
            var rng = new Random(seed);
            var keys = GenerateBoundedSequence(rng, lower, upper, InsertionsPerSeed);

            foreach (var key in keys)
            {
                key.CompareTo(lower).Should().BeGreaterThanOrEqualTo(0,
                    $"seed {seed}: key '{key.Value}' < lower bound '{lower.Value}'");
                key.CompareTo(upper).Should().BeLessThanOrEqualTo(0,
                    $"seed {seed}: key '{key.Value}' > upper bound '{upper.Value}'");
            }
        }
    }

    [Fact]
    public void PropertyTest_BatchCountExact()
    {
        for (var seed = 0; seed < SeedCount; seed++)
        {
            var rng = new Random(seed);
            var count = rng.Next(1, 100);

            var keys = FractionalIndexGenerator.GenerateNKeysBetween(null, null, count);
            keys.Should().HaveCount(count, $"seed {seed}: expected {count} keys");
        }
    }

    [Fact]
    public void PropertyTest_InsertionBetweenExistingKeys()
    {
        for (var seed = 0; seed < SeedCount; seed++)
        {
            var rng = new Random(seed);
            var initial = FractionalIndexGenerator.GenerateNKeysBetween(null, null, 10);

            FractionalIndex? prev = null;
            foreach (var existing in initial)
            {
                if (prev != null)
                {
                    var inserted = FractionalIndexGenerator.GenerateKeyBetween(prev, existing);
                    inserted.CompareTo(prev).Should().BePositive(
                        $"seed {seed}: inserted key should be > lower bound");
                    inserted.CompareTo(existing).Should().BeNegative(
                        $"seed {seed}: inserted key should be < upper bound");
                }
                prev = existing;
            }
        }
    }

    private static List<FractionalIndex> GenerateRandomSequence(Random rng, int count)
    {
        var keys = new List<FractionalIndex>();

        for (var i = 0; i < count; i++)
        {
            FractionalIndex? lower = keys.Count > 0 ? keys[rng.Next(keys.Count)] : null;
            FractionalIndex? upper = keys.Count > 0 ? keys[rng.Next(keys.Count)] : null;

            try
            {
                if (lower != null && upper != null && lower.CompareTo(upper) >= 0)
                    (lower, upper) = (upper, lower);

                var key = FractionalIndexGenerator.GenerateKeyBetween(lower, upper);
                keys.Add(key);
            }
            catch (ArgumentException)
            {
                // Skip invalid combinations
            }
        }

        keys.Sort();
        return keys;
    }

    private static List<FractionalIndex> GenerateBoundedSequence(
        Random rng, FractionalIndex lower, FractionalIndex upper, int count)
    {
        var keys = new List<FractionalIndex>();
        var currentLower = lower;

        for (var i = 0; i < count; i++)
        {
            try
            {
                var key = FractionalIndexGenerator.GenerateKeyBetween(currentLower, upper);
                keys.Add(key);
                currentLower = key;
            }
            catch (ArgumentException)
            {
                break;
            }
        }

        return keys;
    }
}
