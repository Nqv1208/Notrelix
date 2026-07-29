using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Blocks;
using Xunit;

namespace Notrelix.Domain.Tests.Documents.Blocks;

public class BlockCreateChildScopeTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _pageId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private BlockAncestorPath CreatePath(Guid accountId, Guid workspaceId, Guid pageId, Guid targetParentId) =>
        BlockAncestorPath.Create(accountId, workspaceId, pageId, targetParentId, [Guid.NewGuid()]);

    [Fact]
    public void CreateChild_WithMatchingScope_ShouldSucceed()
    {
        var path = CreatePath(_accountId, _workspaceId, _pageId, Guid.NewGuid());
        var block = Block.CreateChild(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now, path);
        block.ParentId.Should().Be(path.TargetParentId);
    }

    [Fact]
    public void CreateChild_WithWrongAccount_ShouldThrow()
    {
        var path = CreatePath(Guid.NewGuid(), _workspaceId, _pageId, Guid.NewGuid());
        var act = () => Block.CreateChild(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now, path);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch);
    }

    [Fact]
    public void CreateChild_WithWrongWorkspace_ShouldThrow()
    {
        var path = CreatePath(_accountId, Guid.NewGuid(), _pageId, Guid.NewGuid());
        var act = () => Block.CreateChild(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now, path);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch);
    }

    [Fact]
    public void CreateChild_WithWrongPage_ShouldThrow()
    {
        var path = CreatePath(_accountId, _workspaceId, Guid.NewGuid(), Guid.NewGuid());
        var act = () => Block.CreateChild(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now, path);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch);
    }

    [Fact]
    public void CreateChild_ShouldAssignParentFromPath()
    {
        var parentId = Guid.NewGuid();
        var path = BlockAncestorPath.Create(_accountId, _workspaceId, _pageId, parentId, [Guid.NewGuid()]);
        var block = Block.CreateChild(_accountId, _workspaceId, _pageId, BlockType.Text,
            BlockContent.Create(JsonValue.EmptyObject()), FractionalIndex.Create("a0"), _actorId, _now, path);
        block.ParentId.Should().Be(parentId);
    }
}
