using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.Pages;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class PageTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        
        var page = Page.Create(workspaceId, "My Page", createdBy, DateTimeOffset.UtcNow);

        page.Title.Should().Be("My Page");
        page.WorkspaceId.Should().Be(workspaceId);
        page.CreatedBy.Should().Be(createdBy);
        page.DomainEvents.Should().ContainSingle(e => e is PageCreatedEvent);
    }

    [Fact]
    public void Move_ShouldThrow_WhenCreatingCycle()
    {
        var pageId = Guid.NewGuid();
        var page = Page.Create(Guid.NewGuid(), "Child", Guid.NewGuid(), DateTimeOffset.UtcNow);
        // Mock hierarchy: parent -> page
        var parentId = Guid.NewGuid();
        
        // Simulating a hierarchy where the target parent is actually a child (though we only have ID here)
        // In a real test, we would have a way to resolve parents.
        
        Func<Guid, Guid?> getParentId = (id) => 
        {
            if (id == parentId) return page.Id; // parent's parent is the page itself -> CYCLE
            return null;
        };

        // Use reflection to set Id if needed, but here we can just use the provided ID in rules
        // Actually, let's use the static rule directly to test the logic.
        
        Action act = () => Notrelix.Domain.Documents.Rules.PageTreeRules.EnsureNoCycle(page.Id, parentId, getParentId);

        act.Should().Throw<BusinessRuleException>().WithMessage("Page move would create a cycle in the page tree.");
    }
}
