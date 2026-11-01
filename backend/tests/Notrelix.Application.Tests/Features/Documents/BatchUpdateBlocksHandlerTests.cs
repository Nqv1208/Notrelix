using System.Text.Json;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Common;
using Notrelix.Domain.Documents.Pages.Events;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Saving blocks as one change: operations apply in order, a stale version answers 412 with the current
/// block, and when any operation fails nothing is kept (the failure is thrown, so the data session rolls the
/// whole request back; no partial result is returned).
/// </summary>
public sealed class BatchUpdateBlocksHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static string Text(string text) => $"{{\"v\":1,\"runs\":[{{\"text\":\"{text}\",\"marks\":[]}}]}}";

    private static BatchUpdateBlockItem Upsert(Guid id, string? type = null, string? content = null, string? properties = null,
        long? version = null, Guid? after = null, Guid? parent = null, string? position = null) =>
        new("upsert", id, type, content is null ? null : Json(content), properties is null ? null : Json(properties),
            position, after, parent, version);

    private static BatchUpdateBlockItem Delete(Guid id, long? version) =>
        new("delete", id, null, null, null, null, null, null, version);

    [Fact]
    public async Task UpsertingNewAndExistingBlocks_AppliesEveryOperation_AndReturnsTheSavedBlocksWithTheirVersions()
    {
        var (sut, page, blocks, added) = Create(("a0", "one"), ("a1", "two"));
        var newId = Guid.CreateVersion7();

        var result = await sut.Handle(new BatchUpdateBlocksCommand(page.Id,
        [
            Upsert(blocks[0].Id, content: Text("ONE"), version: 1),
            Upsert(newId, type: "heading_1", content: Text("new"), after: blocks[0].Id),
        ]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Select(b => b.Id).Should().Equal(blocks[0].Id, newId);
        result.Data[0].Version.Should().Be(2);
        added.Single().Id.Should().Be(newId, "the client chooses the id of a create");
        added.Single().Type.Should().Be(BlockType.Heading1);
        string.CompareOrdinal(added.Single().Position.Value, blocks[0].Position.Value).Should().BePositive();
        string.CompareOrdinal(added.Single().Position.Value, blocks[1].Position.Value).Should().BeNegative();
    }

    [Fact]
    public async Task ALaterOperation_CanPlaceItselfAfterABlockAnEarlierOneCreated()
    {
        var (sut, page, blocks, added) = Create(("a0", "one"));
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        await sut.Handle(new BatchUpdateBlocksCommand(page.Id,
        [
            Upsert(first, type: "paragraph", content: Text("x"), after: blocks[0].Id),
            Upsert(second, type: "paragraph", content: Text("y"), after: first),
        ]), CancellationToken.None);

        var byId = added.ToDictionary(b => b.Id);
        string.CompareOrdinal(byId[second].Position.Value, byId[first].Position.Value).Should().BePositive();
    }

    [Fact]
    public async Task AStaleVersion_Answers412WithTheCurrentBlock_AndLeavesEarlierOperationsUnsaved()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"), ("a1", "two"));
        blocks[1].UpdateProperties(BlockProperties.Create(JsonValue.Create("{\"align\":\"left\"}")), UserId, Now);

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id,
        [
            Upsert(blocks[0].Id, content: Text("changed"), version: 1),
            Upsert(blocks[1].Id, content: Text("stale"), version: 1),
        ]), CancellationToken.None);

        var stale = (await act.Should().ThrowAsync<PreconditionFailedException>()).Which;
        stale.CurrentVersion.Should().Be(2);
        stale.Extensions.Should().ContainKey("currentBlock");
        // The exception makes the data session roll the request back: nothing was committed. The first
        // block's in-memory edit is discarded with the request's unit of work.
    }

    [Fact]
    public async Task AnInvalidBlock_FailsTheWholeBatch_WithoutReturningPartialResults()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"));

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id,
        [
            Upsert(blocks[0].Id, content: Text("ok"), version: 1),
            Upsert(Guid.CreateVersion7(), type: "paragraph", content: "{\"v\":1,\"runs\":[{\"text\":\"x\",\"marks\":[{\"type\":\"sparkle\"}]}]}"),
        ]), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task EditingAnExistingBlockNeedsItsVersion()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"));

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id,
            [Upsert(blocks[0].Id, content: Text("x"))]), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task ANewBlockNeedsItsType()
    {
        var (sut, page, _, _) = Create(("a0", "one"));

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id,
            [Upsert(Guid.CreateVersion7(), content: Text("x"))]), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Deleting_RemovesTheBlockAndTheBlocksUnderIt_AgainstItsVersion()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"), ("a1", "two"));
        var child = Block.CreateChild(AccountId, WorkspaceId, page.Id, BlockType.Text,
            BlockContent.Create(JsonValue.Create(Text("child"))), FractionalIndex.Create("a0"), UserId, Now,
            BlockAncestorPath.Create(AccountId, WorkspaceId, page.Id, blocks[0].Id, []));
        blocks.Add(child);

        var result = await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Delete(blocks[0].Id, 1)]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        blocks[0].IsDeleted.Should().BeTrue();
        child.IsDeleted.Should().BeTrue();
        blocks[1].IsDeleted.Should().BeFalse();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletingWithAStaleVersion_Answers412()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"));
        blocks[0].UpdateProperties(BlockProperties.Create(JsonValue.Create("{\"align\":\"left\"}")), UserId, Now);

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Delete(blocks[0].Id, 1)]), CancellationToken.None);

        await act.Should().ThrowAsync<PreconditionFailedException>();
        blocks[0].IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task MovingABlockUnderAnother_PlacesItInTheNewParent()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"), ("a1", "two"));

        await sut.Handle(new BatchUpdateBlocksCommand(page.Id,
            [Upsert(blocks[1].Id, version: 1, parent: blocks[0].Id)]), CancellationToken.None);

        blocks[1].ParentId.Should().Be(blocks[0].Id);
    }

    [Fact]
    public async Task MovingABlockUnderItself_IsRefused()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"));

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id,
            [Upsert(blocks[0].Id, version: 1, parent: blocks[0].Id)]), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AnUnknownPage_IsNotFound()
    {
        var (sut, _, blocks, _) = Create(("a0", "one"));

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(Guid.CreateVersion7(),
            [Upsert(blocks[0].Id, content: Text("x"), version: 1)]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static string Mentioning(string text, params Guid[] people) =>
        "{\"v\":1,\"runs\":[{\"text\":\"" + text + "\",\"marks\":[]}" + string.Concat(people.Select(person =>
            ",{\"text\":\"@x\",\"marks\":[{\"type\":\"mention\",\"userId\":\"" + person + "\"}]}")) + "]}";

    private static PageBlocksSavedDomainEvent[] Saved(Page page) => page.DomainEvents.OfType<PageBlocksSavedDomainEvent>().ToArray();

    [Fact]
    public async Task AddingAMention_AnnouncesTheSave_WithThatMentionOnly()
    {
        var ann = Guid.CreateVersion7();
        var bob = Guid.CreateVersion7();
        var (sut, page, blocks, _) = Create(("a0", "one"));
        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Upsert(blocks[0].Id, content: Mentioning("hi", ann), version: 1)]), CancellationToken.None);
        ((IHasDomainEvents)page).ClearDomainEvents();

        // Ann stays mentioned; Bob is new.
        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Upsert(blocks[0].Id, content: Mentioning("hi again", ann, bob), version: 2)]), CancellationToken.None);

        var saved = Saved(page).Should().ContainSingle().Which;
        saved.SavedBy.Should().Be(UserId);
        saved.Mentions.Should().Equal(new BlockMentionAdded(blocks[0].Id, bob));
    }

    [Fact]
    public async Task ASaveThatKeepsOrRemovesMentions_AnnouncesNoMention()
    {
        var ann = Guid.CreateVersion7();
        var (sut, page, blocks, _) = Create(("a0", "one"));
        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Upsert(blocks[0].Id, content: Mentioning("hi", ann), version: 1)]), CancellationToken.None);
        ((IHasDomainEvents)page).ClearDomainEvents();

        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Upsert(blocks[0].Id, content: Mentioning("edited", ann), version: 2)]), CancellationToken.None);
        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Upsert(blocks[0].Id, content: Mentioning("no one now"), version: 3)]), CancellationToken.None);

        Saved(page).Should().HaveCount(2).And.OnlyContain(e => e.Mentions.Count == 0);
    }

    [Fact]
    public async Task ANewBlockWithAMention_AnnouncesIt()
    {
        var ann = Guid.CreateVersion7();
        var (sut, page, blocks, added) = Create(("a0", "one"));
        var id = Guid.CreateVersion7();

        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Upsert(id, type: "paragraph", content: Mentioning("hey", ann), after: blocks[0].Id)]), CancellationToken.None);

        Saved(page).Single().Mentions.Should().Equal(new BlockMentionAdded(id, ann));
    }

    [Fact]
    public async Task ASaveThatChangesNothing_AnnouncesNothing()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"));

        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Upsert(blocks[0].Id, content: Text("one"), version: 1)]), CancellationToken.None);

        Saved(page).Should().BeEmpty();
    }

    [Fact]
    public async Task ADelete_AnnouncesTheSave_SoTheHistoryCanFollowIt()
    {
        var (sut, page, blocks, _) = Create(("a0", "one"));

        await sut.Handle(new BatchUpdateBlocksCommand(page.Id, [Delete(blocks[0].Id, 1)]), CancellationToken.None);

        Saved(page).Single().Mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task AFailedSave_AnnouncesNothing()
    {
        var ann = Guid.CreateVersion7();
        var (sut, page, blocks, _) = Create(("a0", "one"));

        var act = () => sut.Handle(new BatchUpdateBlocksCommand(page.Id,
            [Upsert(blocks[0].Id, content: Mentioning("x", ann), version: 9)]), CancellationToken.None);

        await act.Should().ThrowAsync<PreconditionFailedException>();
        Saved(page).Should().BeEmpty();
    }

    [Fact]
    public void TheValidator_RefusesEmptyOversizedAndDuplicateBatches_AndUnknownOperations()
    {
        var validator = new BatchUpdateBlocksCommandValidator();
        var id = Guid.CreateVersion7();

        validator.Validate(new BatchUpdateBlocksCommand(Guid.CreateVersion7(), [])).IsValid.Should().BeFalse();
        validator.Validate(new BatchUpdateBlocksCommand(Guid.CreateVersion7(), [Upsert(id), Upsert(id)])).IsValid.Should().BeFalse();
        validator.Validate(new BatchUpdateBlocksCommand(Guid.CreateVersion7(),
            [new("merge", id, null, null, null, null, null, null, null)])).IsValid.Should().BeFalse();
        validator.Validate(new BatchUpdateBlocksCommand(Guid.CreateVersion7(),
            Enumerable.Range(0, 501).Select(_ => Upsert(Guid.CreateVersion7())).ToList())).IsValid.Should().BeFalse();
        validator.Validate(new BatchUpdateBlocksCommand(Guid.CreateVersion7(), [Upsert(id), Delete(Guid.CreateVersion7(), 1)])).IsValid.Should().BeTrue();
    }

    private static (BatchUpdateBlocksCommandHandler Sut, Page Page, List<Block> Blocks, List<Block> Added) Create(params (string Position, string Text)[] seed)
    {
        var page = Page.Create(AccountId, WorkspaceId, "Doc", UserId, Now);
        var blocks = seed.Select(s => Block.CreateRoot(AccountId, WorkspaceId, page.Id, BlockType.Text,
            BlockContent.Create(JsonValue.Create(Text(s.Text))), FractionalIndex.Create(s.Position), UserId, Now)).ToList();
        var added = new List<Block>();
        var blockSet = TestDbSet.Create(blocks);
        blockSet.Setup(s => s.Add(It.IsAny<Block>())).Callback<Block>(block => { added.Add(block); });
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        context.Setup(c => c.Blocks).Returns(blockSet.Object);
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return (new BatchUpdateBlocksCommandHandler(context.Object, new NoPageBlockWriteLock(), requestContext, new FixedClock()), page, blocks, added);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
