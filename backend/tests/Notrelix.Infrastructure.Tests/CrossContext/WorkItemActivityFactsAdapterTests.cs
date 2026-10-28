using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.WorkManagement.Public.ItemActivity;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Infrastructure.CrossContext.Collaboration.WorkManagement;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.CrossContext;

/// <summary>
/// The Work-owned facts the Item history words a change from: which field values may be shown, the option names,
/// and Board names, all inside the asked Workspace only.
/// </summary>
public sealed class WorkItemActivityFactsAdapterTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _me = Guid.CreateVersion7();
    private readonly ApplicationDbContext _context = CreateContext();

    public void Dispose() => _context.Dispose();

    private WorkItemActivityFactsAdapter Adapter => new(_context);

    private BoardField AddField(Board board, string name, FieldType type, bool sensitive = false)
    {
        var field = BoardField.Create(
            _account, _workspace, board.Id, name, type, FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!),
            FractionalIndex.Create($"a{name.Length}"), _me, Now, isSensitive: sensitive);
        _context.BoardFields.Add(field);
        return field;
    }

    private Board AddBoard(string title)
    {
        var board = Board.Create(_account, _workspace, _me, title, null, Now, BoardVisibility.Workspace);
        _context.Boards.Add(board);
        return board;
    }

    [Fact]
    public async Task TheStatusField_IsOptionBacked_WithItsOptionNames()
    {
        var board = AddBoard("Work");
        var status = AddField(board, "Status", FieldType.Status);
        status.AddDefaultStatusOptions(_me, Now);
        await _context.SaveChangesAsync();

        var facts = await Adapter.FindFieldAsync(_workspace, status.Id, CancellationToken.None);

        facts!.IsStatus.Should().BeTrue();
        facts.ValueMode.Should().Be(WorkFieldValueMode.Option);
        facts.OptionNames.Values.Should().BeEquivalentTo(status.Options.Select(o => o.Name));
    }

    [Theory]
    [InlineData(FieldType.Text, false, WorkFieldValueMode.Plain)]
    [InlineData(FieldType.Number, false, WorkFieldValueMode.Plain)]
    [InlineData(FieldType.LongText, false, WorkFieldValueMode.Hidden)]
    [InlineData(FieldType.Person, false, WorkFieldValueMode.Hidden)]
    [InlineData(FieldType.Text, true, WorkFieldValueMode.Hidden)]
    public async Task AFieldsValuesAreShown_OnlyWhenTheyAreShortAndNotSensitive(FieldType type, bool sensitive, WorkFieldValueMode expected)
    {
        var board = AddBoard("Work");
        var field = AddField(board, "Custom", type, sensitive);
        await _context.SaveChangesAsync();

        var facts = await Adapter.FindFieldAsync(_workspace, field.Id, CancellationToken.None);

        facts!.ValueMode.Should().Be(expected);
        facts.IsStatus.Should().BeFalse();
    }

    [Fact]
    public async Task AFieldOrBoardOfAnotherWorkspace_IsNotFound()
    {
        var board = AddBoard("Work");
        var field = AddField(board, "Custom", FieldType.Text);
        await _context.SaveChangesAsync();
        var elsewhere = Guid.CreateVersion7();

        (await Adapter.FindFieldAsync(elsewhere, field.Id, CancellationToken.None)).Should().BeNull();
        (await Adapter.FindBoardNameAsync(elsewhere, board.Id, CancellationToken.None)).Should().BeNull();
        (await Adapter.FindBoardNameAsync(_workspace, board.Id, CancellationToken.None)).Should().Be("Work");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = new Notrelix.Testing.Application.Fakes.FakeCurrentTenantContext();
        tenant.SetSystem();
        return new FactsTestContext(options, tenant);
    }

    private sealed class FactsTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
