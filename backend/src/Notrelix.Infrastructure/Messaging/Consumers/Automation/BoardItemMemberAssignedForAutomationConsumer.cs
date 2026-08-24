using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Events;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

public sealed class BoardItemMemberAssignedForAutomationConsumer
    : IConsumer<BoardItemMemberAssignedForAutomationIntegrationEvent>
{
    private readonly CheckN8nAutomationPostCommitAction _automation;

    public BoardItemMemberAssignedForAutomationConsumer(CheckN8nAutomationPostCommitAction automation)
    {
        _automation = automation;
    }

    public Task Consume(ConsumeContext<BoardItemMemberAssignedForAutomationIntegrationEvent> context) =>
        _automation.ExecuteAsync(context.Message, context.CancellationToken);
}
