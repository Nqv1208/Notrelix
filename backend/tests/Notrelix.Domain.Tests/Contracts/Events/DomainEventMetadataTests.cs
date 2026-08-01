using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Tests.Contracts.Events;

public class DomainEventMetadataTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static IEnumerable<Type> DiscoverDomainEvents()
    {
        return DomainAssembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false }
                && typeof(IDomainEvent).IsAssignableFrom(type)
                && type.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true)
            .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    [Fact]
    public void EveryDomainEvent_ShouldDeclareEventName()
    {
        var missing = DiscoverDomainEvents()
            .Where(type => type.GetCustomAttribute<EventNameAttribute>() is null)
            .Select(type => type.FullName!)
            .ToList();

        missing.Should().BeEmpty("every concrete Domain event must declare an EventNameAttribute. Missing:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void LogicalNames_ShouldBeUnique()
    {
        var duplicates = DiscoverDomainEvents()
            .Select(type => type.GetCustomAttribute<EventNameAttribute>()!.Name)
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        duplicates.Should().BeEmpty("Domain event logical names must be unique; duplicates found: " + string.Join(", ", duplicates));
    }

    [Fact]
    public void EventVersion_ShouldBeAtLeastOne()
    {
        var violations = DiscoverDomainEvents()
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<EventNameAttribute>()!))
            .Where(x => x.Attribute.Version < 1)
            .Select(x => $"{x.Type.FullName} (version {x.Attribute.Version})")
            .ToList();

        violations.Should().BeEmpty("every Domain event must declare version >= 1. Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void AccountScopedEvents_ShouldExposeAccountId()
    {
        var violations = DiscoverDomainEvents()
            .Where(type => typeof(IAccountScoped).IsAssignableFrom(type))
            .Where(type => type.GetProperty(nameof(IAccountScoped.AccountId), BindingFlags.Public | BindingFlags.Instance) is null)
            .Select(type => type.FullName!)
            .ToList();

        violations.Should().BeEmpty("every account-scoped Domain event must expose an AccountId property. Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void WorkspaceScopedEvents_ShouldExposeWorkspaceId()
    {
        var violations = DiscoverDomainEvents()
            .Where(type => typeof(IWorkspaceScoped).IsAssignableFrom(type))
            .Where(type => type.GetProperty(nameof(IWorkspaceScoped.WorkspaceId), BindingFlags.Public | BindingFlags.Instance) is null)
            .Select(type => type.FullName!)
            .ToList();

        violations.Should().BeEmpty("every workspace-scoped Domain event must expose a WorkspaceId property. Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void DomainEvents_ShouldBeDiscovered()
    {
        DiscoverDomainEvents().Should().NotBeEmpty("the Domain assembly must contain at least one concrete Domain event");
    }
}