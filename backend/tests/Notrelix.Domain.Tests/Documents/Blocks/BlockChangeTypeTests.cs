using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Domain.Tests.Documents;

public class BlockChangeTypeTests
{
    private static Block NewBlock(BlockType type, string json)
    {
        var block = Block.CreateRoot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), type,
            BlockContent.Create(JsonValue.Create(json)), FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)block).ClearDomainEvents();
        return block;
    }

    [Fact]
    public void ChangeType_BetweenTextLikeTypes_KeepsIdAndText()
    {
        var block = NewBlock(BlockType.Text, "{\"text\":\"Hello\",\"bold\":true}");
        var id = block.Id;
        var position = block.Position;
        var version = block.Version;

        block.ChangeType(BlockType.Heading2, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Id.Should().Be(id);
        block.Position.Should().Be(position);
        block.Type.Should().Be(BlockType.Heading2);
        block.Content.Data.Value.Should().Be("{\"text\":\"Hello\",\"bold\":true}");
        block.Version.Should().Be(version + 1);
        block.DomainEvents.OfType<BlockContentUpdatedDomainEvent>().Should().ContainSingle()
            .Which.BlockType.Should().Be(BlockType.Heading2);
    }

    [Fact]
    public void ChangeType_AwayFromToDo_DropsTheCheckedState()
    {
        var block = NewBlock(BlockType.ToDo, "{\"text\":\"Ship\",\"checked\":true}");

        block.ChangeType(BlockType.Text, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Content.Data.Value.Should().Be("{\"text\":\"Ship\"}");
    }

    [Fact]
    public void ChangeType_ToANonTextShape_StartsEmpty()
    {
        var block = NewBlock(BlockType.Text, "{\"text\":\"Hello\"}");

        block.ChangeType(BlockType.PageReference, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Type.Should().Be(BlockType.PageReference);
        block.Content.Data.Value.Should().Be("{}");
    }

    [Fact]
    public void ChangeType_FromANonTextShape_StartsEmpty()
    {
        var target = Guid.NewGuid();
        var block = NewBlock(BlockType.PageReference, $"{{\"linkedPageId\":\"{target}\"}}");

        block.ChangeType(BlockType.Callout, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Type.Should().Be(BlockType.Callout);
        block.Content.Data.Value.Should().Be("{}");
    }

    [Fact]
    public void ChangeType_ToTheSameType_IsANoOp()
    {
        var block = NewBlock(BlockType.Quote, "{\"text\":\"Hi\"}");
        var version = block.Version;

        block.ChangeType(BlockType.Quote, Guid.NewGuid(), DateTimeOffset.UtcNow);

        block.Version.Should().Be(version);
        block.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeType_OnADeletedBlock_Throws()
    {
        var block = NewBlock(BlockType.Text, "{}");
        block.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => block.ChangeType(BlockType.Quote, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<Exception>();
        block.Type.Should().Be(BlockType.Text);
    }

    [Fact]
    public void PageReference_WithAMalformedTarget_IsRejected()
    {
        var act = () => NewBlock(BlockType.PageReference, "{\"linkedPageId\":\"not-a-page\"}");

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Block_PageReferenceTargetInvalid);
    }

    [Fact]
    public void PageReference_WithAPageId_IsAccepted()
    {
        var act = () => NewBlock(BlockType.PageReference, $"{{\"linkedPageId\":\"{Guid.NewGuid()}\"}}");

        act.Should().NotThrow();
    }
}
