using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.ResourceLinks.Events;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class ResourceLinkTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), workspaceId);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), workspaceId);

        var link = ResourceLink.Create(workspaceId, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);

        link.WorkspaceId.Should().Be(workspaceId);
        link.Source.Should().Be(source);
        link.Target.Should().Be(target);
        link.Type.Should().Be(LinkType.Internal);
        link.DomainEvents.Should().ContainSingle(e => e is ResourceLinkCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithSelfReference_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), workspaceId);

        var act = () => ResourceLink.Create(workspaceId, source, source, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*self-referencing*");
    }

    [Fact]
    public void Create_WhenTargetWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), workspaceId);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());

        var act = () => ResourceLink.Create(workspaceId, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*same workspace*");
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), Guid.NewGuid());
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());

        var act = () => ResourceLink.Create(Guid.Empty, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SoftDelete_ShouldRaiseEvent()
    {
        var link = CreateLink();
        link.ClearDomainEvents();

        link.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        link.IsDeleted.Should().BeTrue();
        link.DomainEvents.Should().ContainSingle(e => e is ResourceLinkDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var link = CreateLink();
        link.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        link.ClearDomainEvents();

        link.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        link.DomainEvents.Should().BeEmpty();
    }

    private static ResourceLink CreateLink()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), workspaceId);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), workspaceId);
        return ResourceLink.Create(workspaceId, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
