using System.Reflection;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;

namespace Notrelix.Architecture.Tests.InfrastructureLayer;

/// <summary>
/// TAC-AR-001B / TAC-AR-001C / TAC-AR-002 (TAC-XC-B chain, frozen) —
/// producer-source ownership and delegate-only anti-regression gates:
/// the Analytics-Infrastructure adapter may only delegate to the producer
/// Public contract; only the WorkManagement producer implementation may read
/// Work persistence; and consumers that already hold every fact in the event
/// must not depend on the projection-source port at all.
/// </summary>
public class AnalyticsProjectionSourceOwnershipArchitectureTests
{
    private static readonly Assembly InfrastructureAssembly =
        typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Features.Analytics.Placements.Services.WorkspaceWorkItemPlacementService).Assembly;

    private static Type[] ConcreteTypes(Assembly assembly) =>
        assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }).ToArray();

    private static Type[] ConstructorParameterTypes(Type type) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToArray();

    private static void AssertNoParameterTypes(
        Type type, Func<Type, bool> forbidden, string rule)
    {
        var violations = ConstructorParameterTypes(type).Where(forbidden).ToList();
        violations.Should().BeEmpty(
            $"{rule}: {type.FullName} must not depend on {string.Join(", ", violations.Select(v => v.Name))}.");
    }

    // ── TAC-AR-001B: producer Public source implementation ownership ────

    [Fact]
    public void CrossContextAnalyticsAdapter_NeverImplementsProducerSource()
    {
        var adapterTypes = ConcreteTypes(InfrastructureAssembly)
            .Where(t => t.Namespace == "Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement")
            .ToList();

        adapterTypes.Should().NotBeEmpty("the frozen TAC-XC-B adapter must exist");

        var offenders = adapterTypes
            .Where(t => typeof(IWorkItemProjectionSource).IsAssignableFrom(t))
            .ToList();
        offenders.Should().BeEmpty(
            "TAC-AR-001B: no type under Infrastructure/CrossContext/Analytics/WorkManagement " +
            "may implement the producer Public IWorkItemProjectionSource by reading Work " +
            "persistence directly — only WorkManagement Application owns that implementation.");
    }

    [Fact]
    public void CrossContextAnalyticsAdapter_DoesNotDependOnWorkPersistence()
    {
        foreach (var type in ConcreteTypes(InfrastructureAssembly)
                     .Where(t => t.Namespace == "Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement"))
        {
            AssertNoParameterTypes(
                type,
                forbidden: p => p == typeof(IWorkManagementDbContext),
                rule: "TAC-AR-001B");
        }
    }

    [Fact]
    public void CrossContextAnalyticsAdapter_DependsOnlyOnProducerContract()
    {
        var allowed = new[] { typeof(IWorkItemProjectionSource) };
        foreach (var type in ConcreteTypes(InfrastructureAssembly)
                     .Where(t => t.Namespace == "Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement"))
        {
            var offenders = ConstructorParameterTypes(type).Except(allowed).ToList();
            offenders.Should().BeEmpty(
                $"TAC-AR-001C: {type.Name} is a delegate-only adapter — its only collaborator " +
                "may be the producer Public IWorkItemProjectionSource.");
        }
    }

    [Fact]
    public void OnlyWorkManagementApplication_ImplementsProducerSource()
    {
        var implementers = ConcreteTypes(ApplicationAssembly)
            .Where(t => typeof(IWorkItemProjectionSource).IsAssignableFrom(t))
            .ToList();

        implementers.Select(t => t.FullName).Should().BeEquivalentTo(
            ["Notrelix.Application.Features.WorkManagement.BoardItems.Services.WorkItemProjectionSourceService"],
            "the producer implementation reading Work placement truth is owned exclusively by WorkManagement Application");

        var implementation = implementers.Single();
        ConstructorParameterTypes(implementation).Should().Contain(typeof(IWorkManagementDbContext),
            "the producer implementation must read the producer's own persistence");
    }

    // ── TAC-AR-002: event-sufficient consumers must not touch the source ─

    [Fact]
    public void EventSufficientConsumers_DoNotDependOnProjectionSourcePort()
    {
        var moved = typeof(Notrelix.Infrastructure.Messaging.Consumers.Analytics.BoardItemMovedPlacementConsumer);

        AssertNoParameterTypes(
            moved,
            forbidden: p => p == typeof(IWorkItemProjectionSourceAdapter)
                         || p == typeof(IWorkItemProjectionSource)
                         || p == typeof(IWorkManagementDbContext),
            rule: "TAC-AR-002 (event data sufficient → consumer → Analytics projection service)");

        var archived = typeof(Notrelix.Infrastructure.Messaging.Consumers.Analytics.BoardItemArchivedPlacementConsumer);
        AssertNoParameterTypes(
            archived,
            forbidden: p => p == typeof(IWorkItemProjectionSourceAdapter)
                         || p == typeof(IWorkItemProjectionSource)
                         || p == typeof(IWorkManagementDbContext),
            rule: "TAC-AR-002");
    }

    [Fact]
    public void PlacementConsumers_NeverDependOnWorkPersistence()
    {
        foreach (var type in ConcreteTypes(InfrastructureAssembly)
                     .Where(t => t.Namespace == "Notrelix.Infrastructure.Messaging.Consumers.Analytics"))
        {
            AssertNoParameterTypes(
                type,
                forbidden: p => p == typeof(IWorkManagementDbContext),
                rule: "TAC-AR-002 forbidden: consumer/adapter -> IWorkManagementDbContext");
        }
    }

    // ── AR-FLOW-02: local read handler owns exactly the Analytics port ───

    [Fact]
    public void PlacementQueryHandler_DependsOnlyOnReportingContext()
    {
        var handler = typeof(Notrelix.Application.Features.Analytics.Placements.Queries.GetWorkspacePlacements
            .GetWorkspacePlacementsQueryHandler);

        ConstructorParameterTypes(handler).Should().BeEquivalentTo(
            [typeof(Notrelix.Application.Features.Analytics.Abstractions.IReportingDbContext)],
            "AR-FLOW-02: the production placement query reads only the Analytics local projection");
    }

    [Fact]
    public void PlacementMaintenanceService_DependsOnlyOnAnalyticsPorts()
    {
        var service = typeof(Notrelix.Application.Features.Analytics.Placements.Services.WorkspaceWorkItemPlacementService);

        ConstructorParameterTypes(service).Should().BeEquivalentTo(
            [typeof(IReportingDbContext), typeof(IWorkItemProjectionSourceAdapter)],
            "TAC-XC-B: Analytics placement maintenance composes only the Analytics-local " +
            "reporting context and the Analytics-owned source/rebuild Port");
    }
}
