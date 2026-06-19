using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Views;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class SavedFilterTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var name = "My Filter";
        var rules = new[] { FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "value") };
        var createdBy = Guid.NewGuid();

        var filter = SavedFilter.Create(workspaceId, boardId, name, rules, createdBy, DateTimeOffset.UtcNow);

        filter.WorkspaceId.Should().Be(workspaceId);
        filter.BoardId.Should().Be(boardId);
        filter.Name.Should().Be(name);
        filter.Rules.Should().HaveCount(1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterCreatedDomainEvent);
    }

    [Fact]
    public void Create_ShouldThrow_WhenNameEmpty()
    {
        Action act = () => SavedFilter.Create(Guid.NewGuid(), Guid.NewGuid(), "", new[] { FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "v") }, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }
}
