using FluentAssertions;
using Notrelix.Domain.Common;
using Xunit;

namespace Notrelix.Domain.Tests.Common;

public class ValueObjectTests
{
    private class TestValueObject : ValueObject
    {
        public string StringProp { get; }
        public int IntProp { get; }

        public TestValueObject(string stringProp, int intProp)
        {
            StringProp = stringProp;
            IntProp = intProp;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return StringProp;
            yield return IntProp;
        }
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenPropertiesMatch()
    {
        var obj1 = new TestValueObject("test", 1);
        var obj2 = new TestValueObject("test", 1);

        obj1.Equals(obj2).Should().BeTrue();
        (obj1 == obj2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenPropertiesDiffer()
    {
        var obj1 = new TestValueObject("test", 1);
        var obj2 = new TestValueObject("test2", 1);

        obj1.Equals(obj2).Should().BeFalse();
        (obj1 != obj2).Should().BeTrue();
    }
}
