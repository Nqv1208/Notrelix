using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardItemFieldValueChangedConsumer : IConsumer<BoardItemFieldValueChangedIntegrationEvent>
{
    private readonly ILogger<BoardItemFieldValueChangedConsumer> _logger;

    public BoardItemFieldValueChangedConsumer(ILogger<BoardItemFieldValueChangedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardItemFieldValueChangedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardItemFieldValueChanged: ItemId={ItemId}, FieldId={FieldId}, OldValue={OldValue}, NewValue={NewValue}",
            context.Message.ItemId,
            context.Message.FieldId,
            context.Message.OldValue,
            context.Message.NewValue);
        return Task.CompletedTask;
    }
}
