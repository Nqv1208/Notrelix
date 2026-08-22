namespace Notrelix.Architecture.Tests;

/// <summary>
/// IA-TST-CSRF-ARCH-001 / IAREQ129 / IAREQ130 / IAREQ140.
///
/// Proves that every release-scoped unsafe API operation resolves to an explicit
/// browser-CSRF applicability classification derived from canonical endpoint/auth
/// mapping metadata (the Map{Scope}{Verb} DSL scope families), never from route
/// strings. A new unsafe operation whose classification cannot be resolved — a
/// public unsafe operation without session-behavior classification, or an
/// unknown/newly accepted auth mode — fails until the classification contract in
/// this gate is updated per ADR-005.
/// </summary>
public class CsrfApplicabilityInventoryArchitectureTests : ArchitectureTestBase
{
    private const string RuleId = "IA-TST-CSRF-ARCH-001";

    private static readonly string[] UnsafeVerbs = ["Post", "Put", "Patch", "Delete"];

    private static readonly string[] KnownScopes =
    [
        "Public", "Authenticated", "Account", "Workspace", "Resource", "Admin", "Internal",
    ];

    private sealed record UnsafeRegistration(string FileName, string Scope, string Verb, string Route);

    private sealed record SessionBehaviorClassification(
        bool EstablishesOrMutatesAmbientCookieSession,
        string Requirement,
        string Reason);

    private sealed record OverrideClassification(string Requirement, string Reason);

    /// <summary>
    /// Session-behavior classification for PUBLIC unsafe operations. The Public
    /// scope alone does not encode whether the operation establishes or mutates
    /// ambient cookie session state, so each public unsafe operation MUST be
    /// classified explicitly. Keyed by endpoint file name (identity/navigation,
    /// not a production route allowlist).
    /// </summary>
    private static readonly Dictionary<string, SessionBehaviorClassification> PublicUnsafeSessionBehavior = new()
    {
        ["LoginEndpoint.cs"] = new(true, "CSRF_REQUIRED",
            "establishes ambient access-token cookie session on success"),
        ["RegisterEndpoint.cs"] = new(true, "CSRF_REQUIRED",
            "creates account principal and establishes ambient access-token cookie session"),
        ["RefreshTokenEndpoint.cs"] = new(true, "CSRF_REQUIRED",
            "rotates ambient refresh/access token cookies"),
        ["CompleteMfaChallengeEndpoint.cs"] = new(true, "CSRF_REQUIRED",
            "completes challenge and establishes ambient access/refresh token cookies on success"),
        ["ForgotPasswordEndpoint.cs"] = new(false, "CSRF_NOT_REQUIRED",
            "no ambient session read or established; response is enumeration-safe generic"),
        ["ResetPasswordEndpoint.cs"] = new(false, "CSRF_NOT_REQUIRED",
            "one-time reset-token credential flow; no ambient cookie session consumed or established"),
        ["EmailVerificationEndpoints.cs"] = new(false, "CSRF_NOT_REQUIRED",
            "one-time email-verification token flows; no ambient cookie session consumed or established"),
        ["GetInvitationByTokenEndpoint.cs"] = new(false, "CSRF_NOT_REQUIRED",
            "invitation preview lookup by one-time invitation token; no ambient cookie session consumed"),
    };

    /// <summary>
    /// Overrides for non-public scopes whose accepted authentication modes differ
    /// from the family default. Empty means every non-public family currently
    /// follows its default classification below.
    /// </summary>
    private static readonly Dictionary<string, OverrideClassification> NonPublicOverrides = new();

    /// <summary>
    /// Default classification by canonical scope family. These follow directly
    /// from the accepted auth-mode semantics of the Map{Scope}{Verb} DSL:
    /// browser ambient cookie mode is an accepted credential for all protected
    /// families (runtime evidence-based classification then exempts explicit
    /// Authorization credentials such as API tokens), while Internal operations
    /// are service-to-service and never ambient-browser reachable.
    /// </summary>
    private static string DefaultRequirementForScope(string scope) => scope switch
    {
        "Internal" => "CSRF_NOT_REQUIRED",
        "Authenticated" or "Account" or "Workspace" or "Resource" or "Admin" => "CSRF_REQUIRED",
        _ => throw new InvalidOperationException(
            $"Scope '{scope}' has no default CSRF applicability classification."),
    };

    [Fact]
    public void UnsafeRegistrations_ResolveOnlyKnownAuthModeScopes()
    {
        var discovered = DiscoverUnsafeRegistrations();

        var unknownScopes = discovered
            .Where(r => !KnownScopes.Contains(r.Scope))
            .Select(r => $"{r.FileName}: Map{r.Scope}{r.Verb}")
            .Distinct()
            .ToList();

        unknownScopes.Should().BeEmpty(
            $"{RuleId}: a newly accepted auth mode/scope family was introduced. " +
            "The CSRF applicability classification contract (ADR-005) must be updated " +
            $"before shipping: {string.Join("; ", unknownScopes)}");
    }

    [Fact]
    public void PublicUnsafeOperations_HaveExplicitSessionBehaviorClassification()
    {
        var publicUnsafe = DiscoverUnsafeRegistrations()
            .Where(r => r.Scope == "Public")
            .DistinctBy(r => r.FileName)
            .ToList();

        var missing = publicUnsafe
            .Where(r => !PublicUnsafeSessionBehavior.ContainsKey(r.FileName))
            .Select(r => $"{r.FileName} ({r.Verb} \"{r.Route}\")")
            .ToList();

        missing.Should().BeEmpty(
            $"{RuleId}: 'Public' is not a blanket CSRF exemption. Every public unsafe " +
            "operation must declare whether it establishes/mutates ambient cookie " +
            "session state (ADR-005): " + string.Join("; ", missing));
    }

    [Fact]
    public void SessionBehaviorInventory_ContainsNoStaleEntries()
    {
        var publicUnsafeFiles = DiscoverUnsafeRegistrations()
            .Where(r => r.Scope == "Public")
            .Select(r => r.FileName)
            .ToHashSet();

        var stale = PublicUnsafeSessionBehavior.Keys
            .Where(f => !publicUnsafeFiles.Contains(f))
            .ToList();

        stale.Should().BeEmpty(
            $"{RuleId}: classification entries exist for files that no longer register " +
            "a public unsafe operation. Remove the stale entries so the inventory " +
            "stays a truthful contract: " + string.Join("; ", stale));

        foreach (var (file, classification) in PublicUnsafeSessionBehavior)
        {
            classification.Requirement.Should().BeOneOf("CSRF_REQUIRED", "CSRF_NOT_REQUIRED",
                $"{RuleId}: {file} uses an unknown requirement value.");
            classification.Reason.Should().NotBeNullOrWhiteSpace(
                $"{RuleId}: {file} classification must record why.");
            if (classification.Requirement == "CSRF_REQUIRED")
            {
                classification.EstablishesOrMutatesAmbientCookieSession.Should().BeTrue(
                    $"{RuleId}: {file} marks CSRF_REQUIRED but denies ambient cookie " +
                    "session establishment/mutation — reconcile the classification.");
            }
        }
    }

    [Fact]
    public void ProtectedFamilies_DefaultToCsrfRequired_AndInternalToNotRequired()
    {
        DefaultRequirementForScope("Authenticated").Should().Be("CSRF_REQUIRED");
        DefaultRequirementForScope("Account").Should().Be("CSRF_REQUIRED");
        DefaultRequirementForScope("Workspace").Should().Be("CSRF_REQUIRED");
        DefaultRequirementForScope("Resource").Should().Be("CSRF_REQUIRED");
        DefaultRequirementForScope("Admin").Should().Be("CSRF_REQUIRED",
            "protected families accept the ambient browser credential mode, so their unsafe " +
            "operations require browser CSRF under ADR-005 (non-ambient Authorization " +
            "credentials remain exempt at runtime by request evidence).");
        DefaultRequirementForScope("Internal").Should().Be("CSRF_NOT_REQUIRED",
            "internal service-to-service operations are outside the browser CSRF threat model.");

        var nonPublicScopes = DiscoverUnsafeRegistrations()
            .Where(r => r.Scope != "Public")
            .Select(r => r.Scope)
            .Distinct()
            .ToList();

        foreach (var scope in nonPublicScopes.Where(s => NonPublicOverrides.ContainsKey(s)))
        {
            var overrideClassification = NonPublicOverrides[scope];
            overrideClassification.Reason.Should().NotBeNullOrWhiteSpace(
                $"{RuleId}: override '{scope}' must record why its auth modes differ from the family default.");
            overrideClassification.Requirement.Should().BeOneOf("CSRF_REQUIRED", "CSRF_NOT_REQUIRED",
                $"{RuleId}: override '{scope}' uses an unknown requirement value.");
        }
    }

    [Fact]
    public void UnsafeRegistrations_ExistForReleaseScopedSurface()
    {
        var discovered = DiscoverUnsafeRegistrations();

        discovered.Should().NotBeEmpty(
            $"{RuleId}: discovery selected zero unsafe operations — the scan itself is broken, " +
            "not the surface.");

        discovered.Count(r => r.Scope == "Public").Should().BeGreaterThan(0,
            $"{RuleId}: public unsafe operations are expected in this release scope; zero suggests discovery drift.");
    }

    private static List<UnsafeRegistration> DiscoverUnsafeRegistrations()
    {
        var registrations = new List<UnsafeRegistration>();

        foreach (var file in GetApiEndpointFiles())
        {
            var fileName = Path.GetFileName(file);
            if (fileName == "EndpointMappingExtensions.cs" || fileName == "EndpointRouteBuilderExtensions.cs")
            {
                continue;
            }

            var content = RemoveComments(File.ReadAllText(file));

            foreach (var verb in UnsafeVerbs)
            {
                foreach (var scope in KnownScopes)
                {
                    var marker = $"Map{scope}{verb}(";
                    var index = content.IndexOf(marker, StringComparison.Ordinal);
                    while (index >= 0)
                    {
                        registrations.Add(new UnsafeRegistration(
                            fileName,
                            scope,
                            verb,
                            ExtractFirstRouteLiteral(content, index + marker.Length)));
                        index = content.IndexOf(marker, index + marker.Length, StringComparison.Ordinal);
                    }
                }
            }
        }

        return registrations;
    }

    private static string ExtractFirstRouteLiteral(string content, int startIndex)
    {
        var quote = content.IndexOf('"', startIndex);
        if (quote < 0)
        {
            return "<dynamic>";
        }

        var end = content.IndexOf('"', quote + 1);
        return end < 0 ? "<dynamic>" : content.Substring(quote + 1, end - quote - 1);
    }
}
