using FluentAssertions;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Domain.Tests.Documents;

public class DocumentSnapshotTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var data = JsonValue.EmptyObject();
        var snapshot = DocumentSnapshot.Create(data);
        snapshot.Data.Should().Be(data);
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => DocumentSnapshot.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameData_ShouldBeEqual()
    {
        var data = JsonValue.EmptyObject();
        var s1 = DocumentSnapshot.Create(data);
        var s2 = DocumentSnapshot.Create(data);

        s1.Should().Be(s2);
    }

    [Fact]
    public void Equality_DifferentData_ShouldNotBeEqual()
    {
        var s1 = DocumentSnapshot.Create(JsonValue.Create("{\"a\":1}"));
        var s2 = DocumentSnapshot.Create(JsonValue.Create("{\"a\":2}"));

        s1.Should().NotBe(s2);
    }
}
