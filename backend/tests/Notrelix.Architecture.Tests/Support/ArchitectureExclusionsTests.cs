using FluentAssertions;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

public class ArchitectureExclusionsTests
{
    [Fact]
    public void IsExcludedType_ShouldExcludeObject()
    {
        ArchitectureExclusions.IsExcludedType(typeof(object)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeValueType()
    {
        ArchitectureExclusions.IsExcludedType(typeof(ValueType)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeIEquatable()
    {
        ArchitectureExclusions.IsExcludedType(typeof(IEquatable<int>)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeIComparable()
    {
        ArchitectureExclusions.IsExcludedType(typeof(IComparable<int>)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeObsoleteAttribute()
    {
        ArchitectureExclusions.IsExcludedType(typeof(ObsoleteAttribute)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldNotExcludeDomainType()
    {
        ArchitectureExclusions.IsExcludedType(typeof(AggregateRoot)).Should().BeFalse();
    }
}
