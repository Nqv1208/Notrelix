using FluentAssertions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Domain.Tests.Documents;

public class BlockTreeRulesTests
{
    [Fact]
    public void EnsureNoCycle_WhenBlockIsOwnParent_ShouldThrow()
    {
        var blockId = Guid.NewGuid();
        var act = () => BlockTreeRules.EnsureNoCycle(blockId,
            BlockAncestorPath.Create(blockId, new[] { Guid.NewGuid() }));
        act.Should().Throw<BusinessRuleException>().WithMessage("*own parent*");
    }

    [Fact]
    public void EnsureNoCycle_WhenAncestorContainsBlock_ShouldThrow()
    {
        var blockId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var act = () => BlockTreeRules.EnsureNoCycle(blockId,
            BlockAncestorPath.Create(parentId, new[] { blockId }));

        act.Should().Throw<BusinessRuleException>().WithMessage("*cycle*");
    }

    [Fact]
    public void EnsureNoCycle_WhenDeepChainContainsBlock_ShouldThrow()
    {
        var blockId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var grandparentId = Guid.NewGuid();

        var act = () => BlockTreeRules.EnsureNoCycle(blockId,
            BlockAncestorPath.Create(parentId, new[] { grandparentId, blockId }));

        act.Should().Throw<BusinessRuleException>().WithMessage("*cycle*");
    }

    [Fact]
    public void EnsureNoCycle_WhenNoCycle_ShouldNotThrow()
    {
        var blockId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var act = () => BlockTreeRules.EnsureNoCycle(blockId,
            BlockAncestorPath.Create(parentId, new[] { Guid.NewGuid() }));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureParentSameScope_WithNullParent_ShouldNotThrow()
    {
        var act = () => BlockTreeRules.EnsureParentSameScope(null, Guid.NewGuid(), Guid.NewGuid(), _ => null);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureParentSameScope_WhenParentNotFound_ShouldThrow()
    {
        var parentId = Guid.NewGuid();
        var act = () => BlockTreeRules.EnsureParentSameScope(parentId, Guid.NewGuid(), Guid.NewGuid(), _ => null);
        act.Should().Throw<BusinessRuleException>().WithMessage("*not found*");
    }

    [Fact]
    public void EnsureParentSameScope_WhenScopeMismatch_ShouldThrow()
    {
        var parentId = Guid.NewGuid();
        var act = () => BlockTreeRules.EnsureParentSameScope(parentId, Guid.NewGuid(), Guid.NewGuid(), id =>
            (Guid.NewGuid(), Guid.NewGuid()));

        act.Should().Throw<BusinessRuleException>().WithMessage("*same page and workspace*");
    }

    [Fact]
    public void EnsureParentSameScope_WhenScopeMatches_ShouldNotThrow()
    {
        var parentId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var act = () => BlockTreeRules.EnsureParentSameScope(parentId, pageId, workspaceId, id =>
            (pageId, workspaceId));

        act.Should().NotThrow();
    }
}
