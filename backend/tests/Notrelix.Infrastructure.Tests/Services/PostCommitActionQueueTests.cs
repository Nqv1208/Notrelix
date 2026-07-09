using Microsoft.Extensions.Logging;
using Notrelix.Infrastructure.Services;

namespace Notrelix.Infrastructure.Tests.Services;

public class PostCommitActionQueueTests
{
    [Fact]
    public void BeginScope_SetsScope()
    {
        var queue = CreateQueue();
        queue.BeginScope();
        queue.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Enqueue_AddsAction()
    {
        var queue = CreateQueue();
        queue.BeginScope();
        queue.Enqueue(new MockPostCommitAction());

        queue.Actions.Should().HaveCount(1);
    }

    [Fact]
    public void FlushAsync_WithoutScope_DoesNothing()
    {
        var queue = CreateQueue();
        queue.BeginScope();
        queue.Enqueue(new MockPostCommitAction());
        queue.Clear();
        queue.EndScope();

        var action = new MockPostCommitAction();
        queue.Enqueue(action);

        var act = () => queue.FlushAsync(default);
        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FlushAsync_ExecutesAllActions()
    {
        var queue = CreateQueue();
        queue.BeginScope();

        var executed = false;
        queue.Enqueue(new DelegatePostCommitAction(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }));

        await queue.FlushAsync(default);
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task FlushAsync_ActionFailure_DoesNotStopOtherActions()
    {
        var queue = CreateQueue();
        queue.BeginScope();

        var secondExecuted = false;

        queue.Enqueue(new DelegatePostCommitAction(_ =>
            throw new InvalidOperationException("first action failed")));

        queue.Enqueue(new DelegatePostCommitAction(_ =>
        {
            secondExecuted = true;
            return Task.CompletedTask;
        }));

        var act = () => queue.FlushAsync(default);
        await act.Should().NotThrowAsync();
        secondExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task FlushAsync_ClearsActionsAfterFlush()
    {
        var queue = CreateQueue();
        queue.BeginScope();
        queue.Enqueue(new DelegatePostCommitAction(_ => Task.CompletedTask));

        await queue.FlushAsync(default);

        queue.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Clear_RemovesAllActions()
    {
        var queue = CreateQueue();
        queue.BeginScope();
        queue.Enqueue(new DelegatePostCommitAction(_ => Task.CompletedTask));

        queue.Clear();

        queue.Actions.Should().BeEmpty();
    }

    [Fact]
    public async Task FlushAsync_WithMultipleActions_ExecutesAll()
    {
        var queue = CreateQueue();
        queue.BeginScope();

        var executionOrder = new List<int>();

        queue.Enqueue(new DelegatePostCommitAction(_ =>
        {
            executionOrder.Add(1);
            return Task.CompletedTask;
        }));
        queue.Enqueue(new DelegatePostCommitAction(_ =>
        {
            executionOrder.Add(2);
            return Task.CompletedTask;
        }));

        await queue.FlushAsync(default);

        executionOrder.Should().Equal(1, 2);
    }

    [Fact]
    public void EndScope_EndsScope()
    {
        var queue = CreateQueue();
        queue.BeginScope();
        queue.EndScope();

        queue.Actions.Should().BeEmpty();
    }

    private static PostCommitActionQueue CreateQueue()
    {
        return new PostCommitActionQueue(
            Mock.Of<ILogger<PostCommitActionQueue>>());
    }

    private sealed class MockPostCommitAction : IPostCommitAction
    {
        public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class DelegatePostCommitAction : IPostCommitAction
    {
        private readonly Func<CancellationToken, Task> _action;
        public DelegatePostCommitAction(Func<CancellationToken, Task> action) => _action = action;
        public Task ExecuteAsync(CancellationToken ct) => _action(ct);
    }
}
