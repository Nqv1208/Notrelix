using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Notrelix.Infrastructure.Data;
using Xunit;

namespace Notrelix.Architecture.Tests.DataAccess;

/// <summary>
/// ARCH-BC-004 — Cross-context EF Navigation / Cascade (boundary Wave 2).
///
/// Builds the real EF model of ApplicationDbContext and inspects every foreign
/// key relationship whose declaring and principal entity types resolve to two
/// different bounded contexts:
///   - cross-context DeleteBehavior.Cascade / SetNull  → violation
///     (product lifecycle must be explicit, not a database cascade)
///   - cross-context ORM navigation (dependent-to-principal or
///     principal-to-dependent) → violation (BOUND-DATA-003)
///   - shadow foreign key (no navigation, restricted delete) → reviewed
///     physical integrity debt, permitted (BOUND-DATA-003)
///
/// Same-context relationships, Guid/ResourceRef scalar references and
/// Infrastructure-owned projection/messaging entities are not business-context
/// coupling and are not flagged.
/// </summary>
public class CrossContextCascadeArchitectureTests
{
    private static readonly IReadOnlySet<string> BusinessContexts =
        CrossContextBoundaryScanner.BusinessContexts;

    [Fact]
    public void EFModel_ShouldNotContain_CrossContextNavigationOrCascade()
    {
        var violations = AnalyzeModel(BuildApplicationModel(), ResolveContext);

        violations.Should().BeEmpty(
            "ARCH-BC-004: cross-context EF navigation/cascade is forbidden. Use stable " +
            "IDs / ResourceRef and explicit product-owned lifecycle behavior instead of " +
            "database cascade across bounded contexts.\n" + string.Join("\n", violations));
    }

    [Fact]
    public void ContextResolution_MapsDomainNamespaceToCanonicalContext()
    {
        ResolveContext(typeof(Notrelix.Domain.WorkManagement.Boards.Board))
            .Should().Be("WorkManagement");
        ResolveContext(typeof(ApplicationDbContext))
            .Should().BeNull("Infrastructure-owned entities are not business contexts");
    }

    // ------------------------------------------------------------------
    // Gate self-tests — synthetic EF models prove detection/allowance.
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Detects_CrossContextCascade()
    {
        var model = BuildSyntheticModel(DeleteBehavior.Cascade);

        var violations = AnalyzeModel(model, ResolveSyntheticContext);

        violations.Should().Contain(v =>
            v.Contains("Cascade", StringComparison.Ordinal) &&
            v.Contains("SyntheticConsumer", StringComparison.Ordinal) &&
            v.Contains("SyntheticProducer", StringComparison.Ordinal));
    }

    [Fact]
    public void Gate_Detects_CrossContextNavigation_EvenWithRestrictDelete()
    {
        var model = BuildSyntheticModel(DeleteBehavior.ClientSetNull);

        var violations = AnalyzeModel(model, ResolveSyntheticContext);

        violations.Should().Contain(v =>
            v.Contains("navigation", StringComparison.OrdinalIgnoreCase) &&
            v.Contains("SyntheticProducer", StringComparison.Ordinal));
    }

    [Fact]
    public void Gate_Allows_SameContextCascadeAndShadowForeignKey()
    {
        var model = BuildSyntheticModel(DeleteBehavior.Cascade, crossContext: false, useNavigation: false);

        var violations = AnalyzeModel(model, ResolveSyntheticContext);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Gate_Allows_CrossContextShadowForeignKeyWithRestrict()
    {
        var model = BuildSyntheticModel(DeleteBehavior.Restrict, crossContext: true, useNavigation: false);

        var violations = AnalyzeModel(model, ResolveSyntheticContext);

        violations.Should().BeEmpty(
            "shadow FK with restricted delete is reviewed physical integrity debt, not semantic coupling");
    }

    // ------------------------------------------------------------------
    // Model analysis
    // ------------------------------------------------------------------

    private static IReadOnlyList<string> AnalyzeModel(
        IModel model,
        Func<Type, string?> contextResolver)
    {
        var violations = new List<string>();

        foreach (var entityType in model.GetEntityTypes())
        {
            var dependentContext = contextResolver(entityType.ClrType);
            if (dependentContext is null)
                continue;

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principalContext = contextResolver(foreignKey.PrincipalEntityType.ClrType);
                if (principalContext is null || principalContext == dependentContext)
                    continue;

                var pair = $"{dependentContext} -> {principalContext} " +
                           $"({entityType.ClrType.Name} -> {foreignKey.PrincipalEntityType.ClrType.Name})";
                var deleteBehavior = foreignKey.DeleteBehavior;

                if (deleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.SetNull)
                    violations.Add($"ARCH-BC-004: cross-context cascade {pair} uses {deleteBehavior}.");

                var navigation = foreignKey.DependentToPrincipal?.Name
                                 ?? foreignKey.PrincipalToDependent?.Name;
                if (navigation is not null)
                    violations.Add(
                        $"ARCH-BC-004: cross-context navigation {pair} via '{navigation}'. " +
                        "Use stable IDs/ResourceRef and explicit lifecycle behavior.");
            }
        }

        return violations
            .Distinct()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();
    }

    private static string? ResolveContext(Type type)
    {
        var ns = type.Namespace;
        if (ns is null || !ns.StartsWith("Notrelix.Domain.", StringComparison.Ordinal))
            return null;

        var remainder = ns["Notrelix.Domain.".Length..];
        var dot = remainder.IndexOf('.');
        var candidate = dot > 0 ? remainder[..dot] : remainder;

        return BusinessContexts.Contains(candidate) ? candidate : null;
    }

    private static string? ResolveSyntheticContext(Type type)
    {
        var fullName = type.FullName ?? type.Name;

        if (fullName.Contains("Bc004SyntheticConsumer", StringComparison.Ordinal))
            return "SyntheticConsumer";
        if (fullName.Contains("Bc004SyntheticProducer", StringComparison.Ordinal))
            return "SyntheticProducer";
        return null;
    }

    /// <summary>
    /// Builds the production model. Model building is a design-time operation
    /// and does not open a database connection; the Npgsql provider is required
    /// because the model uses PostgreSQL-specific mapping.
    /// </summary>
    private static IModel BuildApplicationModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=arch-bc-004;Username=arch;Password=arch")
            .Options;

        using var context = new ApplicationDbContext(options);
        return context.Model;
    }

    private static IModel BuildSyntheticModel(
        DeleteBehavior deleteBehavior,
        bool crossContext = true,
        bool useNavigation = true)
    {
        var modelBuilder = new ModelBuilder();

        var dependentType = crossContext
            ? typeof(Bc004SyntheticConsumer.Bc004SyntheticEntity)
            : typeof(Bc004SyntheticProducer.Bc004ProducerEntity);
        var principalType = typeof(Bc004SyntheticProducer.Bc004ProducerEntity);

        modelBuilder.Entity(dependentType).HasOne(principalType, useNavigation ? "Principal" : null)
            .WithMany()
            .HasForeignKey("PrincipalId")
            .IsRequired()
            .OnDelete(deleteBehavior);

        return (IModel)modelBuilder.Model;
    }

    private static class Bc004SyntheticConsumer
    {
        public class Bc004SyntheticEntity
        {
            public Guid Id { get; set; }

            public Guid PrincipalId { get; set; }

            public Bc004SyntheticProducer.Bc004ProducerEntity? Principal { get; set; }
        }
    }

    private static class Bc004SyntheticProducer
    {
        public class Bc004ProducerEntity
        {
            public Guid Id { get; set; }
        }
    }
}
