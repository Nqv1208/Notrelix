using FluentAssertions;
using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Domain.Tests.WorkManagement;

public class RelationRulesTests
{
    private static BoardRelation CreateRelation(Guid sourceBoardId, Guid targetBoardId)
    {
        return BoardRelation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            sourceBoardId, targetBoardId,
            null, null,
            Guid.NewGuid(), DateTimeOffset.UtcNow);
    }

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

    // ── Duplicate relation tests ──────────────────────────────────────────

    [Fact]
    public void EnsureNoDuplicateRelation_WhenNoExisting_ShouldNotThrow()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var existing = Array.Empty<BoardRelation>();

        var act = () => RelationRules.EnsureNoDuplicateRelation(sourceId, targetId, existing);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoDuplicateRelation_WhenDifferentPair_ShouldNotThrow()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var otherSourceId = Guid.NewGuid();
        var existing = new[] { CreateRelation(otherSourceId, targetId) };

        var act = () => RelationRules.EnsureNoDuplicateRelation(sourceId, targetId, existing);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoDuplicateRelation_WhenDuplicateExists_ShouldThrow()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var existing = new[] { CreateRelation(sourceId, targetId) };

        var act = () => RelationRules.EnsureNoDuplicateRelation(sourceId, targetId, existing);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void EnsureNoDuplicateRelation_WhenDeletedDuplicateExists_ShouldNotThrow()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relation = CreateRelation(sourceId, targetId);
        relation.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var existing = new[] { relation };

        var act = () => RelationRules.EnsureNoDuplicateRelation(sourceId, targetId, existing);
        act.Should().NotThrow();
    }

    // ── Cardinality tests ─────────────────────────────────────────────────

    [Fact]
    public void EnsureCardinalityLimit_WhenUnderLimit_ShouldNotThrow()
    {
        var boardId = Guid.NewGuid();
        var existing = new[]
        {
            CreateRelation(boardId, Guid.NewGuid()),
            CreateRelation(Guid.NewGuid(), boardId)
        };

        var act = () => RelationRules.EnsureCardinalityLimit(boardId, 5, existing);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCardinalityLimit_WhenAtLimit_ShouldThrow()
    {
        var boardId = Guid.NewGuid();
        var existing = new[]
        {
            CreateRelation(boardId, Guid.NewGuid()),
            CreateRelation(boardId, Guid.NewGuid()),
            CreateRelation(Guid.NewGuid(), boardId)
        };

        var act = () => RelationRules.EnsureCardinalityLimit(boardId, 3, existing);
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*maximum*");
    }

    [Fact]
    public void EnsureCardinalityLimit_WhenZeroLimit_ShouldNotThrow()
    {
        var boardId = Guid.NewGuid();
        var existing = new[] { CreateRelation(boardId, Guid.NewGuid()) };

        var act = () => RelationRules.EnsureCardinalityLimit(boardId, 0, existing);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCardinalityLimit_ExcludesDeletedRelations()
    {
        var boardId = Guid.NewGuid();
        var relation1 = CreateRelation(boardId, Guid.NewGuid());
        var relation2 = CreateRelation(boardId, Guid.NewGuid());
        relation2.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var existing = new[] { relation1, relation2 };

        // With limit 2, only 1 active relation counts, so should not throw
        var act = () => RelationRules.EnsureCardinalityLimit(boardId, 2, existing);
        act.Should().NotThrow();
    }
}
