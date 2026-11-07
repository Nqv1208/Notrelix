using System.IO.Compression;
using System.Text;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Transfer;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.Transfer;

/// <summary>
/// UC-WM-B31: the export follows the scope (the current view's filter, or every item), leaves a vote field out (like the other computed ones),
/// and writes text a person typed so a spreadsheet never runs it (the formula guard), in CSV and XLSX.
/// </summary>
public class BoardExportTests : WorkManagementHandlerTestBase
{
    private const string ViewFilter = "c2:and~$title.contains.Kho";

    private static BoardItemSummaryDto Summary(BoardItem item, string key) =>
        new(item.Id, item.Name, 0, [], [], 0, 0, 0, 0, "a0", DateTime.UtcNow, null, 1, ItemKey: key, ParentItemId: item.ParentItemId);

    private (BoardExportBuilder Builder, List<BoardItemCriteria> Seen, BoardItem Item) Arrange(Func<Guid, BoardField[]>? fields = null)
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        SetupBoardFields(fields?.Invoke(item.BoardId) ?? []);
        SetupBoardGroups();
        var seen = new List<BoardItemCriteria>();
        var pages = new Mock<IBoardItemPageReader>();
        pages.Setup(p => p.ReadAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CriteriaField?>(), It.IsAny<IReadOnlyList<CriteriaField>>(),
                It.IsAny<BoardItemCriteria>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, IReadOnlyList<Guid> _, CriteriaField? _, IReadOnlyList<CriteriaField> _, BoardItemCriteria criteria, string? _, string? _, int _, CancellationToken _) => seen.Add(criteria))
            .ReturnsAsync([new BoardItemLanePage("", [item.Id], 1, 1, null)]);
        var summaries = new Mock<IBoardItemSummaryBuilder>();
        summaries.Setup(b => b.BuildAsync(It.IsAny<IReadOnlyList<BoardField>>(), It.IsAny<IReadOnlyList<BoardItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BoardField> _, IReadOnlyList<BoardItem> items, CancellationToken _) =>
                items.Select(i => Summary(i, "ERP-1")).ToList());
        return (new BoardExportBuilder(DbContextMock.Object, pages.Object, summaries.Object, Mock.Of<IActorLookupService>()), seen, item);
    }

    [Fact]
    public async Task ViewScope_ReadsTheItemsThroughTheViewsFilter()
    {
        var (builder, seen, item) = Arrange();

        await builder.BuildAsync(item.BoardId, BoardExportBuilder.ScopeView, ViewFilter, null, "en", null, CancellationToken.None);

        seen.Should().NotBeEmpty().And.OnlyContain(c => c.HasFilter && c.Conditions.Single().FieldId == "$title");
    }

    [Fact]
    public async Task AllScope_IgnoresTheViewsFilter()
    {
        var (builder, seen, item) = Arrange();

        await builder.BuildAsync(item.BoardId, BoardExportBuilder.ScopeAll, ViewFilter, null, "en", null, CancellationToken.None);

        seen.Should().NotBeEmpty().And.OnlyContain(c => c == BoardItemCriteria.None);
    }

    [Fact]
    public async Task AVoteField_IsNotExported_AndOtherFieldsFollowTheBuiltInColumns()
    {
        var (builder, _, item) = Arrange(boardId =>
        [
            BoardField.Create(TestAccountId, TestWorkspaceId, boardId, "Effort", FieldType.Number, FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow),
            BoardField.Create(TestAccountId, TestWorkspaceId, boardId, "Votes", FieldType.Vote, FieldSettings.Empty(), FractionalIndex.Create("a1"), TestUserId, TestNow),
        ]);

        var table = await builder.BuildAsync(item.BoardId, BoardExportBuilder.ScopeAll, null, null, "en", null, CancellationToken.None);

        table.Headers.Should().Contain("Effort").And.NotContain("Votes");
        table.Headers.ToList().IndexOf("Effort").Should().Be(11, "the eleven built-in columns come first");
        table.Rows.Should().ContainSingle().Which.Length.Should().Be(table.Headers.Count);
    }

    [Theory]
    [InlineData("=SUM(A1:A9)")]
    [InlineData("+84 912")]
    [InlineData("-1")]
    [InlineData("@cmd")]
    [InlineData("\tTab")]
    public void Csv_PrefixesTextThatASpreadsheetWouldRun(string typed)
    {
        var csv = WriteCsv(["Title"], [[typed]]);

        csv.Split("\r\n")[1].Should().StartWith("'").And.Contain(typed.TrimStart('\t'));
    }

    [Fact]
    public void Csv_KeepsNumbersAndDates_StartsWithABom_AndQuotesSeparators()
    {
        using var stream = new MemoryStream();
        TabularWriter.Write(TabularWriter.Csv, ["Title", "Effort", "Due"],
            [["Kho, tháng 9", -2.5m, new DateTimeOffset(2026, 10, 16, 0, 0, 0, TimeSpan.Zero)]], stream);
        var bytes = stream.ToArray();

        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).Split("\r\n")[1]
            .Should().Be("\"Kho, tháng 9\",-2.5,2026-10-16", "a typed number is not text, so a negative one is not guarded");
    }

    [Fact]
    public void Xlsx_GuardsTextCells_InTheSheet()
    {
        using var stream = new MemoryStream();
        TabularWriter.Write(TabularWriter.Xlsx, ["Title"], [["=HYPERLINK(\"x\")"]], stream);
        stream.Position = 0;

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheet = reader.ReadToEnd();

        sheet.Should().Contain("&apos;=HYPERLINK(&quot;x&quot;)").And.NotContain("<f>");
    }

    private static string WriteCsv(IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
    {
        using var stream = new MemoryStream();
        TabularWriter.Write(TabularWriter.Csv, headers, rows, stream);
        var bytes = stream.ToArray();
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }
}
