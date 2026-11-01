using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Documents.Templates.Commands.CreatePageFromTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.CreatePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Queries.ListPageTemplates;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>Templates are creation input (DCT-018): a copy of a page's content that new pages are made from.</summary>
public sealed class PageTemplateHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static string Text(string text) => $"{{\"v\":1,\"runs\":[{{\"text\":\"{text}\",\"marks\":[]}}]}}";

    private sealed class Fixture
    {
        public Page Page { get; }
        public List<Block> Blocks { get; } = [];
        public List<PageTemplate> Templates { get; } = [];
        public List<Page> Pages { get; }
        public Mock<IResourceCreatorGrant> Grant { get; } = new();
        public IDocumentDbContext Context { get; }

        public Fixture(string? icon = null)
        {
            Page = Page.Create(AccountId, WorkspaceId, "Weekly sync", UserId, Now, icon: icon);
            Pages = [Page];
            var context = new Mock<IDocumentDbContext>();
            context.Setup(c => c.Pages).Returns(TestDbSet.Create(Pages).Object);
            context.Setup(c => c.Blocks).Returns(TestDbSet.Create(Blocks).Object);
            context.Setup(c => c.PageTemplates).Returns(TestDbSet.Create(Templates).Object);
            Context = context.Object;
        }

        public FakeCurrentRequestContext Request()
        {
            var request = new FakeCurrentRequestContext().AsUser(UserId);
            request.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
            return request;
        }

        public Block Add(string position, string text, BlockType type = BlockType.Text, Block? parent = null)
        {
            var content = BlockContent.Create(type is BlockType.Image or BlockType.File or BlockType.Video ? JsonValue.EmptyObject() : JsonValue.Create(Text(text)));
            var block = parent is null
                ? Block.CreateRoot(AccountId, WorkspaceId, Page.Id, type, content, FractionalIndex.Create(position), UserId, Now)
                : Block.CreateChild(AccountId, WorkspaceId, Page.Id, type, content, FractionalIndex.Create(position), UserId, Now,
                    BlockAncestorPath.Create(AccountId, WorkspaceId, Page.Id, parent.Id, []));
            Blocks.Add(block);
            return block;
        }
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(1);
    }

    [Fact]
    public async Task SavingAsATemplate_CopiesTheTitleAndLiveBlocks_AndPublishesItInTheWorkspace()
    {
        var f = new Fixture();
        var first = f.Add("a0", "agenda");
        f.Add("a0", "item", parent: first);
        f.Add("a1", "notes").Delete(UserId, Now);
        var sut = new CreatePageTemplateCommandHandler(f.Context, f.Request(), new Clock());

        var result = await sut.Handle(new CreatePageTemplateCommand(WorkspaceId, f.Page.Id, "  Weekly sync  ", "Every Monday"), CancellationToken.None);

        result.Data!.Name.Should().Be("Weekly sync");
        result.Data.Title.Should().Be("Weekly sync");
        result.Data.BlockCount.Should().Be(2);
        result.Data.Description.Should().Be("Every Monday");
        var template = f.Templates.Single();
        template.Status.Should().Be(PageTemplateStatus.Published);
        template.WorkspaceId.Should().Be(WorkspaceId);
        template.CreatedBy.Should().Be(UserId);
    }

    [Fact]
    public async Task APageOfAnotherWorkspace_CannotBeSavedAsATemplate()
    {
        var f = new Fixture();
        var sut = new CreatePageTemplateCommandHandler(f.Context, f.Request(), new Clock());

        var act = () => sut.Handle(new CreatePageTemplateCommand(Guid.CreateVersion7(), f.Page.Id, "x", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        f.Templates.Should().BeEmpty();
    }

    [Fact]
    public void TheTemplateCommandsAreAuthorizedOnThePageAndOnThePlacement()
    {
        var pageId = Guid.CreateVersion7();
        var save = new CreatePageTemplateCommand(WorkspaceId, pageId, "x", null);
        save.Action.Should().Be(PermissionAction.UpdatePage);
        save.Resource.ResourceId.Should().Be(pageId);

        var atRoot = new CreatePageFromTemplateCommand(WorkspaceId, Guid.CreateVersion7(), null, null);
        atRoot.Action.Should().Be(PermissionAction.CreatePage);
        atRoot.Resource.Kind.Value.Should().Be("workspaces.workspace");
        new CreatePageFromTemplateCommand(WorkspaceId, Guid.CreateVersion7(), null, pageId).Resource.ResourceId.Should().Be(pageId);
        new CreatePageFromTemplateCommand(WorkspaceId, Guid.CreateVersion7(), null, null, Guid.CreateVersion7()).Resource.Kind.Value.Should().Be("workspaces.space");
    }

    [Fact]
    public async Task TheList_HoldsThePublishedTemplatesOfTheWorkspaceByName()
    {
        var f = new Fixture();
        f.Templates.Add(Template("Zeta", WorkspaceId, publish: true));
        f.Templates.Add(Template("Alpha", WorkspaceId, publish: true));
        f.Templates.Add(Template("Draft", WorkspaceId, publish: false));
        f.Templates.Add(Template("Elsewhere", Guid.CreateVersion7(), publish: true));

        var result = await new ListPageTemplatesQueryHandler(f.Context).Handle(new ListPageTemplatesQuery(WorkspaceId), CancellationToken.None);

        result.Data!.Select(t => t.Name).Should().Equal("Alpha", "Zeta");
    }

    [Fact]
    public async Task CreatingFromATemplate_MakesAnOrdinaryPage_WithNewBlockIdsAndTheSameShape()
    {
        var f = new Fixture();
        var parent = f.Add("a0", "agenda");
        var child = f.Add("a0", "item", parent: parent);
        var other = f.Add("a1", "notes", BlockType.Heading1);
        var template = await Save(f);
        var (sut, added, pagesAdded) = CreateFromHandler(f, template);

        var result = await sut.Handle(new CreatePageFromTemplateCommand(WorkspaceId, template.Id, null, null), CancellationToken.None);

        result.Data!.DroppedBlockCount.Should().Be(0);
        var page = pagesAdded.Single();
        page.Id.Should().Be(result.Data.PageId);
        page.Title.Should().Be("Weekly sync");
        added.Should().HaveCount(3).And.OnlyContain(b => b.PageId == page.Id);
        added.Select(b => b.Id).Should().NotIntersectWith([parent.Id, child.Id, other.Id]);
        var newParent = added.Single(b => b.Position.Value == "a0" && b.ParentId is null);
        added.Single(b => b.ParentId == newParent.Id).Content.Data.Value.Should().Contain("item");
        added.Single(b => b.Type == BlockType.Heading1).Position.Value.Should().Be("a1");
        f.Grant.Verify(g => g.GrantManager(AccountId, WorkspaceId, "documents.page", page.Id, UserId, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public async Task CreatingFromATemplate_TakesTheIconOfTheTemplate()
    {
        var f = new Fixture("🧪");
        f.Add("a0", "kept");
        var template = await Save(f);
        var (sut, _, pagesAdded) = CreateFromHandler(f, template);

        await sut.Handle(new CreatePageFromTemplateCommand(WorkspaceId, template.Id, null, null), CancellationToken.None);

        pagesAdded.Single().Icon.Should().Be("🧪");
    }

    [Fact]
    public async Task CreatingFromATemplate_OfAPageWithTheDefaultIcon_KeepsTheDefaultIcon()
    {
        var f = new Fixture();
        var template = await Save(f);
        var (sut, _, pagesAdded) = CreateFromHandler(f, template);

        await sut.Handle(new CreatePageFromTemplateCommand(WorkspaceId, template.Id, null, null), CancellationToken.None);

        pagesAdded.Single().Icon.Should().Be(Page.DefaultIcon);
    }

    [Fact]
    public async Task CreatingFromATemplate_DropsImagesFilesAndVideos_WithTheBlocksUnderThem_AndSaysSo()
    {
        var f = new Fixture();
        f.Add("a0", "kept");
        var media = f.Add("a1", "x", BlockType.Image);
        f.Add("a0", "under media", parent: media);
        f.Add("a2", "x", BlockType.File);
        f.Add("a3", "x", BlockType.Video);
        var template = await Save(f);
        var (sut, added, _) = CreateFromHandler(f, template);

        var result = await sut.Handle(new CreatePageFromTemplateCommand(WorkspaceId, template.Id, "My copy", null), CancellationToken.None);

        result.Data!.DroppedBlockCount.Should().Be(4);
        added.Should().ContainSingle().Which.Content.Data.Value.Should().Contain("kept");
    }

    [Fact]
    public async Task TheNewPage_TakesTheGivenTitle()
    {
        var f = new Fixture();
        var template = await Save(f);
        var (sut, _, pagesAdded) = CreateFromHandler(f, template);

        await sut.Handle(new CreatePageFromTemplateCommand(WorkspaceId, template.Id, "  Sprint 12 ", null), CancellationToken.None);

        pagesAdded.Single().Title.Should().Be("Sprint 12");
    }

    [Fact]
    public async Task ATemplateOfAnotherWorkspace_OrADraft_IsNotFound()
    {
        var f = new Fixture();
        var foreign = Template("Foreign", Guid.CreateVersion7(), publish: true);
        var draft = Template("Draft", WorkspaceId, publish: false);
        f.Templates.AddRange([foreign, draft]);
        var (sut, added, pagesAdded) = CreateFromHandler(f, foreign);

        foreach (var template in new[] { foreign, draft })
        {
            var act = () => sut.Handle(new CreatePageFromTemplateCommand(WorkspaceId, template.Id, null, null), CancellationToken.None);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        added.Should().BeEmpty();
        pagesAdded.Should().BeEmpty();
    }

    private static PageTemplate Template(string name, Guid workspaceId, bool publish)
    {
        var template = PageTemplate.Create(name, JsonValue.Create("{\"title\":\"" + name + "\",\"icon\":\"x\"}"), JsonValue.EmptyArray(), Now, workspaceId, UserId);
        if (publish) template.Publish(UserId, Now);
        return template;
    }

    private static async Task<PageTemplate> Save(Fixture f)
    {
        await new CreatePageTemplateCommandHandler(f.Context, f.Request(), new Clock())
            .Handle(new CreatePageTemplateCommand(WorkspaceId, f.Page.Id, "T", null), CancellationToken.None);
        return f.Templates.Single();
    }

    private static (CreatePageFromTemplateCommandHandler Sut, List<Block> Added, List<Page> PagesAdded) CreateFromHandler(Fixture f, PageTemplate _)
    {
        var added = new List<Block>();
        var pagesAdded = new List<Page>();
        var blockSet = TestDbSet.Create<Block>();
        blockSet.Setup(s => s.Add(It.IsAny<Block>())).Callback<Block>(added.Add);
        var pageSet = TestDbSet.Create(f.Pages);
        pageSet.Setup(s => s.Add(It.IsAny<Page>())).Callback<Page>(pagesAdded.Add);
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(pageSet.Object);
        context.Setup(c => c.Blocks).Returns(blockSet.Object);
        context.Setup(c => c.PageTemplates).Returns(TestDbSet.Create(f.Templates).Object);
        var sut = new CreatePageFromTemplateCommandHandler(context.Object, new Mock<IPageSpacePlacement>().Object, new InMemoryPageSubtreeStore(), f.Grant.Object, f.Request(), new Clock());
        return (sut, added, pagesAdded);
    }
}
