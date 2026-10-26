using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;
using Notrelix.Application.Features.Documents.Versions.Commands.RestorePageVersion;
using Notrelix.Application.Features.Documents.Versions.Queries.GetPageVersion;
using Notrelix.Application.Features.Documents.Versions.Queries.ListPageVersions;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Pages.Events;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// History of a page: named versions, the read-only content of an entry, restore (new entry, current blocks
/// rewritten, nothing lost, guarded like a block save) and the automatic entry policy.
/// </summary>
public sealed class PageVersionHandlerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static string Text(string text) => $"{{\"v\":1,\"runs\":[{{\"text\":\"{text}\",\"marks\":[]}}]}}";

    private static Block NewBlock(Page page, string position, string text, BlockType type = BlockType.Text) =>
        Block.CreateRoot(AccountId, WorkspaceId, page.Id, type, BlockContent.Create(JsonValue.Create(Text(text))), FractionalIndex.Create(position), UserId, T0);

    private sealed class Fixture
    {
        public Page Page { get; } = Page.Create(AccountId, WorkspaceId, "Plan", UserId, T0);
        public List<Block> Blocks { get; } = [];
        public List<DocumentVersion> Versions { get; } = [];
        public DateTimeOffset Now { get; set; } = T0.AddMinutes(30);
        public IDocumentDbContext Context { get; }
        public PageVersionRecorder Recorder { get; }

        public Fixture()
        {
            var versionSet = TestDbSet.Create(Versions);
            var blockSet = TestDbSet.Create(Blocks);
            var context = new Mock<IDocumentDbContext>();
            context.Setup(c => c.Pages).Returns(TestDbSet.Create([Page]).Object);
            context.Setup(c => c.Blocks).Returns(blockSet.Object);
            context.Setup(c => c.DocumentVersions).Returns(versionSet.Object);
            Context = context.Object;
            Recorder = new PageVersionRecorder(Context);
        }

        public FakeCurrentRequestContext Request()
        {
            var request = new FakeCurrentRequestContext().AsUser(UserId);
            request.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
            return request;
        }

        public IDateTimeProvider Clock => new FixedClock(this);

        private sealed class FixedClock(Fixture owner) : IDateTimeProvider
        {
            public DateTimeOffset UtcNow => owner.Now;
        }
    }

    [Fact]
    public async Task SavingANamedVersion_CapturesTheLiveBlocksInTreeOrder_AndNumbersTheEntry()
    {
        var f = new Fixture();
        var first = NewBlock(f.Page, "a0", "one");
        var second = NewBlock(f.Page, "a1", "two");
        var child = Block.CreateChild(AccountId, WorkspaceId, f.Page.Id, BlockType.Text, BlockContent.Create(JsonValue.Create(Text("kid"))),
            FractionalIndex.Create("a0"), UserId, T0, BlockAncestorPath.Create(AccountId, WorkspaceId, f.Page.Id, first.Id, []));
        var gone = NewBlock(f.Page, "a2", "deleted");
        gone.Delete(UserId, T0);
        f.Blocks.AddRange([second, child, gone, first]);
        var sut = new CreatePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);

        var result = await sut.Handle(new CreatePageVersionCommand(f.Page.Id, "  Before review "), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Kind.Should().Be("named");
        result.Data.Name.Should().Be("Before review");
        result.Data.VersionNumber.Should().Be(1);
        var saved = f.Versions.Single();
        var (title, blocks) = PageVersionSnapshot.Parse(saved.Snapshot.Data!.Value);
        title.Should().Be("Plan");
        blocks.Select(b => b.Id).Should().Equal(first.Id, child.Id, second.Id);
        blocks[1].ParentId.Should().Be(first.Id);
    }

    [Fact]
    public async Task EachEntryGetsTheNextNumberOfItsPage()
    {
        var f = new Fixture();
        f.Blocks.Add(NewBlock(f.Page, "a0", "one"));
        var sut = new CreatePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);

        await sut.Handle(new CreatePageVersionCommand(f.Page.Id, "v1"), CancellationToken.None);
        var second = await sut.Handle(new CreatePageVersionCommand(f.Page.Id, "v2"), CancellationToken.None);

        second.Data!.VersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task ANamedVersion_NeedsAName_AndAnArchivedPageTakesNone()
    {
        var f = new Fixture();
        var validator = new CreatePageVersionCommandValidator();
        validator.Validate(new CreatePageVersionCommand(f.Page.Id, " ")).IsValid.Should().BeFalse();
        validator.Validate(new CreatePageVersionCommand(f.Page.Id, new string('x', 201))).IsValid.Should().BeFalse();

        f.Page.Archive(UserId, T0);
        var sut = new CreatePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);
        var act = () => sut.Handle(new CreatePageVersionCommand(f.Page.Id, "x"), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotEditArchived);
    }

    [Fact]
    public async Task TheList_IsNewestFirst_AndNamesTheKindOfEachEntry()
    {
        var f = new Fixture();
        f.Versions.Add(Entry(f, 1, DocumentVersionKind.Automatic, null));
        f.Versions.Add(Entry(f, 2, DocumentVersionKind.Named, "Draft"));
        f.Versions.Add(Entry(f, 3, DocumentVersionKind.Restore, "Restored from version 1"));

        var result = await new ListPageVersionsQueryHandler(f.Context).Handle(new ListPageVersionsQuery(f.Page.Id), CancellationToken.None);

        result.Data!.Select(v => (v.VersionNumber, v.Kind, v.Name)).Should().Equal(
            (3, "restore", "Restored from version 1"), (2, "named", "Draft"), (1, "automatic", null));
    }

    [Fact]
    public async Task ReadingAnEntry_ReturnsItsCapturedBlocks_ButOnlyThroughItsOwnPage()
    {
        var f = new Fixture();
        var block = NewBlock(f.Page, "a0", "kept");
        f.Versions.Add(Entry(f, 1, DocumentVersionKind.Automatic, null, [block]));
        var sut = new GetPageVersionQueryHandler(f.Context);

        var result = await sut.Handle(new GetPageVersionQuery(f.Page.Id, f.Versions[0].Id), CancellationToken.None);

        result.Data!.Title.Should().Be("Plan");
        result.Data.Blocks.Single().Id.Should().Be(block.Id);
        result.Data.Blocks.Single().Content.GetProperty("runs")[0].GetProperty("text").GetString().Should().Be("kept");

        var otherPage = () => sut.Handle(new GetPageVersionQuery(f.Page.Id, Guid.CreateVersion7()), CancellationToken.None);
        await otherPage.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Restoring_RewritesTheBlocks_AddsARestoreEntry_AndKeepsWhatItReplaced()
    {
        var f = new Fixture();
        var kept = NewBlock(f.Page, "a0", "old text");
        var removedLater = NewBlock(f.Page, "a1", "will be removed");
        f.Blocks.AddRange([kept, removedLater]);
        f.Versions.Add(Entry(f, 1, DocumentVersionKind.Named, "Good state", [kept, removedLater]));

        // The page moves on: edit one block, delete another, add a new one.
        kept.Edit(null, BlockContent.Create(JsonValue.Create(Text("new text"))), null, UserId, T0.AddMinutes(1));
        removedLater.Delete(UserId, T0.AddMinutes(2));
        var added = NewBlock(f.Page, "a2", "added later");
        f.Blocks.Add(added);
        var sut = new RestorePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);

        var result = await sut.Handle(new RestorePageVersionCommand(f.Page.Id, f.Versions[0].Id,
            [new SeenBlockVersion(kept.Id, kept.Version), new SeenBlockVersion(added.Id, added.Version)]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        kept.Content.Data.Value.Should().Contain("old text");
        removedLater.IsDeleted.Should().BeFalse("a deleted block the version holds is brought back");
        removedLater.Content.Data.Value.Should().Contain("will be removed");
        added.IsDeleted.Should().BeTrue("a block the version does not hold goes away");
        result.Data!.Blocks.Select(b => b.Id).Should().Equal(kept.Id, removedLater.Id);

        f.Versions.Select(v => (v.VersionNumber, v.Kind)).Should().Equal(
            (1, DocumentVersionKind.Named), (2, DocumentVersionKind.Automatic), (3, DocumentVersionKind.Restore));
        f.Versions[1].ChangeSummary.Should().Be("Before restoring version 1");
        PageVersionSnapshot.Parse(f.Versions[1].Snapshot.Data!.Value).Blocks.Select(b => b.Id)
            .Should().Equal(kept.Id, added.Id);
        f.Versions[2].ChangeSummary.Should().Be("Restored from version 1");
        result.Data.Version.Kind.Should().Be("restore");
        f.Page.DomainEvents.OfType<PageBlocksSavedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Restoring_WhenTheCurrentContentIsAlreadyTheNewestEntry_AddsNoSafetyEntry()
    {
        var f = new Fixture();
        var block = NewBlock(f.Page, "a0", "same");
        f.Blocks.Add(block);
        f.Versions.Add(Entry(f, 1, DocumentVersionKind.Named, "Now", [block]));
        var sut = new RestorePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);

        await sut.Handle(new RestorePageVersionCommand(f.Page.Id, f.Versions[0].Id, [new SeenBlockVersion(block.Id, block.Version)]), CancellationToken.None);

        f.Versions.Select(v => v.Kind).Should().Equal(DocumentVersionKind.Named, DocumentVersionKind.Restore);
    }

    [Fact]
    public async Task Restoring_AgainstBlocksThatChanged_Answers412WithTheCurrentBlocks_AndChangesNothing()
    {
        var f = new Fixture();
        var block = NewBlock(f.Page, "a0", "one");
        f.Blocks.Add(block);
        f.Versions.Add(Entry(f, 1, DocumentVersionKind.Named, "Start", [block]));
        var sut = new RestorePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);
        var seen = block.Version;
        block.Edit(null, BlockContent.Create(JsonValue.Create(Text("someone else"))), null, UserId, T0.AddMinutes(3));

        var act = () => sut.Handle(new RestorePageVersionCommand(f.Page.Id, f.Versions[0].Id, [new SeenBlockVersion(block.Id, seen)]), CancellationToken.None);

        var stale = (await act.Should().ThrowAsync<PreconditionFailedException>()).Which;
        stale.ErrorCode.Should().Be("documents.version.restore-conflict");
        stale.Extensions.Should().ContainKey("currentBlocks");
        block.Content.Data.Value.Should().Contain("someone else");
        f.Versions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Restoring_WithABlockMissingFromWhatTheCallerSaw_IsRefusedToo()
    {
        var f = new Fixture();
        var seen = NewBlock(f.Page, "a0", "one");
        var unseen = NewBlock(f.Page, "a1", "two");
        f.Blocks.AddRange([seen, unseen]);
        f.Versions.Add(Entry(f, 1, DocumentVersionKind.Named, "Start", [seen]));
        var sut = new RestorePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);

        var act = () => sut.Handle(new RestorePageVersionCommand(f.Page.Id, f.Versions[0].Id, [new SeenBlockVersion(seen.Id, seen.Version)]), CancellationToken.None);

        await act.Should().ThrowAsync<PreconditionFailedException>();
        unseen.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Restoring_KeepsTheTreeOfTheVersion()
    {
        var f = new Fixture();
        var parent = NewBlock(f.Page, "a0", "parent");
        var child = Block.CreateChild(AccountId, WorkspaceId, f.Page.Id, BlockType.Text, BlockContent.Create(JsonValue.Create(Text("child"))),
            FractionalIndex.Create("a0"), UserId, T0, BlockAncestorPath.Create(AccountId, WorkspaceId, f.Page.Id, parent.Id, []));
        f.Blocks.AddRange([parent, child]);
        f.Versions.Add(Entry(f, 1, DocumentVersionKind.Named, "Tree", [parent, child]));
        child.MoveToRoot(FractionalIndex.Create("a5"), UserId, T0.AddMinutes(1));
        var sut = new RestorePageVersionCommandHandler(f.Context, f.Recorder, f.Request(), f.Clock);

        await sut.Handle(new RestorePageVersionCommand(f.Page.Id, f.Versions[0].Id,
            [new SeenBlockVersion(parent.Id, parent.Version), new SeenBlockVersion(child.Id, child.Version)]), CancellationToken.None);

        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task TheAutomaticEntry_IsTakenOnlyWhenTheContentChangedAndTheLastEntryIsTenMinutesOld()
    {
        var f = new Fixture();
        var block = NewBlock(f.Page, "a0", "one");
        f.Blocks.Add(block);

        // No entry yet: the first one is taken.
        f.Now = T0.AddMinutes(1);
        (await f.Recorder.CaptureAutomaticAsync(f.Page.Id, UserId, f.Now, CancellationToken.None))!.VersionNumber.Should().Be(1);

        // Edited, but the last entry is young.
        block.Edit(null, BlockContent.Create(JsonValue.Create(Text("two"))), null, UserId, T0.AddMinutes(2));
        (await f.Recorder.CaptureAutomaticAsync(f.Page.Id, UserId, T0.AddMinutes(10), CancellationToken.None)).Should().BeNull();

        // Old enough and changed.
        var third = await f.Recorder.CaptureAutomaticAsync(f.Page.Id, UserId, T0.AddMinutes(12), CancellationToken.None);
        third!.VersionNumber.Should().Be(2);
        third.Kind.Should().Be(DocumentVersionKind.Automatic);

        // Old enough but nothing changed since.
        (await f.Recorder.CaptureAutomaticAsync(f.Page.Id, UserId, T0.AddMinutes(60), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task TheAutomaticEntry_SkipsAnEmptyNeverCapturedPage_AndADeletedOne()
    {
        var f = new Fixture();
        (await f.Recorder.CaptureAutomaticAsync(f.Page.Id, UserId, f.Now, CancellationToken.None)).Should().BeNull();

        f.Blocks.Add(NewBlock(f.Page, "a0", "one"));
        f.Page.Delete(UserId, T0);
        (await f.Recorder.CaptureAutomaticAsync(f.Page.Id, UserId, f.Now, CancellationToken.None)).Should().BeNull();
    }

    private static DocumentVersion Entry(Fixture f, int number, DocumentVersionKind kind, string? name, IEnumerable<Block>? blocks = null)
    {
        var json = PageVersionSnapshot.Build(f.Page.Title, blocks ?? []);
        return DocumentVersion.Create(AccountId, WorkspaceId, f.Page.Id, number, DocumentSnapshot.Create(JsonValue.Create(json)), UserId, T0.AddMinutes(number), name, kind);
    }
}
