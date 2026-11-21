using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.SharedKernel.Ordering;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// The automatic version taken when editing goes quiet (owner decision 2026-11-15): five minutes without a block
/// save, unsnapshotted edits only, idempotent, and the ten-minute rule stays separate.
/// </summary>
public sealed class PageQuietVersionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid Editor = Guid.CreateVersion7();
    private static readonly TimeSpan Lookback = TimeSpan.FromMinutes(30);

    private static string Text(string text) => $"{{\"v\":1,\"runs\":[{{\"text\":\"{text}\",\"marks\":[]}}]}}";

    private sealed class Fixture
    {
        public List<Page> Pages { get; } = [];
        public List<Block> Blocks { get; } = [];
        public List<DocumentVersion> Versions { get; } = [];
        public PageVersionRecorder Recorder { get; }

        public Fixture()
        {
            var context = new Mock<IDocumentDbContext>();
            context.Setup(c => c.Pages).Returns(TestDbSet.Create(Pages).Object);
            context.Setup(c => c.Blocks).Returns(TestDbSet.Create(Blocks).Object);
            context.Setup(c => c.DocumentVersions).Returns(TestDbSet.Create(Versions).Object);
            Recorder = new PageVersionRecorder(context.Object);
        }

        public Page AddPage(params (string Text, DateTimeOffset At)[] saves)
        {
            var page = Page.Create(AccountId, WorkspaceId, "Plan", Editor, T0);
            Pages.Add(page);
            for (var i = 0; i < saves.Length; i++)
            {
                Blocks.Add(Block.CreateRoot(
                    AccountId, WorkspaceId, page.Id, BlockType.Text,
                    BlockContent.Create(JsonValue.Create(Text(saves[i].Text))),
                    FractionalIndex.Create($"a{i}"), Editor, saves[i].At));
            }

            return page;
        }
    }

    [Fact]
    public async Task QuietPages_NamesAPageFiveMinutesAfterItsLastSave_NotBeforeAndNotPastTheLookback()
    {
        var f = new Fixture();
        var quiet = f.AddPage(("one", T0));
        var typing = f.AddPage(("two", T0.AddMinutes(3)));
        var old = f.AddPage(("three", T0.AddHours(-3)));
        var now = T0.AddMinutes(6);

        var pages = await f.Recorder.QuietPagesAsync(now, Lookback, 10, CancellationToken.None);

        pages.Should().Equal(quiet.Id);
        pages.Should().NotContain([typing.Id, old.Id]);
    }

    [Fact]
    public async Task QuietPages_ANewestSaveKeepsThePageOut_EvenWhenItsOlderBlocksAreQuiet()
    {
        var f = new Fixture();
        f.AddPage(("one", T0), ("two", T0.AddMinutes(4)));

        var pages = await f.Recorder.QuietPagesAsync(T0.AddMinutes(6), Lookback, 10, CancellationToken.None);

        pages.Should().BeEmpty("the second block was saved two minutes ago");
    }

    [Fact]
    public async Task CaptureWhenQuiet_AddsOneAutomaticEntryAsTheEditor_AndRepeatingItAddsNone()
    {
        var f = new Fixture();
        var page = f.AddPage(("one", T0));
        var now = T0.AddMinutes(6);

        var first = await f.Recorder.CaptureWhenQuietAsync(page.Id, now, CancellationToken.None);
        var second = await f.Recorder.CaptureWhenQuietAsync(page.Id, now.AddMinutes(1), CancellationToken.None);

        first.Should().NotBeNull();
        first!.Kind.Should().Be(DocumentVersionKind.Automatic);
        first.VersionNumber.Should().Be(1);
        first.CreatedBy.Should().Be(Editor);
        second.Should().BeNull("the content equals the newest entry");
        f.Versions.Should().ContainSingle();
    }

    [Fact]
    public async Task CaptureWhenQuiet_IgnoresTheTenMinuteRule_AfterAnEntryTwoMinutesAgo()
    {
        var f = new Fixture();
        var page = f.AddPage(("one", T0));
        f.Versions.Add(DocumentVersion.Create(AccountId, WorkspaceId, page.Id, 1,
            DocumentSnapshot.Create(JsonValue.Create(PageVersionSnapshot.Build("Other title", []))), Editor, T0.AddMinutes(4)));

        var version = await f.Recorder.CaptureWhenQuietAsync(page.Id, T0.AddMinutes(6), CancellationToken.None);

        version.Should().NotBeNull("five quiet minutes are enough, the entry before it is only two minutes old");
        version!.VersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task CaptureWhenQuiet_DoesNothingWhileEditingContinues()
    {
        var f = new Fixture();
        var page = f.AddPage(("one", T0.AddMinutes(3)));

        var version = await f.Recorder.CaptureWhenQuietAsync(page.Id, T0.AddMinutes(6), CancellationToken.None);

        version.Should().BeNull("the last save was three minutes ago");
        f.Versions.Should().BeEmpty();
    }

    [Fact]
    public async Task CaptureAutomatic_KeepsTheTenMinuteRule()
    {
        var f = new Fixture();
        var page = f.AddPage(("one", T0));
        f.Versions.Add(DocumentVersion.Create(AccountId, WorkspaceId, page.Id, 1,
            DocumentSnapshot.Create(JsonValue.Create(PageVersionSnapshot.Build("Other title", []))), Editor, T0.AddMinutes(4)));

        var tooSoon = await f.Recorder.CaptureAutomaticAsync(page.Id, Editor, T0.AddMinutes(9), CancellationToken.None);
        var old = await f.Recorder.CaptureAutomaticAsync(page.Id, Editor, T0.AddMinutes(15), CancellationToken.None);

        tooSoon.Should().BeNull();
        old.Should().NotBeNull();
    }
}
