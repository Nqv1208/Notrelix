using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Events.Documents;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;
using Notrelix.Infrastructure.Messaging.Consumers.Documents;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// The reactions to a committed save of a page: a mention is recorded once per (save, block, person) for people
/// who can view the page and are not the author, and an automatic history entry follows the ten-minute policy.
/// </summary>
public sealed class PageBlocksSavedConsumersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid PageId = Guid.CreateVersion7();
    private static readonly Guid Author = Guid.CreateVersion7();
    private static readonly Guid Ann = Guid.CreateVersion7();
    private static readonly Guid Bob = Guid.CreateVersion7();
    private static readonly Guid BlockA = Guid.CreateVersion7();
    private static readonly Guid BlockB = Guid.CreateVersion7();

    private static PageBlocksSavedIntegrationEvent Saved(Guid save, params PageBlockMention[] mentions) =>
        SavedBy(Author, PageId, save, mentions);

    private static PageBlocksSavedIntegrationEvent SavedBy(Guid? actor, Guid pageId, Guid save, params PageBlockMention[] mentions) => new(
        Guid.CreateVersion7(), AccountId, pageId, WorkspaceId, "Plan", mentions, save, actor, null, Now);

    private static Mock<IPageViewAccess> Viewers(params Guid[] canView)
    {
        var access = new Mock<IPageViewAccess>();
        access.Setup(a => a.CanViewAsync(AccountId, WorkspaceId, PageId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, Guid _, Guid user, CancellationToken _) => canView.Contains(user));
        return access;
    }

    private static PageBlocksSavedMentionConsumer Mentions(ApplicationDbContext db, IPageViewAccess access) =>
        new(db, db, access, NullLogger<PageBlocksSavedMentionConsumer>.Instance);

    [Fact]
    public async Task ANewlyMentionedViewer_GetsOneMentionOnThePage_FromTheAuthor()
    {
        await using var db = CreateDb();
        var save = Guid.CreateVersion7();

        await Mentions(db, Viewers(Ann).Object).Consume(Context(Saved(save, new PageBlockMention(BlockA, Ann))));

        var mention = await db.PageMentions.SingleAsync();
        mention.MentionedId.Should().Be(Ann);
        mention.MentionedByUserId.Should().Be(Author);
        mention.Source.ResourceId.Should().Be(PageId);
        mention.Source.Kind.Value.Should().Be("documents.page");
        mention.Type.Should().Be(MentionType.User);
        mention.Id.Should().Be(PageBlocksSavedMentionConsumer.MentionId(save, PageId, BlockA, Ann));
    }

    [Fact]
    public async Task ARedeliveredSave_AddsNoSecondMention()
    {
        await using var db = CreateDb();
        var message = Saved(Guid.CreateVersion7(), new PageBlockMention(BlockA, Ann));
        var consumer = Mentions(db, Viewers(Ann).Object);

        await consumer.Consume(Context(message));
        await consumer.Consume(Context(SavedBy(Author, PageId, message.CorrelationId, new PageBlockMention(BlockA, Ann))));

        (await db.PageMentions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ASeparateSaveMentioningTheSamePersonAgain_IsAnotherMention()
    {
        await using var db = CreateDb();
        var consumer = Mentions(db, Viewers(Ann).Object);

        await consumer.Consume(Context(Saved(Guid.CreateVersion7(), new PageBlockMention(BlockA, Ann))));
        await consumer.Consume(Context(Saved(Guid.CreateVersion7(), new PageBlockMention(BlockA, Ann))));

        (await db.PageMentions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task TheAuthor_IsNeverTold_AndSomeoneWhoCannotViewThePageIsNotTold()
    {
        await using var db = CreateDb();
        var access = Viewers(Ann, Author);

        await Mentions(db, access.Object).Consume(Context(Saved(Guid.CreateVersion7(),
            new PageBlockMention(BlockA, Author), new PageBlockMention(BlockA, Bob), new PageBlockMention(BlockB, Ann))));

        (await db.PageMentions.Select(m => m.MentionedId).ToListAsync()).Should().Equal(Ann);
        access.Verify(a => a.CanViewAsync(AccountId, WorkspaceId, PageId, Author, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task APersonMentionedInTwoBlocks_IsCheckedOnce_AndGetsAMentionPerBlock()
    {
        await using var db = CreateDb();
        var access = Viewers(Ann);

        await Mentions(db, access.Object).Consume(Context(Saved(Guid.CreateVersion7(),
            new PageBlockMention(BlockA, Ann), new PageBlockMention(BlockB, Ann), new PageBlockMention(BlockA, Ann))));

        (await db.PageMentions.CountAsync()).Should().Be(2);
        access.Verify(a => a.CanViewAsync(AccountId, WorkspaceId, PageId, Ann, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ASaveWithoutMentions_DoesNothing()
    {
        await using var db = CreateDb();
        var access = Viewers(Ann);

        await Mentions(db, access.Object).Consume(Context(Saved(Guid.CreateVersion7())));

        (await db.PageMentions.CountAsync()).Should().Be(0);
        access.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AnEventWithoutAnAuthoritativeAuthor_RecordsNothing()
    {
        await using var db = CreateDb();
        var message = SavedBy(null, PageId, Guid.CreateVersion7(), new PageBlockMention(BlockA, Ann));

        await Mentions(db, Viewers(Ann).Object).Consume(Context(message));

        (await db.PageMentions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AViewCheckThatCannotResolveThePage_CountsAsNotViewable()
    {
        await using var db = CreateDb();
        var access = new Mock<IPageViewAccess>();
        access.Setup(a => a.CanViewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Notrelix.Application.Common.Exceptions.NotFoundException("Page", PageId));

        await Mentions(db, access.Object).Consume(Context(Saved(Guid.CreateVersion7(), new PageBlockMention(BlockA, Ann))));

        (await db.PageMentions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void TheMentionId_IsStableForTheSameFacts_AndDiffersWhenAnyOfThemDoes()
    {
        var save = Guid.CreateVersion7();
        var id = PageBlocksSavedMentionConsumer.MentionId(save, PageId, BlockA, Ann);

        PageBlocksSavedMentionConsumer.MentionId(save, PageId, BlockA, Ann).Should().Be(id);
        PageBlocksSavedMentionConsumer.MentionId(Guid.CreateVersion7(), PageId, BlockA, Ann).Should().NotBe(id);
        PageBlocksSavedMentionConsumer.MentionId(save, PageId, BlockB, Ann).Should().NotBe(id);
        PageBlocksSavedMentionConsumer.MentionId(save, PageId, BlockA, Bob).Should().NotBe(id);
    }

    [Fact]
    public async Task TheAutomaticEntry_IsAddedAfterASave_AndNotRepeatedByARedelivery()
    {
        await using var db = CreateDb();
        var page = SeedPage(db);
        var consumer = new PageBlocksSavedVersionConsumer(new PageVersionRecorder(db), db, NullLogger<PageBlocksSavedVersionConsumer>.Instance);
        var message = SavedBy(Author, page.Id, Guid.CreateVersion7());

        await consumer.Consume(Context(message));
        await consumer.Consume(Context(message));

        var version = await db.DocumentVersions.SingleAsync();
        version.PageId.Should().Be(page.Id);
        version.VersionNumber.Should().Be(1);
        version.CreatedBy.Should().Be(Author);
    }

    [Fact]
    public async Task TheAutomaticEntry_NeedsAnAuthoritativeActor()
    {
        await using var db = CreateDb();
        var page = SeedPage(db);
        var consumer = new PageBlocksSavedVersionConsumer(new PageVersionRecorder(db), db, NullLogger<PageBlocksSavedVersionConsumer>.Instance);

        await consumer.Consume(Context(SavedBy(null, page.Id, Guid.CreateVersion7())));

        (await db.DocumentVersions.CountAsync()).Should().Be(0);
    }

    private static Page SeedPage(ApplicationDbContext db)
    {
        var page = Page.Create(AccountId, WorkspaceId, "Plan", Author, Now.AddHours(-1));
        db.Pages.Add(page);
        db.Blocks.Add(Block.CreateRoot(AccountId, WorkspaceId, page.Id, BlockType.Text,
            BlockContent.Create(JsonValue.Create("{\"v\":1,\"runs\":[{\"text\":\"hi\",\"marks\":[]}]}")), FractionalIndex.Create("a0"), Author, Now.AddHours(-1)));
        db.SaveChanges();
        return page;
    }

    private static ConsumeContext<PageBlocksSavedIntegrationEvent> Context(PageBlocksSavedIntegrationEvent message)
    {
        var context = new Mock<ConsumeContext<PageBlocksSavedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-page-saved-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        // A consumer reads under the tenant restored from the event envelope; the model's workspace filter applies.
        var tenant = new Mock<ICurrentTenantContext>();
        tenant.SetupGet(t => t.IsSystemContext).Returns(false);
        tenant.SetupGet(t => t.AccountId).Returns(AccountId);
        tenant.SetupGet(t => t.WorkspaceId).Returns(WorkspaceId);
        return new TestApplicationDbContext(options, tenant.Object);
    }

    private sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
            modelBuilder.Entity<SearchDocumentRecord>().Ignore(x => x.SearchVector);
        }
    }
}
