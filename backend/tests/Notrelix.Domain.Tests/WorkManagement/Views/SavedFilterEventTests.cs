using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Domain.Tests.WorkManagement.Views;

[CoversAggregate(typeof(SavedFilter))]
public class SavedFilterEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(SavedFilter), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SavedFilter_Rename_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.Rename("Renamed", Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterRenamedDomainEvent);
        var evt = (SavedFilterRenamedDomainEvent)filter.DomainEvents.Single(e => e is SavedFilterRenamedDomainEvent);
        evt.Name.Should().Be("Renamed");
    }

    [CoversMutation(typeof(SavedFilter), "UpdateGroup(Notrelix.Domain.WorkManagement.Views.GroupRule,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateSorts(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateFilters(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateVisibility(Notrelix.Domain.WorkManagement.Views.SavedFilterVisibility,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SavedFilter_UpdateVisibility_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateVisibility(SavedFilterVisibility.Public, Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterVisibilityUpdatedDomainEvent);
    }

    [CoversMutation(typeof(SavedFilter), "UpdateGroup(Notrelix.Domain.WorkManagement.Views.GroupRule,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateSorts(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateFilters(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateVisibility(Notrelix.Domain.WorkManagement.Views.SavedFilterVisibility,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SavedFilter_UpdateFilters_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateFilters(new[] { FilterRule.Create(Guid.NewGuid(), FilterOperator.NotEquals, "other") }, Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterFiltersUpdatedDomainEvent);
    }

    [CoversMutation(typeof(SavedFilter), "UpdateGroup(Notrelix.Domain.WorkManagement.Views.GroupRule,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateSorts(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateFilters(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateVisibility(Notrelix.Domain.WorkManagement.Views.SavedFilterVisibility,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SavedFilter_UpdateSorts_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateSorts(new[] { SortRule.Create(Guid.NewGuid(), SortDirection.Ascending) }, Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterSortsUpdatedDomainEvent);
    }

    [CoversMutation(typeof(SavedFilter), "UpdateGroup(Notrelix.Domain.WorkManagement.Views.GroupRule,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateSorts(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateFilters(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(SavedFilter), "UpdateVisibility(Notrelix.Domain.WorkManagement.Views.SavedFilterVisibility,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SavedFilter_UpdateGroup_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateGroup(GroupRule.Create(Guid.NewGuid()), Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterGroupUpdatedDomainEvent);
    }

    [CoversMutation(typeof(SavedFilter), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SavedFilter_SoftDelete_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.SoftDelete(Actor, Now);

        filter.IsDeleted.Should().BeTrue();
        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterSoftDeletedDomainEvent);
    }

    [CoversMutation(typeof(SavedFilter), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SavedFilter_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.SoftDelete(Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.SoftDelete(Actor, Now);

        filter.Version.Should().Be(version);
        filter.DomainEvents.Should().NotContain(e => e is SavedFilterSoftDeletedDomainEvent);
    }

    [CoversMutation(typeof(SavedFilter), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void SavedFilter_Restore_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.SoftDelete(Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.Restore(Actor, Now);

        filter.IsDeleted.Should().BeFalse();
        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterRestoredDomainEvent);
    }

    [CoversMutation(typeof(SavedFilter), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(SavedFilter), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SavedFilter_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(Guid.NewGuid(), WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        ((IHasDomainEvents)filter).ClearDomainEvents();
        var version = filter.Version;

        filter.Restore(Actor, Now);

        filter.Version.Should().Be(version);
        filter.DomainEvents.Should().NotContain(e => e is SavedFilterRestoredDomainEvent);
    }
}
