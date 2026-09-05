using System.Reflection;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// ARCH-BC-001 — Foreign Persistence Dependency (boundary execution Wave 1).
///
/// Companion to DbContextBoundaryArchitectureTests (which text-scans *Handler.cs
/// files). This gate covers every Application feature type (handlers, services,
/// event handlers, projection writers, validators, DTOs) via compiled type
/// signatures: an Application feature type of one business context must not
/// reference another context's DbContext abstraction. Infrastructure is allowed
/// to implement multiple context interfaces and is not scanned here.
///
/// ARCH-BC-001 baseline must stay empty: KnownForeignPersistenceReferences is
/// reserved for reviewed, precise (consumer type, foreign type) signatures only.
/// </summary>
public class CrossContextPersistenceBoundaryTests
{
    private static readonly HashSet<(string ConsumerType, string ForeignType)> KnownForeignPersistenceReferences = [];

    [Fact]
    public void ApplicationFeatureTypes_ShouldNotReference_ForeignContextDbContext()
    {
        var violations = CrossContextBoundaryScanner
            .ScanApplicationFeatureTypes(GetApplicationAssembly())
            .Where(v => v.RuleId == CrossContextBoundaryScanner.RuleForeignPersistence)
            .Where(v => !KnownForeignPersistenceReferences.Contains((v.ConsumerType, v.ForeignType)))
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-001: Application feature code must use its own bounded-context " +
            "DbContext abstraction only. Cross-context reads/writes go through producer " +
            "Public contracts or consumer ports. Violations:\n" + Format(violations));
    }

    // ------------------------------------------------------------------
    // Gate self-tests — prove the gate detects violations and allows valid
    // patterns (synthetic fixtures live near the gate tests).
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Detects_ForeignDbContextInConstructor()
    {
        var violations = CrossContextBoundaryScanner.ScanTypeSignatures(
            typeof(ForeignDbContextConsumerFixture),
            "Automation");

        violations
            .Where(v => v.RuleId == CrossContextBoundaryScanner.RuleForeignPersistence)
            .Select(v => v.ForeignType)
            .Distinct()
            .Should()
            .BeEquivalentTo([typeof(IWorkspaceDbContext).FullName]);
    }

    [Fact]
    public void Gate_Allows_OwnContextDbContext()
    {
        var violations = CrossContextBoundaryScanner.ScanTypeSignatures(
            typeof(OwnDbContextConsumerFixture),
            "WorkManagement");

        violations.Should().NotContain(v => v.RuleId == CrossContextBoundaryScanner.RuleForeignPersistence);
    }

    [Fact]
    public void Gate_Detects_ForeignDbContextInsideGenericSignature()
    {
        var violations = CrossContextBoundaryScanner.ScanTypeSignatures(
            typeof(GenericForeignDbContextConsumerFixture),
            "Collaboration");

        violations.Should().Contain(v =>
            v.RuleId == CrossContextBoundaryScanner.RuleForeignPersistence &&
            v.ForeignType == typeof(IWorkManagementDbContext).FullName);
    }

    private static Assembly GetApplicationAssembly() => Assembly.Load("Notrelix.Application");

    private static string Format(IEnumerable<CrossContextBoundaryScanner.SignatureReferenceViolation> violations)
    {
        return string.Join("\n", violations.Select(v =>
            $"  [{v.RuleId}] {v.ConsumerType} ({v.ConsumerContext}) -> {v.ProducerContext} {v.ForeignType} via {v.Surface}"));
    }

    private sealed class ForeignDbContextConsumerFixture(IWorkspaceDbContext foreignDbContext)
    {
        public IWorkspaceDbContext ForeignDbContext { get; } = foreignDbContext;
    }

    private sealed class OwnDbContextConsumerFixture(IWorkManagementDbContext ownDbContext)
    {
        public IWorkManagementDbContext OwnDbContext { get; } = ownDbContext;
    }

    private sealed class GenericForeignDbContextConsumerFixture
    {
        public Task<IReadOnlyList<IWorkManagementDbContext>>? LoadedContexts { get; init; }
    }
}
