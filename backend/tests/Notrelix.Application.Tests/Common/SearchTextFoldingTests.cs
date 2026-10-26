using System.Text.RegularExpressions;
using Notrelix.Application.Common.Search;

namespace Notrelix.Application.Tests.Common;

public class SearchTextFoldingTests
{
    [Theory]
    [InlineData("Hoá Đơn", "hoa don")]
    [InlineData("ĐƯỜNG", "duong")]
    [InlineData("Crème Brûlée", "creme brulee")]
    [InlineData("ERP-142", "erp-142")]
    public void Fold_RemovesMarksAndCase(string text, string expected) =>
        SearchTextFolding.Fold(text).Should().Be(expected);

    [Fact]
    public void Clean_TrimsCollapsesAndCuts()
    {
        SearchTextFolding.Clean("  hoa   don  ").Should().Be("hoa don");
        SearchTextFolding.Clean(null).Should().BeEmpty();
        SearchTextFolding.Clean(new string('a', 300)).Length.Should().Be(SearchTextFolding.MaxLength);
    }

    [Theory]
    [InlineData("hoa don", "Hóa đơn tháng 10", true)]
    [InlineData("hóa đơn", "HOA DON", true)]
    [InlineData("duong", "Đường dẫn", true)]
    [InlineData("đuong", "duong", true)]
    [InlineData("erp-14", "ERP-142", true)]
    [InlineData("hoa don", "Họp tuần", false)]
    [InlineData("a", "anything", false)]
    public void ToPattern_MatchesWhatFoldingSaysItShould(string typed, string stored, bool matches)
    {
        var pattern = SearchTextFolding.ToPattern(typed);

        (pattern.Length > 0 && Regex.IsMatch(stored, pattern)).Should().Be(matches);
    }

    [Theory]
    [InlineData(".*")]
    [InlineData("(a+")]
    [InlineData("[a-z]")]
    [InlineData("a|b")]
    [InlineData("\\d+")]
    public void ToPattern_TypedTextIsLiteral(string typed)
    {
        var pattern = SearchTextFolding.ToPattern(typed);

        Regex.IsMatch("plain words 123 az", pattern).Should().BeFalse();
        Regex.IsMatch($"before {typed} after", pattern, RegexOptions.IgnoreCase).Should().BeTrue();
    }

    [Fact]
    public void ToPattern_ShortText_IsEmpty() => SearchTextFolding.ToPattern(" a ").Should().BeEmpty();
}
