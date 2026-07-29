using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class ArchitectureExclusionRegistryTests
{
    [Fact]
    public void IsExcludedType_ShouldExcludeObject()
    {
        ArchitectureExclusionRegistry.IsExcludedType(typeof(object)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeValueType()
    {
        ArchitectureExclusionRegistry.IsExcludedType(typeof(ValueType)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeIEquatable()
    {
        ArchitectureExclusionRegistry.IsExcludedType(typeof(IEquatable<int>)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeIComparable()
    {
        ArchitectureExclusionRegistry.IsExcludedType(typeof(IComparable<int>)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldExcludeObsoleteAttribute()
    {
        ArchitectureExclusionRegistry.IsExcludedType(typeof(ObsoleteAttribute)).Should().BeTrue();
    }

    [Fact]
    public void IsExcludedType_ShouldNotExcludeDomainType()
    {
        ArchitectureExclusionRegistry.IsExcludedType(typeof(AggregateRoot)).Should().BeFalse();
    }
}
