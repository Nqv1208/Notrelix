using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// Minimal async DbSet stub backed by an in-memory list — supports the
/// FirstOrDefaultAsync/AnyAsync query surface the Collaboration handlers use
/// and captures added aggregates for assertions.
/// </summary>
internal static class TestDbSet
{
    public static Mock<DbSet<T>> Create<T>(List<T>? data = null) where T : class
    {
        data ??= [];
        var mock = new Mock<DbSet<T>>();
        var queryable = data.AsQueryable();

        mock.As<IAsyncEnumerable<T>>()
            .Setup(s => s.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable));
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

        mock.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        return mock;
    }
}

internal class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
}

internal class TestAsyncQueryProvider<T>(IQueryable<T> inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => inner.Provider.CreateQuery(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncEnumerable<TElement>(inner.Provider.CreateQuery<TElement>(expression));

    public object? Execute(Expression expression) => inner.Provider.Execute(expression);

    public TResult Execute<TResult>(Expression expression)
        => (TResult)inner.Provider.Execute(expression)!;

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = resultType.GenericTypeArguments[0];
            var sync = typeof(TestAsyncQueryProvider<T>)
                .GetMethod(nameof(ExecuteSync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(innerType);
            return (TResult)sync.Invoke(this, [expression])!;
        }

        throw new NotSupportedException($"Unsupported async result type: {resultType}");
    }

    private Task<TResult> ExecuteSync<TResult>(Expression expression)
    {
        var result = inner.Provider.Execute(expression);
        return Task.FromResult((TResult)result!);
    }
}

internal class TestAsyncEnumerable<T>(IQueryable<T> queryable)
    : IAsyncEnumerable<T>, IQueryable<T>
{
    public Type ElementType => queryable.ElementType;
    public Expression Expression => queryable.Expression;
    public IQueryProvider Provider { get; } = new TestAsyncQueryProvider<T>(queryable);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(queryable.GetEnumerator());

    public IEnumerator<T> GetEnumerator() => queryable.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => queryable.GetEnumerator();
}
