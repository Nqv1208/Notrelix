using System.Reflection;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests.Events;

/// <summary>
/// TAC-WM-007 — the Work integration-event reference is pinned: the Work
/// item contracts are WorkManagement-owned records with explicit versioned
/// event names, no generic WorkChanged duplicate exists, and no retired
/// Automation-owned member-assigned contract remains.
/// </summary>
public class WorkIntegrationEventPinningArchitectureTests
{
    private static readonly string[] PinnedWorkItemEventNames =
    [
        "work-management.board-item-member-assigned",
        "board_item.moved",
        "board.item.created",
        "board_item.archived",
    ];

    [Fact]
    public void PinnedWorkItemEvents_AreWorkManagementOwned_VersionedContracts()
    {
        var eventsAssembly = typeof(Notrelix.Application.Events.WorkManagement.BoardItemMovedIntegrationEvent);

        foreach (var eventName in PinnedWorkItemEventNames)
        {
            var contractType = eventsAssembly.Assembly.GetTypes()
                .Single(t =>
                    t.GetCustomAttribute<EventNameAttribute>() is { } attr
                    && attr.Name == eventName);

            contractType.Namespace.Should().StartWith("Notrelix.Application.Events.WorkManagement",
                $"event '{eventName}' is produced from WorkManagement ownership");
            contractType.GetCustomAttribute<EventNameAttribute>()!.Version.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void BoardItemMemberAssignedContract_IsOwnedBy_WorkManagement_NotAutomation()
    {
        var contractType = typeof(Notrelix.Application.Events.WorkManagement.BoardItemMemberAssignedIntegrationEvent);

        contractType.Namespace.Should().StartWith("Notrelix.Application.Events.WorkManagement",
            "the member-assigned contract was migrated to WorkManagement ownership; the Automation-owned contract is retired");
        contractType.GetCustomAttribute<EventNameAttribute>()!.Name
            .Should().Be("work-management.board-item-member-assigned");
    }

    [Fact]
    public void No_GenericWorkChanged_Contract_Exists()
    {
        var appTypes = typeof(Notrelix.Application.Events.WorkManagement.BoardItemMovedIntegrationEvent)
            .Assembly.GetTypes();

        appTypes.Where(t => t.Name.Contains("WorkChanged", StringComparison.Ordinal))
            .Should().BeEmpty("a generic WorkChanged event would duplicate every specific Work fact");
    }

    [Fact]
    public void PinnedWorkItemEvents_AreRecords_NotRawAggregates()
    {
        foreach (var eventName in PinnedWorkItemEventNames)
        {
            var contractType = typeof(Notrelix.Application.Events.WorkManagement.BoardItemMovedIntegrationEvent)
                .Assembly.GetTypes()
                .Single(t => t.GetCustomAttribute<EventNameAttribute>() is { } attr && attr.Name == eventName);

            contractType.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
                .Should().BeFalse("TAC-WM-008: integration contracts are immutable semantic records, never raw aggregates");
            typeof(Notrelix.Domain.Common.AggregateRoot)
                .IsAssignableFrom(contractType)
                .Should().BeFalse("raw mutable Work aggregates must never be serialized outward");
        }
    }
}