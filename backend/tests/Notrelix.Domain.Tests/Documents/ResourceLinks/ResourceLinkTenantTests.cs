using FluentAssertions;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Documents.ResourceLinks;

public class ResourceLinkTenantTests
{
    [Fact]
    public void Create_SameWorkspace_ShouldSucceed()
    {
        var ws = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), ws);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), ws);
        var link = ResourceLink.Create(Guid.NewGuid(), ws, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);
        link.WorkspaceId.Should().Be(ws);
    }

    [Fact]
    public void Create_DifferentWorkspace_ShouldThrow()
    {
        var ws = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), ws);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());
        var act = () => ResourceLink.Create(Guid.NewGuid(), ws, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_SelfReference_ShouldThrow()
    {
        var ws = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), ws);
        var act = () => ResourceLink.Create(Guid.NewGuid(), ws, source, source, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(ResourceLink), nameof(ResourceLink.Delete), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldRaiseEvent()
    {
        var ws = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), ws);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), ws);
        var link = ResourceLink.Create(Guid.NewGuid(), ws, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)link).ClearDomainEvents();
        link.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        link.DomainEvents.Should().ContainSingle(e => e is ResourceLinkDeletedDomainEvent);
    }

    [CoversMutation(typeof(ResourceLink), nameof(ResourceLink.Restore), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldRaiseEvent()
    {
        var ws = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), ws);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), ws);
        var link = ResourceLink.Create(Guid.NewGuid(), ws, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);
        link.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)link).ClearDomainEvents();
        link.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        link.DomainEvents.Should().ContainSingle(e => e is ResourceLinkRestoredDomainEvent);
    }

    [CoversMutation(typeof(ResourceLink), nameof(ResourceLink.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(ResourceLink), nameof(ResourceLink.Delete), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_IsIdempotent_ShouldNotIncrementVersion()
    {
        var ws = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), ws);
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), ws);
        var link = ResourceLink.Create(Guid.NewGuid(), ws, source, target, LinkType.Internal, Guid.NewGuid(), DateTimeOffset.UtcNow);
        link.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = link.Version;
        link.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        link.Version.Should().Be(before);
    }
}
