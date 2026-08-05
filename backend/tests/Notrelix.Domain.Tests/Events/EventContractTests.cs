using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Events;

public class EventContractTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;

    private static IEnumerable<Type> GetConcreteDomainEvents()
    {
        return DomainAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true })
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t))
            .Where(t => !t.Name.StartsWith("<")); // exclude compiler-generated
    }

    [Fact]
    public void AllConcreteEvents_ShouldHaveEventNameAttribute()
    {
        var events = GetConcreteDomainEvents();
        var missing = events
            .Where(e => e.GetCustomAttribute<EventNameAttribute>() is null)
            .Select(e => e.Name)
            .ToList();

        missing.Should().BeEmpty(
            $"all concrete domain events must have [EventName]. Missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EventNames_ShouldBeUnique()
    {
        var events = GetConcreteDomainEvents();
        var names = events
            .Select(e => e.GetCustomAttribute<EventNameAttribute>())
            .Where(a => a is not null)
            .Select(a => (a!.Name, a.Version))
            .ToList();

        var duplicates = names
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            $"event (Name, Version) pairs must be unique. Duplicates: {string.Join(", ", duplicates.Select(d => $"{d.Name}@v{d.Version}"))}");
    }

    [Fact]
    public void EventNames_ShouldNotBeEmpty()
    {
        var events = GetConcreteDomainEvents();
        var emptyNames = events
            .Select(e => e.GetCustomAttribute<EventNameAttribute>())
            .Where(a => a is not null)
            .Where(a => string.IsNullOrWhiteSpace(a!.Name))
            .ToList();

        emptyNames.Should().BeEmpty();
    }

    [Fact]
    public void EventVersions_ShouldBePositive()
    {
        var events = GetConcreteDomainEvents();
        var invalidVersions = events
            .Select(e => e.GetCustomAttribute<EventNameAttribute>())
            .Where(a => a is not null)
            .Where(a => a!.Version <= 0)
            .ToList();

        invalidVersions.Should().BeEmpty();
    }

    [Fact]
    public void AllConcreteEvents_ShouldBeSealed()
    {
        var events = GetConcreteDomainEvents();
        var unsealed = events
            .Where(e => !e.IsSealed)
            .Select(e => e.Name)
            .ToList();

        unsealed.Should().BeEmpty(
            $"all concrete domain events must be sealed. Unsealed: {string.Join(", ", unsealed)}");
    }

    [Fact]
    public void AllEventClassNames_ShouldEndWithDomainEvent()
    {
        var events = GetConcreteDomainEvents();
        var badNames = events
            .Where(e => !e.Name.EndsWith("DomainEvent"))
            .Select(e => e.Name)
            .ToList();

        badNames.Should().BeEmpty(
            $"all domain event classes must end with 'DomainEvent'. Bad names: {string.Join(", ", badNames)}");
    }

    [Fact]
    public void EventNameFormat_ShouldFollowConvention()
    {
        var events = GetConcreteDomainEvents();
        var badFormats = events
            .Select(e => e.GetCustomAttribute<EventNameAttribute>())
            .Where(a => a is not null)
            .Where(a => !a!.Name.Contains('.'))
            .Select(a => a!.Name)
            .ToList();

        badFormats.Should().BeEmpty(
            $"event names must follow 'context.action' format. Bad: {string.Join(", ", badFormats)}");
    }
}
