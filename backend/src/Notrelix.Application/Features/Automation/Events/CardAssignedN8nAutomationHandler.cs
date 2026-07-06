namespace Notrelix.Application.Features.Automation.Events;

public class CardAssignedN8nAutomationHandler
    : INotificationHandler<DomainEventNotification<BoardItemMemberAssignedDomainEvent>>
{
    private readonly IPostCommitActionQueue _postCommit;
    private readonly IServiceProvider _services;

    public CardAssignedN8nAutomationHandler(
        IPostCommitActionQueue postCommit,
        IServiceProvider services)
    {
        _postCommit = postCommit;
        _services = services;
    }

    public Task Handle(
        DomainEventNotification<BoardItemMemberAssignedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        _postCommit.Enqueue(new DelegatePostCommitAction(async ct =>
        {
            var runner = ActivatorUtilities.CreateInstance<CheckN8nAutomationPostCommitAction>(
                _services, notification.DomainEvent);
            await runner.ExecuteAsync(ct);
        }));

        return Task.CompletedTask;
    }
}
