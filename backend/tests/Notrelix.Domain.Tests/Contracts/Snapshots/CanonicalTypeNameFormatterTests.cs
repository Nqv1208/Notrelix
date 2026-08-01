using FluentAssertions;

namespace Notrelix.Domain.Tests.Contracts.Snapshots;

public class CanonicalTypeNameFormatterTests
{
    [Fact]
    public void BuiltIn_Aliases_AreFullyQualified()
    {
        CanonicalTypeNameFormatter.Format(typeof(string)).Should().Be("System.String");
        CanonicalTypeNameFormatter.Format(typeof(int)).Should().Be("System.Int32");
        CanonicalTypeNameFormatter.Format(typeof(long)).Should().Be("System.Int64");
        CanonicalTypeNameFormatter.Format(typeof(bool)).Should().Be("System.Boolean");
        CanonicalTypeNameFormatter.Format(typeof(Guid)).Should().Be("System.Guid");
        CanonicalTypeNameFormatter.Format(typeof(DateTimeOffset)).Should().Be("System.DateTimeOffset");
        CanonicalTypeNameFormatter.Format(typeof(void)).Should().Be("System.Void");
    }

    [Fact]
    public void Nullable_ValueTypes_GetQuestionSuffix()
    {
        CanonicalTypeNameFormatter.Format(typeof(Guid?)).Should().Be("System.Guid?");
        CanonicalTypeNameFormatter.Format(typeof(int?)).Should().Be("System.Int32?");
        CanonicalTypeNameFormatter.Format(typeof(DateTimeOffset?)).Should().Be("System.DateTimeOffset?");
    }

    [Fact]
    public void Array_Types_AreFormatted()
    {
        CanonicalTypeNameFormatter.Format(typeof(byte[])).Should().Be("System.Byte[]");
        CanonicalTypeNameFormatter.Format(typeof(string[])).Should().Be("System.String[]");
        CanonicalTypeNameFormatter.Format(typeof(int[][])).Should().Be("System.Int32[][]");
    }

    [Fact]
    public void Generic_Types_AreFullyQualified()
    {
        CanonicalTypeNameFormatter.Format(typeof(List<string>))
            .Should().Be("System.Collections.Generic.List<System.String>");

        CanonicalTypeNameFormatter.Format(typeof(Dictionary<string, int>))
            .Should().Be("System.Collections.Generic.Dictionary<System.String,System.Int32>");
    }

    [Fact]
    public void Nested_Types_UsePlusSeparator()
    {
        CanonicalTypeNameFormatter.Format(typeof(Environment.SpecialFolder))
            .Should().Be("System.Environment+SpecialFolder");
    }

    [Fact]
    public void Domain_Types_AreFullyQualified()
    {
        var boardItemType = typeof(Notrelix.Domain.WorkManagement.Items.BoardItem);
        CanonicalTypeNameFormatter.Format(boardItemType)
            .Should().Be("Notrelix.Domain.WorkManagement.Items.BoardItem");
    }
}
