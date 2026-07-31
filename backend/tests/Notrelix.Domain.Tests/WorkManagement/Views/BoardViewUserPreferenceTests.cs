using FluentAssertions;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardViewUserPreferenceTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSucceed()
    {
        var viewId = Guid.NewGuid();

        var pref = BoardViewUserPreference.Create(Guid.NewGuid(), WorkspaceId, BoardId, viewId, UserId, DateTimeOffset.UtcNow);

        pref.WorkspaceId.Should().Be(WorkspaceId);
        pref.BoardId.Should().Be(BoardId);
        pref.ViewId.Should().Be(viewId);
        pref.UserId.Should().Be(UserId);
        pref.DomainEvents.Should().ContainSingle(e => e is BoardViewUserPreferenceCreatedDomainEvent);
    }

    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyGroup), MutationScenario.Valid, typeof(GroupRule), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplySort), MutationScenario.Valid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyFilter), MutationScenario.Valid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>), typeof(DateTimeOffset))]
    [Fact]
    public void ApplyFilter_ShouldUpdateFilterRules()
    {
        var pref = BoardViewUserPreference.Create(Guid.NewGuid(), WorkspaceId, BoardId, Guid.NewGuid(), UserId, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)pref).ClearDomainEvents();

        var rules = new[] { FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "value") };
        pref.ApplyFilter(rules, DateTimeOffset.UtcNow);

        pref.FilterRules.Should().HaveCount(1);
        pref.DomainEvents.Should().ContainSingle(e => e is BoardViewUserPreferenceFilterChangedDomainEvent);
    }

    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyGroup), MutationScenario.Valid, typeof(GroupRule), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplySort), MutationScenario.Valid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyFilter), MutationScenario.Valid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>), typeof(DateTimeOffset))]
    [Fact]
    public void ApplySort_ShouldUpdateSortRules()
    {
        var pref = BoardViewUserPreference.Create(Guid.NewGuid(), WorkspaceId, BoardId, Guid.NewGuid(), UserId, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)pref).ClearDomainEvents();

        var sorts = new[] { SortRule.Create(Guid.NewGuid(), SortDirection.Ascending) };
        pref.ApplySort(sorts, DateTimeOffset.UtcNow);

        pref.SortRules.Should().HaveCount(1);
        pref.DomainEvents.Should().ContainSingle(e => e is BoardViewUserPreferenceSortChangedDomainEvent);
    }

    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyGroup), MutationScenario.Valid, typeof(GroupRule), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplySort), MutationScenario.Valid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyFilter), MutationScenario.Valid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>), typeof(DateTimeOffset))]
    [Fact]
    public void ApplyGroup_ShouldSetGroupRule()
    {
        var pref = BoardViewUserPreference.Create(Guid.NewGuid(), WorkspaceId, BoardId, Guid.NewGuid(), UserId, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)pref).ClearDomainEvents();

        var group = GroupRule.Create(Guid.NewGuid());
        pref.ApplyGroup(group, DateTimeOffset.UtcNow);

        pref.GroupRule.Should().NotBeNull();
        pref.GroupRule!.FieldId.Should().Be(group.FieldId);
        pref.DomainEvents.Should().ContainSingle(e => e is BoardViewUserPreferenceGroupChangedDomainEvent);
    }

    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyGroup), MutationScenario.Invalid, typeof(GroupRule), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplySort), MutationScenario.Invalid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>), typeof(DateTimeOffset))]
    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyFilter), MutationScenario.Invalid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>), typeof(DateTimeOffset))]
    [Fact]
    public void ApplyGroup_WithNull_ShouldClearGroup()
    {
        var pref = BoardViewUserPreference.Create(Guid.NewGuid(), WorkspaceId, BoardId, Guid.NewGuid(), UserId, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)pref).ClearDomainEvents();

        pref.ApplyGroup(null, DateTimeOffset.UtcNow);

        pref.GroupRule.Should().BeNull();
    }

    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplyFilter), MutationScenario.Invalid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.FilterRule>), typeof(DateTimeOffset))]
    [Fact]
    public void DuplicateFilter_ShouldThrow()
    {
        var pref = BoardViewUserPreference.Create(Guid.NewGuid(), WorkspaceId, BoardId, Guid.NewGuid(), UserId, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)pref).ClearDomainEvents();

        var fieldId = Guid.NewGuid();
        var rules = new[]
        {
            FilterRule.Create(fieldId, FilterOperator.Equals, "a"),
            FilterRule.Create(fieldId, FilterOperator.NotEquals, "b")
        };

        var act = () => pref.ApplyFilter(rules, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Duplicate filter rules for the same field are not allowed.");
    }

    [CoversMutation(typeof(BoardViewUserPreference), nameof(BoardViewUserPreference.ApplySort), MutationScenario.Invalid, typeof(System.Collections.Generic.IEnumerable<Notrelix.Domain.WorkManagement.Views.SortRule>), typeof(DateTimeOffset))]
    [Fact]
    public void DuplicateSort_ShouldThrow()
    {
        var pref = BoardViewUserPreference.Create(Guid.NewGuid(), WorkspaceId, BoardId, Guid.NewGuid(), UserId, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)pref).ClearDomainEvents();

        var fieldId = Guid.NewGuid();
        var sorts = new[]
        {
            SortRule.Create(fieldId, SortDirection.Ascending),
            SortRule.Create(fieldId, SortDirection.Descending)
        };

        var act = () => pref.ApplySort(sorts, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Duplicate sort rules for the same field are not allowed.");
    }
}
