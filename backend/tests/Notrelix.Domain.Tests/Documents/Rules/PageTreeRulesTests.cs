using FluentAssertions;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Domain.Tests.Documents.Rules;

public class PageTreeRulesTests
{
    [Fact]
    public void EnsureNoCycle_WhenValid_ShouldNotThrow()
    {
        var pageId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        Func<Guid, Guid?> getParentId = _ => null;

        Action act = () => PageTreeRules.EnsureNoCycle(pageId, parentId, getParentId);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoCycle_WhenSameAsParent_ShouldThrow()
    {
        var pageId = Guid.NewGuid();

        Func<Guid, Guid?> getParentId = _ => null;

        Action act = () => PageTreeRules.EnsureNoCycle(pageId, pageId, getParentId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*own parent*");
    }

    [Fact]
    public void EnsureNoCycle_WhenCycleDetected_ShouldThrow()
    {
        var pageId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        Func<Guid, Guid?> getParentId = id =>
        {
            if (id == parentId) return pageId;
            return null;
        };

        Action act = () => PageTreeRules.EnsureNoCycle(pageId, parentId, getParentId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*cycle*");
    }

    [Fact]
    public void EnsureNoCycle_WhenDeepCycleDetected_ShouldThrow()
    {
        var pageId = Guid.NewGuid();
        var grandparentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        Func<Guid, Guid?> getParentId = id =>
        {
            if (id == parentId) return grandparentId;
            if (id == grandparentId) return pageId;
            return null;
        };

        Action act = () => PageTreeRules.EnsureNoCycle(pageId, parentId, getParentId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*cycle*");
    }

    [Fact]
    public void EnsureNoCycle_WhenLongChainNoCycle_ShouldNotThrow()
    {
        var pageId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var grandparentId = Guid.NewGuid();

        Func<Guid, Guid?> getParentId = id =>
        {
            if (id == parentId) return grandparentId;
            if (id == grandparentId) return null;
            return null;
        };

        Action act = () => PageTreeRules.EnsureNoCycle(pageId, parentId, getParentId);

        act.Should().NotThrow();
    }
}
