using FluentAssertions;
using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Domain.Tests.WorkManagement;

public class RelationRulesTests
{
    [Fact]
    public void EnsureDifferentBoards_WithDifferentIds_ShouldNotThrow()
    {
        var act = () => RelationRules.EnsureDifferentBoards(Guid.NewGuid(), Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureDifferentBoards_WithSameId_ShouldThrow()
    {
        var id = Guid.NewGuid();
        var act = () => RelationRules.EnsureDifferentBoards(id, id);
        act.Should().Throw<DomainException>().WithMessage("*must be different*");
    }

    [Fact]
    public void EnsureBoardsInSameWorkspace_WhenBothInWorkspace_ShouldNotThrow()
    {
        var wsId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var act = () => RelationRules.EnsureBoardsInSameWorkspace(wsId, sourceId, targetId, id =>
            id == sourceId || id == targetId ? wsId : Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureBoardsInSameWorkspace_WhenSourceNotInWorkspace_ShouldThrow()
    {
        var wsId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var act = () => RelationRules.EnsureBoardsInSameWorkspace(wsId, sourceId, targetId, id =>
            id == sourceId ? Guid.NewGuid() : wsId);

        act.Should().Throw<BusinessRuleException>().WithMessage("*does not belong*");
    }

    [Fact]
    public void EnsureBoardsInSameWorkspace_WhenTargetNotInWorkspace_ShouldThrow()
    {
        var wsId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var act = () => RelationRules.EnsureBoardsInSameWorkspace(wsId, sourceId, targetId, id =>
            id == targetId ? Guid.NewGuid() : wsId);

        act.Should().Throw<BusinessRuleException>().WithMessage("*does not belong*");
    }
}
