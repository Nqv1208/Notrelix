using System.Reflection;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Boards;
using Xunit;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// ARCH-BC-002 — Foreign Domain Model Dependency
/// ARCH-BC-003 — Producer Internal Dependency (Wave 1 boundary execution).
///
/// One scanner drives both rules; failure output is separated by Rule ID:
///
///   ARCH-BC-002: an Application feature type of one business context must not
///   reference producer Domain model types (aggregates, entities, value objects,
///   enums) from another context. Allowed: producer Public facts/references,
///   stable IDs, Domain.Common / Domain.SharedKernel primitives.
///
///   ARCH-BC-003: an Application feature type of one business context must not
///   reference producer internal Application implementation — producer
///   Commands/Queries request types or producer Abstractions service ports.
///   Producer `Features.{P}.Public.*` is the approved cross-context surface.
///   MediatR itself is not banned; internal requests just may not cross
///   context boundaries.
///
/// Detection combines a Roslyn source scan (explicit usings / qualified chains /
/// aliases, immune to comments and strings — global usings are project-level and
/// not attributed to files) with a compiled signature scan (reflection over
/// constructor parameters, fields, properties, method signatures, generic
/// constraints with generic/array unwrapping).
///
/// Baselines are precise (consumer type/chain, producer type) — never wildcard.
/// </summary>
public class CrossContextApplicationDependencyTests : ArchitectureTestBase
{
    // ARCH-BC-002 signature baseline — precise (rule, consumer type, foreign type) only.
    // Currently empty: no Application feature type carries a foreign Domain type on
    // its compiled signature surface.
    private static readonly HashSet<(string RuleId, string ConsumerType, string ForeignType)>
        KnownForeignDomainReferences = [];

    // ARCH-BC-002 source baseline — precise (rule, consumer source, foreign chain) only.
    //
    // AcceptInvitation (Workspaces) imports Domain.Accounts.Accounts for the
    // AccountStatus enum consumed through the Accounts-owned status reader port.
    // Classification: MIGRATE-ON-TOUCH (R2). Trigger: next material edit to the
    // AcceptInvitation use case. Target: Accounts-owned Public status fact or a
    // Workspaces-owned consumer port (BOUND-PORT-002), removing the producer
    // Domain enum import.
    private static readonly HashSet<(string RuleId, string RelativePath, string Chain)>
        KnownForeignDomainSourceReferences =
        [
            (
                CrossContextBoundaryScanner.RuleForeignDomainModel,
                "Features/Workspaces/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs",
                "Notrelix.Domain.Accounts.Accounts"
            ),
        ];

    // ARCH-BC-003 signature baseline — precise (rule, consumer type, foreign type) only.
    //
    // AcceptInvitationCommandHandler (Workspaces) injects Accounts/Identity
    // Abstractions service ports:
    //   IAccountMembershipProvisioner, IAccountStatusReader (Accounts)
    //   IIdentityUserLookupService (Identity)
    // Classification: MIGRATE-ON-TOUCH (R2). Trigger: next material edit to the
    // AcceptInvitation use case. Target: producer Public semantic contracts or
    // Workspaces-owned consumer ports speaking workspace language.
    private static readonly HashSet<(string RuleId, string ConsumerType, string ForeignType)>
        KnownProducerInternalReferences =
        [
            (
                CrossContextBoundaryScanner.RuleProducerInternal,
                "Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation.AcceptInvitationCommandHandler",
                "Notrelix.Application.Features.Accounts.Abstractions.IAccountMembershipProvisioner"
            ),
            (
                CrossContextBoundaryScanner.RuleProducerInternal,
                "Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation.AcceptInvitationCommandHandler",
                "Notrelix.Application.Features.Accounts.Abstractions.IAccountStatusReader"
            ),
            (
                CrossContextBoundaryScanner.RuleProducerInternal,
                "Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation.AcceptInvitationCommandHandler",
                "Notrelix.Application.Features.Identity.Abstractions.IIdentityUserLookupService"
            ),
        ];

    // ARCH-BC-003 source baseline — precise (rule, consumer source, foreign chain) only.
    private static readonly HashSet<(string RuleId, string RelativePath, string Chain)>
        KnownProducerInternalSourceReferences = [];

    private const string ApplicationAssemblyName = "Notrelix.Application";

    [Fact]
    public void ApplicationFeatureTypes_ShouldNotReference_ForeignDomainModelTypes()
    {
        var violations = CrossContextBoundaryScanner
            .ScanApplicationFeatureTypes(LoadApplicationAssembly())
            .Where(v => v.RuleId == CrossContextBoundaryScanner.RuleForeignDomainModel)
            .Where(v => !KnownForeignDomainReferences.Contains((v.RuleId, v.ConsumerType, v.ForeignType)))
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-002: consumer Application code must not depend on producer Domain " +
            "model types. Use producer Public facts/references or stable IDs. Violations:\n" +
            Format(violations));
    }

    [Fact]
    public void ApplicationFeatureTypes_ShouldNotReference_ProducerInternalRequestsOrAbstractions()
    {
        var violations = CrossContextBoundaryScanner
            .ScanApplicationFeatureTypes(LoadApplicationAssembly())
            .Where(v => v.RuleId == CrossContextBoundaryScanner.RuleProducerInternal)
            .Where(v => !KnownProducerInternalReferences.Contains((v.RuleId, v.ConsumerType, v.ForeignType)))
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-003: consumer Application code must not reference producer internal " +
            "requests (Commands/Queries) or producer Abstractions service ports. Use " +
            "producer Public contracts or consumer-owned ports. Violations:\n" +
            Format(violations));
    }

    [Fact]
    public void ApplicationFeatureSource_ShouldNotImport_ForeignDomainNamespaces()
    {
        var violations = ScanAllFeatureSource()
            .Where(v => v.RuleId == CrossContextBoundaryScanner.RuleForeignDomainModel)
            .Where(v => !KnownForeignDomainSourceReferences.Contains((v.RuleId, v.RelativePath, v.Chain)))
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-002: consumer Application feature source must not import producer " +
            "Domain namespaces. Violations:\n" + FormatSource(violations));
    }

    [Fact]
    public void ApplicationFeatureSource_ShouldNotImport_ProducerInternalNamespaces()
    {
        var violations = ScanAllFeatureSource()
            .Where(v => v.RuleId == CrossContextBoundaryScanner.RuleProducerInternal)
            .Where(v => !KnownProducerInternalSourceReferences.Contains((v.RuleId, v.RelativePath, v.Chain)))
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-003: consumer Application feature source must not import producer " +
            "internal Commands/Queries namespaces. Violations:\n" + FormatSource(violations));
    }

    // ------------------------------------------------------------------
    // Gate self-tests — synthetic fixtures proving detection/allowance.
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Detects_ForeignDomainAggregateInSignature()
    {
        var violations = CrossContextBoundaryScanner.ScanTypeSignatures(
            typeof(ForeignDomainConsumerFixture),
            "Automation");

        violations.Should().Contain(v =>
            v.RuleId == CrossContextBoundaryScanner.RuleForeignDomainModel &&
            v.ForeignType == typeof(Board).FullName);
    }

    [Fact]
    public void Gate_Detects_ForeignDomainReferenceInsideGenericAndArrayTypes()
    {
        var violations = CrossContextBoundaryScanner.ScanTypeSignatures(
            typeof(GenericForeignDomainConsumerFixture),
            "Analytics");

        violations.Should().Contain(v =>
            v.RuleId == CrossContextBoundaryScanner.RuleForeignDomainModel &&
            v.ForeignType == typeof(Notrelix.Domain.Documents.Pages.Page).FullName);
    }

    [Fact]
    public void Gate_Allows_DomainCommonAndSharedKernel()
    {
        var violations = CrossContextBoundaryScanner.ScanTypeSignatures(
            typeof(SharedKernelConsumerFixture),
            "WorkManagement");

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Gate_Allows_ProducerPublicSurface()
    {
        var violations = CrossContextBoundaryScanner.ClassifyReferencedType(
            typeof(WorkPublicContractFixture.WorkFact),
            "Automation");

        violations.Should().BeNull(
            "producer Public namespaces are the approved cross-context surface");
    }

    [Fact]
    public void Gate_Allows_OwnContextDomainTypes()
    {
        var violations = CrossContextBoundaryScanner.ScanTypeSignatures(
            typeof(OwnDomainConsumerFixture),
            "WorkManagement");

        violations.Should().NotContain(v =>
            v.RuleId == CrossContextBoundaryScanner.RuleForeignDomainModel);
    }

    [Fact]
    public void Gate_SourceScan_Detects_ForeignDomainUsingDirective()
    {
        const string source =
            """
            using Notrelix.Domain.WorkManagement.Boards;

            namespace Notrelix.Application.Features.Automation.Rules;

            public class FakeRuleService
            {
                public Board? Loaded { get; set; }
            }
            """;

        var violations = CrossContextBoundaryScanner.ScanSource(source, "Automation", "Features/Automation/Fake.cs");

        violations.Should().Contain(v =>
            v.RuleId == CrossContextBoundaryScanner.RuleForeignDomainModel &&
            v.Kind == "using");
    }

    [Fact]
    public void Gate_SourceScan_Ignores_CommentsAndStrings()
    {
        const string source =
            """
            namespace Notrelix.Application.Features.Automation.Rules;

            // using Notrelix.Domain.WorkManagement.Boards; — comment only
            public class FakeRuleService
            {
                public string Documentation { get; } = "using Notrelix.Domain.WorkManagement.Boards;";
            }
            """;

        var violations = CrossContextBoundaryScanner.ScanSource(source, "Automation", "Features/Automation/Fake.cs");

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Gate_SourceScan_Detects_AliasedForeignReference()
    {
        const string source =
            """
            using Boards = Notrelix.Domain.WorkManagement.Boards;

            namespace Notrelix.Application.Features.Automation.Rules;

            public class FakeRuleService
            {
                public Boards.Board? Loaded { get; set; }
            }
            """;

        var violations = CrossContextBoundaryScanner.ScanSource(source, "Automation", "Features/Automation/Fake.cs");

        violations.Should().Contain(v =>
            v.RuleId == CrossContextBoundaryScanner.RuleForeignDomainModel &&
            v.Chain.Contains("WorkManagement", StringComparison.Ordinal));
    }

    [Fact]
    public void Gate_SourceScan_Allows_ProducerPublicImport()
    {
        const string source =
            """
            using Notrelix.Application.Features.Workspaces.Public.Facts;

            namespace Notrelix.Application.Features.WorkManagement.Boards;

            public class FakeBoardService
            {
                public void Consume(WorkspaceScopeFact fact) { }
            }
            """;

        var violations = CrossContextBoundaryScanner.ScanSource(
            source,
            "WorkManagement",
            "Features/WorkManagement/Fake.cs");

        violations.Should().BeEmpty();
    }

    private static Assembly LoadApplicationAssembly() => Assembly.Load(ApplicationAssemblyName);

    private static IReadOnlyList<CrossContextBoundaryScanner.SourceReferenceViolation> ScanAllFeatureSource()
    {
        var appPath = GetApplicationPath();
        var files = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<CrossContextBoundaryScanner.SourceReferenceViolation>();
        foreach (var file in files)
        {
            var relativePath = file[(appPath.Length + 1)..];
            var context = CrossContextBoundaryScanner.ResolveContextFromRelativePath(relativePath);
            if (context is null)
                continue;

            violations.AddRange(
                CrossContextBoundaryScanner.ScanSource(File.ReadAllText(file), context, relativePath));
        }

        return violations;
    }

    private static string Format(IEnumerable<CrossContextBoundaryScanner.SignatureReferenceViolation> violations)
    {
        return string.Join("\n", violations.Select(v =>
            $"  [{v.RuleId}] {v.ConsumerType} ({v.ConsumerContext}) -> {v.ProducerContext} {v.ForeignType} via {v.Surface}"));
    }

    private static string FormatSource(IEnumerable<CrossContextBoundaryScanner.SourceReferenceViolation> violations)
    {
        return string.Join("\n", violations.Select(v =>
            $"  [{v.RuleId}] {v.RelativePath}:{v.Line} ({v.ConsumerContext} -> {v.ProducerContext}) {v.Kind}: {v.Chain}"));
    }

    // Synthetic producer Public surface used by the source-scan allowance test.
    private static class WorkPublicContractFixture
    {
        public sealed record WorkFact(Guid BoardId);
    }

    private sealed class ForeignDomainConsumerFixture(Board board)
    {
        public Board Board { get; } = board;
    }

    private sealed class GenericForeignDomainConsumerFixture
    {
        public Task<IReadOnlyList<Notrelix.Domain.Documents.Pages.Page>>[]? Pages { get; init; }
    }

    private sealed class SharedKernelConsumerFixture(Guid boardId, ResourceRef resourceRef)
    {
        public Guid BoardId { get; } = boardId;
        public ResourceRef Resource { get; } = resourceRef;
    }

    private sealed class OwnDomainConsumerFixture(Board board)
    {
        public Board Board { get; } = board;
    }
}
