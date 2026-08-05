using FluentAssertions;
using Notrelix.Domain.Documents.Rules;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Domain.Tests.Documents.Rules;

public class PageRulesTests
{
    [Fact]
    public void EnsureCanEdit_WhenActive_ShouldNotThrow()
    {
        Action act = () => PageRules.EnsureCanEdit(PageStatus.Active);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_WhenArchived_ShouldThrow()
    {
        Action act = () => PageRules.EnsureCanEdit(PageStatus.Archived);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*archived*");
    }

    [Fact]
    public void EnsureTitleNotTooLong_WithinLimit_ShouldNotThrow()
    {
        Action act = () => PageRules.EnsureTitleNotTooLong("My Page Title");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureTitleNotTooLong_WhenExceedsLimit_ShouldThrow()
    {
        var longTitle = new string('A', 501);

        Action act = () => PageRules.EnsureTitleNotTooLong(longTitle);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EnsureTitleNotTooLong_AtExactLimit_ShouldNotThrow()
    {
        var exactTitle = new string('A', 500);

        Action act = () => PageRules.EnsureTitleNotTooLong(exactTitle);

        act.Should().NotThrow();
    }
}
