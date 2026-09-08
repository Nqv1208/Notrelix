using Notrelix.Application.Features.Collaboration.Public.ResourceSummary;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Infrastructure.CrossContext.Collaboration.ResourceSummary;
using Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration;

namespace Notrelix.Architecture.Tests.Contracts;

/// <summary>
/// TAC-WM-002/003 — the Collaboration read edge follows TAC-XC-B: the port is
/// consumer-owned (WorkManagement), the Infrastructure adapter is classified
/// consumer=WorkManagement / producer=Collaboration, and the adapter reads the
/// Collaboration-owned published ResourceSummary boundary — never the foreign
/// Collaboration DbContext.
/// </summary>
public class CollaborationReadBoundaryArchitectureTests
{
    [Fact]
    public void ConsumerPort_IsOwnedBy_WorkManagement()
    {
        typeof(IWorkManagementCollaborationReadPort).Namespace.Should().Be(
            "Notrelix.Application.Features.WorkManagement.Ports.Collaboration",
            "the read port belongs to the consuming context");
    }

    [Fact]
    public void Adapter_IsRuntimeOwned_And_ReadsOnlyTheProducerPublishedBoundary()
    {
        var adapterType = typeof(WorkManagementCollaborationReadAdapter);

        adapterType.Namespace.Should().StartWith("Notrelix.Infrastructure.CrossContext.WorkManagement",
            "the adapter is runtime composition on the WorkManagement consumer side");
        adapterType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .Should().Contain(typeof(ICollaborationResourceSummary),
                "the adapter reads the Collaboration-owned published ResourceSummary boundary");

        var referencedTypes = adapterType
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType.FullName)
            .ToList();
        referencedTypes.Should().NotContain(
            "Notrelix.Application.Features.Collaboration.Abstractions.ICollaborationDbContext",
            "the foreign Collaboration DbContext must not leak through the consumer adapter");
    }

    [Fact]
    public void ProducerBoundary_IsOwnedBy_Collaboration_AndImplReadsOwnContext()
    {
        typeof(ICollaborationResourceSummary).Namespace.Should().Be(
            "Notrelix.Application.Features.Collaboration.Public.ResourceSummary",
            "the semantic boundary is producer-owned by Collaboration");

        typeof(PostgresCollaborationResourceSummary).Namespace.Should().StartWith(
            "Notrelix.Infrastructure.CrossContext.Collaboration",
            "producer/purpose = Collaboration: the source implementation reads Collaboration-owned storage");
    }

    [Fact]
    public void Adapter_MapsOntoConsumerShape_WithoutExtraAclClass()
    {
        // The (kind, id) → counts mapping is mechanical and stays inside the
        // adapter; no dedicated ACL class exists for it.
        var adapterAssembly = typeof(WorkManagementCollaborationReadAdapter).Assembly;
        adapterAssembly.GetTypes()
            .Where(t => t.Name.Contains("WorkItemCollaboration", StringComparison.Ordinal)
                        && t.Name != nameof(WorkItemCollaborationCounts))
            .Should().BeEmpty("no ACL-only class: the mapping lives in the adapter itself");
    }
}