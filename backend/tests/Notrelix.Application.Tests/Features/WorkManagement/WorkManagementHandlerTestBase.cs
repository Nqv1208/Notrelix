using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Application.Tests.Features.WorkManagement;

public abstract class WorkManagementHandlerTestBase
{
    protected readonly Mock<IWorkManagementDbContext> DbContextMock = new();
    protected readonly Mock<ICurrentRequestContext> RequestContextMock = new();
    protected readonly Mock<IDateTimeProvider> DateTimeProviderMock = new();

    protected readonly Guid TestAccountId = Guid.CreateVersion7();
    protected readonly Guid TestWorkspaceId = Guid.CreateVersion7();
    protected readonly Guid TestUserId = Guid.CreateVersion7();
    protected readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    protected WorkManagementHandlerTestBase()
    {
        RequestContextMock.Setup(c => c.UserId).Returns(TestUserId);
        RequestContextMock.Setup(c => c.RequireAccountId()).Returns(TestAccountId);
        RequestContextMock.Setup(c => c.RequireWorkspaceId()).Returns(TestWorkspaceId);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow);

        DbContextMock.Setup(c => c.Boards).Returns(CreateAsyncDbSet(new List<Board>()));
        DbContextMock.Setup(c => c.BoardGroups).Returns(CreateAsyncDbSet(new List<BoardGroup>()));
        DbContextMock.Setup(c => c.BoardFields).Returns(CreateAsyncDbSet(new List<BoardField>()));
        DbContextMock.Setup(c => c.FieldOptions).Returns(CreateAsyncDbSet(new List<FieldOption>()));
        DbContextMock.Setup(c => c.BoardViews).Returns(CreateAsyncDbSet(new List<BoardView>()));
        DbContextMock.Setup(c => c.BoardViewPins).Returns(CreateAsyncDbSet(new List<BoardViewPin>()));
        DbContextMock.Setup(c => c.BoardViewUserPreferences).Returns(CreateAsyncDbSet(new List<BoardViewUserPreference>()));
        DbContextMock.Setup(c => c.SavedFilters).Returns(CreateAsyncDbSet(new List<SavedFilter>()));
        DbContextMock.Setup(c => c.BoardItems).Returns(CreateAsyncDbSet(new List<BoardItem>()));
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(new List<BoardItemValue>()));
        DbContextMock.Setup(c => c.BoardItemMembers).Returns(CreateAsyncDbSet(new List<BoardItemMember>()));
        DbContextMock.Setup(c => c.BoardItemLabels).Returns(CreateAsyncDbSet(new List<BoardItemLabel>()));
        DbContextMock.Setup(c => c.BoardItemLinks).Returns(CreateAsyncDbSet(new List<BoardItemLink>()));
        DbContextMock.Setup(c => c.Labels).Returns(CreateAsyncDbSet(new List<Label>()));
        DbContextMock.Setup(c => c.BoardMembers).Returns(CreateAsyncDbSet(new List<BoardMember>()));
        DbContextMock.Setup(c => c.BoardSubscribers).Returns(CreateAsyncDbSet(new List<BoardSubscriber>()));
        DbContextMock.Setup(c => c.Checklists).Returns(CreateAsyncDbSet(new List<Checklist>()));
        DbContextMock.Setup(c => c.ChecklistItems).Returns(CreateAsyncDbSet(new List<ChecklistItem>()));
    }

    protected void SetupBoards(params Board[] boards) =>
        DbContextMock.Setup(c => c.Boards).Returns(CreateAsyncDbSet(boards.ToList()));

    protected void SetupBoardMembers(params BoardMember[] members) =>
        DbContextMock.Setup(c => c.BoardMembers).Returns(CreateAsyncDbSet(members.ToList()));

    protected void SetupBoardGroups(params BoardGroup[] groups) =>
        DbContextMock.Setup(c => c.BoardGroups).Returns(CreateAsyncDbSet(groups.ToList()));

    protected void SetupBoardItems(params BoardItem[] items) =>
        DbContextMock.Setup(c => c.BoardItems).Returns(CreateAsyncDbSet(items.ToList()));

    protected void SetupBoardItemMembers(params BoardItemMember[] members) =>
        DbContextMock.Setup(c => c.BoardItemMembers).Returns(CreateAsyncDbSet(members.ToList()));

    protected void SetupBoardItemLabels(params BoardItemLabel[] labels) =>
        DbContextMock.Setup(c => c.BoardItemLabels).Returns(CreateAsyncDbSet(labels.ToList()));

    protected void SetupBoardItemLinks(params BoardItemLink[] links) =>
        DbContextMock.Setup(c => c.BoardItemLinks).Returns(CreateAsyncDbSet(links.ToList()));

    protected void SetupLabels(params Label[] labels) =>
        DbContextMock.Setup(c => c.Labels).Returns(CreateAsyncDbSet(labels.ToList()));

    protected void SetupChecklists(params Checklist[] checklists) =>
        DbContextMock.Setup(c => c.Checklists).Returns(CreateAsyncDbSet(checklists.ToList()));

    protected void SetupChecklistItems(params ChecklistItem[] items) =>
        DbContextMock.Setup(c => c.ChecklistItems).Returns(CreateAsyncDbSet(items.ToList()));

    protected void SetupBoardViews(params BoardView[] views) =>
        DbContextMock.Setup(c => c.BoardViews).Returns(CreateAsyncDbSet(views.ToList()));

    protected void SetupBoardFields(params BoardField[] fields) =>
        DbContextMock.Setup(c => c.BoardFields).Returns(CreateAsyncDbSet(fields.ToList()));

    protected Board CreateBoard(Guid? id = null, Guid? workspaceId = null)
    {
        var board = Board.Create(
            TestAccountId,
            workspaceId ?? TestWorkspaceId,
            TestUserId,
            "Test Board",
            null,
            TestNow,
            BoardVisibility.Workspace);
        if (id.HasValue)
            board.GetType().GetProperty(nameof(Board.Id))!.SetValue(board, id.Value);
        return board;
    }

    protected BoardItem CreateBoardItem(Guid? id = null, Guid? boardId = null, Guid? groupId = null)
    {
        var item = BoardItem.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? Guid.CreateVersion7(),
            groupId ?? Guid.CreateVersion7(),
            "Test Item",
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            item.GetType().GetProperty(nameof(BoardItem.Id))!.SetValue(item, id.Value);
        return item;
    }

    protected BoardItemLink CreateBoardItemLink(Guid? id = null, Guid? sourceItemId = null, Guid? targetItemId = null)
    {
        var link = BoardItemLink.Create(
            TestAccountId,
            TestWorkspaceId,
            Guid.CreateVersion7(),
            sourceItemId ?? Guid.CreateVersion7(),
            ResourceRef.Create(ResourceType.BoardItem, targetItemId ?? Guid.CreateVersion7(), TestWorkspaceId),
            BoardItemLinkType.Reference,
            TestUserId,
            TestNow);
        if (id.HasValue)
            link.GetType().GetProperty(nameof(BoardItemLink.Id))!.SetValue(link, id.Value);
        return link;
    }

    protected BoardItemMember CreateBoardItemMember(Guid? itemId = null, Guid? userId = null)
    {
        return BoardItemMember.Create(
            TestAccountId,
            TestWorkspaceId,
            Guid.CreateVersion7(),
            itemId ?? Guid.CreateVersion7(),
            userId ?? TestUserId,
            TestUserId,
            TestNow);
    }

    protected Checklist CreateChecklist(Guid? id = null, Guid? itemId = null)
    {
        var checklist = Checklist.Create(
            TestAccountId,
            TestWorkspaceId,
            itemId ?? Guid.CreateVersion7(),
            "Test Checklist",
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            checklist.GetType().GetProperty(nameof(Checklist.Id))!.SetValue(checklist, id.Value);
        return checklist;
    }

    private static DbSet<T> CreateAsyncDbSet<T>(List<T> data) where T : class
    {
        var mock = new Mock<DbSet<T>>();
        var queryable = data.AsQueryable();

        mock.As<IAsyncEnumerable<T>>()
            .Setup(s => s.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mock.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable));

        mock.As<IQueryable<T>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mock.As<IQueryable<T>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mock.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => data.GetEnumerator());

        mock.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        mock.Setup(m => m.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(items => data.AddRange(items));
        mock.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => data.Remove(item));

        return mock.Object;
    }
}

// ── Async query infrastructure ────────────────────────────────

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryable<T> inner) => _inner = inner.Provider;

    public IQueryable CreateQuery(Expression expression)
    {
        var queryable = _inner.CreateQuery<T>(expression);
        return new TestAsyncEnumerable<T>(queryable);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var queryable = _inner.CreateQuery<TElement>(expression);
        return new TestAsyncEnumerable<TElement>(queryable);
    }

    public object Execute(Expression expression) => _inner.Execute(expression)!;

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult);

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = resultType.GenericTypeArguments[0];

            var executeMethod = typeof(TestAsyncQueryProvider<T>)
                .GetMethod(nameof(ExecuteSync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(innerType);

            var taskResult = executeMethod.Invoke(this, [expression]);
            return (TResult)taskResult!;
        }

        throw new NotSupportedException($"Unsupported async result type: {resultType}");
    }

    private Task<TResult> ExecuteSync<TResult>(Expression expression)
    {
        var result = _inner.Execute(expression);
        return Task.FromResult((TResult)result!);
    }
}

internal class TestAsyncEnumerable<T> : IAsyncEnumerable<T>, IOrderedQueryable<T>
{
    private readonly IQueryable<T> _queryable;

    public TestAsyncEnumerable(IQueryable<T> queryable)
    {
        _queryable = queryable;
        Provider = new TestAsyncQueryProvider<T>(queryable);
    }

    public Type ElementType => _queryable.ElementType;
    public Expression Expression => _queryable.Expression;
    public IQueryProvider Provider { get; }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(_queryable.GetEnumerator());
    public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();
}
