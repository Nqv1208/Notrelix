using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.DuplicatePage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>Duplicate copies one page, not its subpages, in one command: new ids, same place, the caller as Manager.</summary>
public sealed class DuplicatePageHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static string Text(string text) => $"{{\"v\":1,\"runs\":[{{\"text\":\"{text}\",\"marks\":[]}}]}}";

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(1);
    }

    private sealed class Fixture
    {
        public Page Source { get; }
        public List<Page> Pages { get; }
        public List<Block> SourceBlocks { get; } = [];
        public List<Block> AddedBlocks { get; } = [];
        public List<Page> AddedPages { get; } = [];
        public Mock<IResourceCreatorGrant> Grant { get; } = new();
        public Mock<IPageSpacePlacement> Placement { get; } = new();
        public DuplicatePageCommandHandler Sut { get; }

        public Fixture(Guid? parentId = null, Guid? spaceId = null, string title = "Weekly sync", string? icon = null)
        {
            Source = Page.Create(AccountId, WorkspaceId, title, UserId, Now, parentId, spaceId, icon);
            Pages = [Source];
            var blockSet = TestDbSet.Create(SourceBlocks);
            blockSet.Setup(s => s.Add(It.IsAny<Block>())).Callback<Block>(AddedBlocks.Add);
            var pageSet = TestDbSet.Create(Pages);
            pageSet.Setup(s => s.Add(It.IsAny<Page>())).Callback<Page>(AddedPages.Add);
            var context = new Mock<IDocumentDbContext>();
            context.Setup(c => c.Pages).Returns(pageSet.Object);
            context.Setup(c => c.Blocks).Returns(blockSet.Object);
            var request = new FakeCurrentRequestContext().AsUser(UserId);
            request.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
            Sut = new DuplicatePageCommandHandler(context.Object, Placement.Object, new InMemoryPageSubtreeStore(), Grant.Object, request, new Clock());
        }

        public Block Add(string position, string text, BlockType type = BlockType.Text, Block? parent = null)
        {
            var content = BlockContent.Create(type is BlockType.Image or BlockType.File or BlockType.Video ? JsonValue.EmptyObject() : JsonValue.Create(Text(text)));
            var block = parent is null
                ? Block.CreateRoot(AccountId, WorkspaceId, Source.Id, type, content, FractionalIndex.Create(position), UserId, Now)
                : Block.CreateChild(AccountId, WorkspaceId, Source.Id, type, content, FractionalIndex.Create(position), UserId, Now,
                    BlockAncestorPath.Create(AccountId, WorkspaceId, Source.Id, parent.Id, []));
            SourceBlocks.Add(block);
            return block;
        }
    }

    [Fact]
    public async Task TheCopy_IsANewPage_WithNewBlockIds_TheSameShape_AndTheCallerAsManager()
    {
        var f = new Fixture(icon: "🧪");
        var parent = f.Add("a0", "agenda");
        var child = f.Add("a0", "item", parent: parent);
        var other = f.Add("a1", "notes", BlockType.Heading1);
        f.Add("a2", "gone").Delete(UserId, Now);

        var result = await f.Sut.Handle(new DuplicatePageCommand(WorkspaceId, f.Source.Id, null), CancellationToken.None);

        result.Data!.DroppedBlockCount.Should().Be(0);
        var page = f.AddedPages.Single();
        page.Id.Should().Be(result.Data.PageId).And.NotBe(f.Source.Id);
        page.Title.Should().Be("Copy of Weekly sync");
        page.Icon.Should().Be("🧪");
        f.AddedBlocks.Should().HaveCount(3).And.OnlyContain(b => b.PageId == page.Id);
        f.AddedBlocks.Select(b => b.Id).Should().NotIntersectWith([parent.Id, child.Id, other.Id]);
        var newParent = f.AddedBlocks.Single(b => b.Position.Value == "a0" && b.ParentId is null);
        f.AddedBlocks.Single(b => b.ParentId == newParent.Id).Content.Data.Value.Should().Contain("item");
        f.Grant.Verify(g => g.GrantManager(AccountId, WorkspaceId, "documents.page", page.Id, UserId, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public async Task TheCopy_SitsBesideTheOriginal_AndTheCallerMustBeAbleToCreateThere()
    {
        var parentId = Guid.CreateVersion7();
        var nested = new Fixture(parentId: parentId);
        await nested.Sut.Handle(new DuplicatePageCommand(WorkspaceId, nested.Source.Id, null), CancellationToken.None);
        nested.AddedPages.Single().ParentId.Should().Be(parentId);
        nested.Placement.Verify(p => p.AuthorizeParentAsync(WorkspaceId, parentId, It.IsAny<CancellationToken>()), Times.Once);

        var spaceId = Guid.CreateVersion7();
        var root = new Fixture(spaceId: spaceId);
        await root.Sut.Handle(new DuplicatePageCommand(WorkspaceId, root.Source.Id, null), CancellationToken.None);
        root.AddedPages.Single().SpaceId.Should().Be(spaceId);
        root.Placement.Verify(p => p.AuthorizeTargetAsync(WorkspaceId, spaceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ACallerWhoCannotCreateAtTheDestination_GetsNothingCopied()
    {
        var f = new Fixture();
        f.Add("a0", "kept");
        f.Placement.Setup(p => p.AuthorizeTargetAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("no"));

        var act = () => f.Sut.Handle(new DuplicatePageCommand(WorkspaceId, f.Source.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        f.AddedPages.Should().BeEmpty();
        f.AddedBlocks.Should().BeEmpty();
    }

    [Fact]
    public async Task Images_Files_AndVideos_AreLeftBehind_WithTheBlocksUnderThem_AndTheResultSaysSo()
    {
        var f = new Fixture();
        f.Add("a0", "kept");
        var media = f.Add("a1", "x", BlockType.Image);
        f.Add("a0", "under media", parent: media);
        f.Add("a2", "x", BlockType.File);
        f.Add("a3", "x", BlockType.Video);

        var result = await f.Sut.Handle(new DuplicatePageCommand(WorkspaceId, f.Source.Id, "  My copy "), CancellationToken.None);

        result.Data!.DroppedBlockCount.Should().Be(4);
        f.AddedPages.Single().Title.Should().Be("My copy");
        f.AddedBlocks.Should().ContainSingle().Which.Content.Data.Value.Should().Contain("kept");
    }

    [Fact]
    public async Task ATrashedPage_OrAPageOfAnotherWorkspace_IsNotFound_AndNothingIsCopied()
    {
        var trashed = new Fixture();
        trashed.Source.Delete(UserId, Now);
        var foreign = new Fixture();

        var inTrash = () => trashed.Sut.Handle(new DuplicatePageCommand(WorkspaceId, trashed.Source.Id, null), CancellationToken.None);
        var elsewhere = () => foreign.Sut.Handle(new DuplicatePageCommand(Guid.CreateVersion7(), foreign.Source.Id, null), CancellationToken.None);

        await inTrash.Should().ThrowAsync<NotFoundException>();
        await elsewhere.Should().ThrowAsync<NotFoundException>();
        trashed.AddedPages.Should().BeEmpty();
        foreign.AddedPages.Should().BeEmpty();
    }

    [Fact]
    public void TheCommand_NeedsReadOfTheSource_AndIsIdempotentAndValidated()
    {
        var pageId = Guid.CreateVersion7();
        var command = new DuplicatePageCommand(WorkspaceId, pageId, null);
        command.Action.Should().Be(PermissionAction.ViewPage);
        command.Resource.ResourceId.Should().Be(pageId);
        command.Should().BeAssignableTo<IIdempotentRequest>();

        var validator = new DuplicatePageCommandValidator();
        validator.Validate(command).IsValid.Should().BeTrue();
        validator.Validate(command with { Title = new string('x', 201) }).IsValid.Should().BeFalse();
        validator.Validate(command with { PageId = Guid.Empty }).IsValid.Should().BeFalse();
    }
}
