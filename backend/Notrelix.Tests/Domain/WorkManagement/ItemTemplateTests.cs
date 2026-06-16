using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Templates;
using Notrelix.Domain.WorkManagement.Templates.Events;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class ItemTemplateTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var values = JsonValue.EmptyObject();
        var now = DateTimeOffset.UtcNow;

        var template = ItemTemplate.Create(workspaceId, boardId, "Task", values, now);

        template.WorkspaceId.Should().Be(workspaceId);
        template.BoardId.Should().Be(boardId);
        template.Name.Should().Be("Task");
        template.Status.Should().Be(TemplateStatus.Published);
        template.DomainEvents.Should().ContainSingle(e => e is ItemTemplateCreatedEvent);
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var boardId = Guid.NewGuid();
        var values = JsonValue.EmptyObject();
        var act = () => ItemTemplate.Create(Guid.Empty, boardId, "Task", values, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyBoardId_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var values = JsonValue.EmptyObject();
        var act = () => ItemTemplate.Create(workspaceId, Guid.Empty, "Task", values, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var values = JsonValue.EmptyObject();
        var act = () => ItemTemplate.Create(Guid.NewGuid(), Guid.NewGuid(), null!, values, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullValues_ShouldThrow()
    {
        var act = () => ItemTemplate.Create(Guid.NewGuid(), Guid.NewGuid(), "Task", null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var values = JsonValue.EmptyObject();
        var template = ItemTemplate.Create(Guid.NewGuid(), Guid.NewGuid(), "  Task  ", values, DateTimeOffset.UtcNow);
        template.Name.Should().Be("Task");
    }
}
