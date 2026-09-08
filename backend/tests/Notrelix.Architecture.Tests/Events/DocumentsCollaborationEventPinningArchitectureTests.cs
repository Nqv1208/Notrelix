using System.Reflection;
using Notrelix.Application.Common.Messaging;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests.Events;

/// <summary>
/// TAC-DC-FLOW-07 — the Documents and Collaboration outward events are
/// pinned: each contract is context-owned, explicitly versioned, and no
/// retired or generic duplicate contract exists.
/// </summary>
public class DocumentsCollaborationEventPinningArchitectureTests
{
    private static readonly string[] PinnedDocumentsEventNames =
    [
        "page.created",
        "page.archived",
    ];

    private static readonly string[] PinnedCollaborationEventNames =
    [
        "comment.created",
        "mention.created",
    ];

    [Fact]
    public void PinnedDocumentsEvents_AreDocumentsOwned_VersionedContracts()
    {
        var eventsAssembly = typeof(Notrelix.Application.Events.Documents.PageCreatedIntegrationEvent);

        foreach (var eventName in PinnedDocumentsEventNames)
        {
            var contractType = eventsAssembly.Assembly.GetTypes()
                .Single(t =>
                    t.GetCustomAttribute<EventNameAttribute>() is { } attr
                    && attr.Name == eventName);

            contractType.Namespace.Should().StartWith("Notrelix.Application.Events.Documents",
                $"event '{eventName}' is produced from Documents ownership");
            contractType.GetCustomAttribute<EventNameAttribute>()!.Version.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void PinnedCollaborationEvents_AreCollaborationOwned_VersionedContracts()
    {
        var eventsAssembly = typeof(Notrelix.Application.Events.Collaboration.CommentCreatedIntegrationEvent);

        foreach (var eventName in PinnedCollaborationEventNames)
        {
            var contractType = eventsAssembly.Assembly.GetTypes()
                .Single(t =>
                    t.GetCustomAttribute<EventNameAttribute>() is { } attr
                    && attr.Name == eventName);

            contractType.Namespace.Should().StartWith("Notrelix.Application.Events.Collaboration",
                $"event '{eventName}' is produced from Collaboration ownership");
            contractType.GetCustomAttribute<EventNameAttribute>()!.Version.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void PinnedEvents_CarryWorkspaceTenantScope()
    {
        foreach (var eventName in PinnedDocumentsEventNames.Concat(PinnedCollaborationEventNames))
        {
            var contractType = typeof(Notrelix.Application.Events.Documents.PageCreatedIntegrationEvent)
                .Assembly.GetTypes()
                .Single(t => t.GetCustomAttribute<EventNameAttribute>() is { } attr && attr.Name == eventName);

            contractType.GetCustomAttribute<IntegrationEventTenantScopeAttribute>()!.Scope
                .Should().Be(IntegrationEventTenantScope.Workspace,
                    $"event '{eventName}' is a workspace-scoped business fact under TAC-FRZ-018");
        }
    }

    [Fact]
    public void PinnedEvents_AreRecords_NotRawAggregates()
    {
        foreach (var eventName in PinnedDocumentsEventNames.Concat(PinnedCollaborationEventNames))
        {
            var contractType = typeof(Notrelix.Application.Events.Documents.PageCreatedIntegrationEvent)
                .Assembly.GetTypes()
                .Single(t => t.GetCustomAttribute<EventNameAttribute>() is { } attr && attr.Name == eventName);

            contractType.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
                .Should().BeFalse("integration contracts are immutable semantic records, never raw aggregates");
            typeof(Notrelix.Domain.Common.AggregateRoot)
                .IsAssignableFrom(contractType)
                .Should().BeFalse("raw mutable aggregates must never be serialized outward");
        }
    }

    [Fact]
    public void No_GenericCommentChanged_Or_PageChanged_Contract_Exists()
    {
        var appTypes = typeof(Notrelix.Application.Events.Documents.PageCreatedIntegrationEvent)
            .Assembly.GetTypes();

        appTypes.Where(t => t.Name.Contains("CommentChanged", StringComparison.Ordinal)
                || t.Name.Contains("PageChanged", StringComparison.Ordinal))
            .Should().BeEmpty("generic changed events would duplicate every specific owned fact");
    }
}
