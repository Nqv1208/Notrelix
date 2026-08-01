using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class ItemParentPathTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Parent = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetProperties_AndDeriveChildLevel()
    {
        var path = ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 2, new[] { Guid.NewGuid(), Guid.NewGuid() });

        path.AccountId.Should().Be(AccountId);
        path.WorkspaceId.Should().Be(WsA);
        path.BoardId.Should().Be(BoardA);
        path.ParentItemId.Should().Be(Parent);
        path.ParentLevel.Should().Be(2);
        path.ChildLevel.Should().Be(3);
    }

    [Fact]
    public void Create_ShouldRejectEmptyAccountId()
    {
        Action act = () => ItemParentPath.Create(Guid.Empty, WsA, BoardA, Parent, 0, Array.Empty<Guid>());
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldRejectEmptyWorkspaceId()
    {
        Action act = () => ItemParentPath.Create(AccountId, Guid.Empty, BoardA, Parent, 0, Array.Empty<Guid>());
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldRejectEmptyBoardId()
    {
        Action act = () => ItemParentPath.Create(AccountId, WsA, Guid.Empty, Parent, 0, Array.Empty<Guid>());
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldRejectEmptyParentItemId()
    {
        Action act = () => ItemParentPath.Create(AccountId, WsA, BoardA, Guid.Empty, 0, Array.Empty<Guid>());
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldRejectNegativeParentLevel()
    {
        Action act = () => ItemParentPath.Create(AccountId, WsA, BoardA, Parent, -1, Array.Empty<Guid>());
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldRejectEmptyAncestorId()
    {
        Action act = () => ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 0, new[] { Guid.Empty });
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldRejectDuplicateAncestors()
    {
        var duplicate = Guid.NewGuid();
        Action act = () => ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 0, new[] { duplicate, duplicate });
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldRejectAncestorContainingTargetParent()
    {
        Action act = () => ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 0, new[] { Parent });
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldCopyAncestors_ForImmutability()
    {
        var ancestor = Guid.NewGuid();
        var source = new List<Guid> { ancestor };
        var path = ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 0, source);

        source[0] = Guid.NewGuid();

        path.AncestorIds.Should().ContainSingle().Which.Should().Be(ancestor);
    }

    [Fact]
    public void Equality_ShouldCompareAllComponents()
    {
        var ancestor = Guid.NewGuid();
        var pathA = ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 1, new[] { ancestor });
        var pathB = ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 1, new[] { ancestor });
        var pathC = ItemParentPath.Create(AccountId, WsA, BoardA, Parent, 2, new[] { ancestor });

        pathA.Should().Be(pathB);
        pathA.Should().NotBe(pathC);
        pathA.GetHashCode().Should().Be(pathB.GetHashCode());
    }
}
