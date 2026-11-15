using Notrelix.Application.Events.Documents;
using Notrelix.Application.Features.Collaboration.Activity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

// Page activity: each Documents fact becomes one append-only row keyed by the source event id, so a
// redelivery adds nothing and readers sort by the fact's own time. Facts carry no page tree and no body.
internal static class PageActivityRows
{
    public const string NotApplicable =
        "Append-only page activity row keyed by the source event id with its own OccurredAt; rows do not supersede one another and readers sort by occurrence time.";

    public static Task Record(
        ItemActivityRecorder recorder, Guid? workspaceId, Guid pageId, string type, Guid eventId, Guid? actor,
        DateTimeOffset at, string title, string? pageTitle, IReadOnlyDictionary<string, string?>? data, CancellationToken ct) =>
        workspaceId is not { } workspace
            ? Task.CompletedTask
            : recorder.RecordAsync(
                workspace, pageId, null, type, eventId, actor, at, title, pageTitle, data, ct,
                resourceKind: PageActivityTypes.ResourceKind,
                subjectType: PageActivityTypes.SubjectType,
                sourceContext: PageActivityTypes.SourceContext);
}

[OrderingNotApplicable(PageActivityRows.NotApplicable)]
public sealed class PageRenamedActivityConsumer(ItemActivityRecorder recorder) : IConsumer<PageRenamedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PageRenamedIntegrationEvent> context)
    {
        var m = context.Message;
        var from = ItemChangeWords.Truncate(m.OldTitle);
        var to = ItemChangeWords.Truncate(m.NewTitle);
        return PageActivityRows.Record(recorder, m.WorkspaceId, m.PageId, PageActivityTypes.Renamed, m.EventId, m.ActorUserId,
            m.OccurredAt, $"renamed the page from \"{from}\" to \"{to}\"", to,
            new Dictionary<string, string?> { ["from"] = from, ["to"] = to }, context.CancellationToken);
    }
}

[OrderingNotApplicable(PageActivityRows.NotApplicable)]
public sealed class PageMovedActivityConsumer(ItemActivityRecorder recorder) : IConsumer<PageMovedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PageMovedIntegrationEvent> context)
    {
        var m = context.Message;
        return PageActivityRows.Record(recorder, m.WorkspaceId, m.PageId, PageActivityTypes.Moved, m.EventId, m.ActorUserId,
            m.OccurredAt, "moved the page", null, null, context.CancellationToken);
    }
}

[OrderingNotApplicable(PageActivityRows.NotApplicable)]
public sealed class PageTrashedActivityConsumer(ItemActivityRecorder recorder) : IConsumer<PageArchivedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PageArchivedIntegrationEvent> context)
    {
        var m = context.Message;
        return PageActivityRows.Record(recorder, m.WorkspaceId, m.PageId, PageActivityTypes.Trashed, m.EventId, m.ActorUserId,
            m.OccurredAt, "moved the page to the trash", null, null, context.CancellationToken);
    }
}

[OrderingNotApplicable(PageActivityRows.NotApplicable)]
public sealed class PageRestoredActivityConsumer(ItemActivityRecorder recorder) : IConsumer<PageRestoredIntegrationEvent>
{
    public Task Consume(ConsumeContext<PageRestoredIntegrationEvent> context)
    {
        var m = context.Message;
        return PageActivityRows.Record(recorder, m.WorkspaceId, m.PageId, PageActivityTypes.Restored, m.EventId, m.ActorUserId,
            m.OccurredAt, "restored the page", null, null, context.CancellationToken);
    }
}

public sealed class PageRenamedActivityConsumerDefinition : ItemActivityConsumerDefinition<PageRenamedActivityConsumer>
{
    public PageRenamedActivityConsumerDefinition() : base("notrelix-activity-page-renamed-v1") { }
}

public sealed class PageMovedActivityConsumerDefinition : ItemActivityConsumerDefinition<PageMovedActivityConsumer>
{
    public PageMovedActivityConsumerDefinition() : base("notrelix-activity-page-moved-v1") { }
}

public sealed class PageTrashedActivityConsumerDefinition : ItemActivityConsumerDefinition<PageTrashedActivityConsumer>
{
    public PageTrashedActivityConsumerDefinition() : base("notrelix-activity-page-trashed-v1") { }
}

public sealed class PageRestoredActivityConsumerDefinition : ItemActivityConsumerDefinition<PageRestoredActivityConsumer>
{
    public PageRestoredActivityConsumerDefinition() : base("notrelix-activity-page-restored-v1") { }
}
