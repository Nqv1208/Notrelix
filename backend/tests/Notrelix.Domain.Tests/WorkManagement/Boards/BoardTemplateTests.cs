using FluentAssertions;
using Notrelix.Domain.WorkManagement.Templates;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardTemplateTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSucceedInItsWorkspace()
    {
        var template = BoardTemplate.Create(AccountId, WorkspaceId, "Sprint", "Two-week sprint", JsonValue.EmptyObject(), DateTimeOffset.UtcNow);

        template.AccountId.Should().Be(AccountId);
        template.WorkspaceId.Should().Be(WorkspaceId);
        template.Name.Should().Be("Sprint");
        template.Description.Should().Be("Two-week sprint");
        template.Status.Should().Be(TemplateStatus.Published);
        template.DomainEvents.Should().ContainSingle(e => e is BoardTemplateCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithoutWorkspace_ShouldThrow()
    {
        var act = () => BoardTemplate.Create(AccountId, Guid.Empty, "Sprint", null, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => BoardTemplate.Create(AccountId, WorkspaceId, null!, null, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullStructure_ShouldThrow()
    {
        var act = () => BoardTemplate.Create(AccountId, WorkspaceId, "Sprint", null, null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldTrimNameAndDropBlankDescription()
    {
        var template = BoardTemplate.Create(AccountId, WorkspaceId, "  Sprint  ", "  ", JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Name.Should().Be("Sprint");
        template.Description.Should().BeNull();
    }
}
