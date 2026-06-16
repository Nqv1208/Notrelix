using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Templates;
using Notrelix.Domain.WorkManagement.Templates.Events;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardTemplateTests
{
    [Fact]
    public void Create_WithWorkspace_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var structure = JsonValue.EmptyObject();
        var now = DateTimeOffset.UtcNow;

        var template = BoardTemplate.Create("Sprint", structure, now, workspaceId);

        template.WorkspaceId.Should().Be(workspaceId);
        template.Name.Should().Be("Sprint");
        template.Status.Should().Be(TemplateStatus.Published);
        template.DomainEvents.Should().ContainSingle(e => e is BoardTemplateCreatedEvent);
    }

    [Fact]
    public void Create_WithoutWorkspace_ShouldSucceed()
    {
        var structure = JsonValue.EmptyObject();
        var now = DateTimeOffset.UtcNow;

        var template = BoardTemplate.Create("Sprint", structure, now);

        template.WorkspaceId.Should().BeNull();
        template.DomainEvents.Should().ContainSingle(e => e is BoardTemplateCreatedEvent);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var structure = JsonValue.EmptyObject();
        var act = () => BoardTemplate.Create(null!, structure, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullStructure_ShouldThrow()
    {
        var act = () => BoardTemplate.Create("Sprint", null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var structure = JsonValue.EmptyObject();
        var template = BoardTemplate.Create("  Sprint  ", structure, DateTimeOffset.UtcNow);
        template.Name.Should().Be("Sprint");
    }
}
