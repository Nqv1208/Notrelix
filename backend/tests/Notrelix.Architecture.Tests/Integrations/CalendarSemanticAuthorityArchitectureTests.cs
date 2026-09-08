using System.Reflection;

namespace Notrelix.Architecture.Tests.Integrations;

/// <summary>
/// TAC-GATE-024 — Calendar semantic/persistence authority (structural side of TAC-FRZ-019).
///
/// The Calendar capability is a product binding on top of a generic provider
/// relationship. The gate enforces the semantic distinctions that the Calendar
/// execution record froze:
///
///   IntegrationConnection != CalendarIntegration
///   IntegrationSecretVersion != CurrentSecretRef pointer alone
///   inbound provider receipt != outbound WebhookDelivery
///   provider authenticity != ordinary authenticated application session
///
/// Enforced semantics:
///
///   1. Calendar capability owns its CalendarIntegration binding with an
///      authoritative ConnectionId relationship.
///   2. A connection-active guard exists as the binding authority.
///   3. Secret persistence authority is the persisted IntegrationSecretVersion
///      row; the in-memory CurrentSecretRef pointer must never be persisted.
///   4. Outbound WebhookDelivery must never be reused as an inbound receipt.
///   5. A real inbound provider webhook handler must verify provider signature;
///      the only allowed session-auth webhook shape is the exact unimplemented
///      M8-gap stub baseline (shrink it when M8 lands the receipt flow).
///   6. Tenant Account/Workspace scope must not be trusted from an unverified
///      provider payload.
///   7. Domain integration types must not carry raw secret strings — secrets
///      travel only inside SecretRef/secret-hash value objects.
///
/// This gate certifies structure only. A NotImplemented Calendar Real Flow
/// remains an M8 implementation gap and must not be marked VERIFIED during M3.
/// </summary>
public class CalendarSemanticAuthorityArchitectureTests : ArchitectureTestBase
{
    private const string GateId = "TAC-GATE-024";

    /// <summary>
    /// Exact M8-gap baseline: the unimplemented calendar webhook stub still
    /// carries the legacy session-auth request shape. When the real flow is
    /// implemented it must be replaced with a signature-verified inbound
    /// receipt and this baseline must shrink in the same change.
    /// </summary>
    private static readonly IReadOnlySet<string> SessionAuthWebhookStubBaseline =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs",
        };

    // ------------------------------------------------------------------
    // Production rules
    // ------------------------------------------------------------------

    [Fact]
    public void CalendarCapability_MustOwn_ConnectionIdRelationship()
    {
        var calendarIntegration = typeof(Notrelix.Domain.Integrations.Calendar.CalendarIntegration);
        var connection = typeof(Notrelix.Domain.Integrations.Connections.IntegrationConnection);

        calendarIntegration.Should().NotBeSameAs(
            connection,
            $"{GateId}: CalendarIntegration (product binding) and IntegrationConnection " +
            "(generic provider relationship) are distinct concepts");

        var connectionId = calendarIntegration.GetProperty(
            nameof(Notrelix.Domain.Integrations.Calendar.CalendarIntegration.ConnectionId),
            BindingFlags.Public | BindingFlags.Instance);

        connectionId.Should().NotBeNull(
            $"{GateId}: CalendarIntegration must expose its authoritative ConnectionId relationship");
        connectionId!.PropertyType.Should().Be<Guid>(
            $"{GateId}: ConnectionId must be a scalar relationship identifier");
    }

    [Fact]
    public void ConnectionActiveGuard_MustExist_AsBindingAuthority()
    {
        typeof(Notrelix.Domain.Integrations.Rules.CalendarSyncRules)
            .GetMethod(
                nameof(Notrelix.Domain.Integrations.Rules.CalendarSyncRules.EnsureConnectionActive),
                BindingFlags.Public | BindingFlags.Static)
            .Should().NotBeNull(
                $"{GateId}: Calendar binding flows must go through the connection-active guard");
    }

    [Fact]
    public void SecretAuthorityConcepts_MustExist()
    {
        typeof(Notrelix.Domain.Integrations.Connections.IntegrationSecretVersion)
            .Should().NotBeNull($"{GateId}: persisted secret authority concept must exist");

        typeof(Notrelix.Domain.SharedKernel.SecretRef)
            .Should().NotBeNull($"{GateId}: the secret-reference value object must exist");

        typeof(Notrelix.Domain.Integrations.Connections.IntegrationSecretVersion)
            .GetProperty(
                nameof(Notrelix.Domain.Integrations.Connections.IntegrationSecretVersion.SecretReference),
                BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull(
                $"{GateId}: IntegrationSecretVersion must carry the persisted SecretReference");

        typeof(Notrelix.Domain.Integrations.Connections.IntegrationSecretVersion)
            .GetProperty(
                nameof(Notrelix.Domain.Integrations.Connections.IntegrationSecretVersion.ConnectionId),
                BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull(
                $"{GateId}: IntegrationSecretVersion must belong to its IntegrationConnection");
    }

    [Fact]
    public void ConnectionSecretPersistence_MustUse_IntegrationSecretVersionAuthority()
    {
        var connectionConfig = ReadSourceFileOrThrow(
            Path.Combine("Data", "Configurations", "Integrations", "IntegrationConnectionConfiguration.cs"));

        connectionConfig.Should().Contain(
            "Ignore(x => x.CurrentSecretRef)",
            $"{GateId}: the in-memory CurrentSecretRef pointer is not durable secret persistence");

        connectionConfig.Should().NotContain(
            "Property(x => x.CurrentSecretRef)",
            $"{GateId}: CurrentSecretRef must never be EF-persisted as the secret authority");

        var versionConfig = ReadSourceFileOrThrow(
            Path.Combine("Data", "Configurations", "Integrations", "IntegrationSecretVersionConfiguration.cs"));

        versionConfig.Should().Contain(
            "Property(x => x.SecretReference)",
            $"{GateId}: IntegrationSecretVersion rows are the persisted secret authority");
    }

    [Fact]
    public void DomainIntegrations_MustNotCarry_RawSecretProperties()
    {
        var violations = CollectRawSecretViolations();

        violations.Should().BeEmpty(
            $"{GateId}: Domain integration types must not carry raw secret strings. " +
            "Wrap provider secrets in SecretRef or secret-hash value objects, persist " +
            "them only through IntegrationSecretVersion, and keep raw material out of " +
            "Domain/events/logs. Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void OutboundWebhookDelivery_MustNotBeReused_AsInboundReceipt()
    {
        var violations = ScanProductionSources(
            new[] { "WebhookDelivery" },
            IsPersistenceOwnedPath,
            GateId,
            "Outbound WebhookDelivery is delivery state, not an inbound provider receipt. " +
            "Inbound provider intake requires its own Infrastructure receipt/dedup authority.");

        violations.Should().BeEmpty(string.Join("\n", violations));
    }

    [Fact]
    public void InboundWebhookEvent_MustNotGrow_NewConsumers()
    {
        var violations = ScanProductionSources(
            new[] { "InboundWebhookEvent" },
            path => IsPersistenceOwnedPath(path) || IsDomainPath(path),
            GateId,
            "InboundWebhookEvent is an exact/no-growth legacy gap. Inbound intake belongs " +
            "to an Infrastructure-owned technical receipt authority (M8).");

        violations.Should().BeEmpty(string.Join("\n", violations));
    }

    [Fact]
    public void RealInboundProviderWebhook_MustVerifySignature_NotSessionAuthentication()
    {
        var violations = CollectSessionAuthWebhookViolations();

        violations.Should().BeEmpty(
            $"{GateId}: provider webhook authenticity must never rest solely on the " +
            "ordinary authenticated application session. Verify a provider signature " +
            "(HMAC) inside the handler and let Infrastructure own receipt/dedup. " +
            "Violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void TenantScope_MustNotBeTrusted_FromUnverifiedProviderPayload()
    {
        var violations = CollectPayloadTenantExtractionViolations();

        violations.Should().BeEmpty(
            $"{GateId}: tenant Account/Workspace scope must come from the verified " +
            "integration envelope or authoritative resolution — never from an " +
            "unverified provider payload. Violations:\n" + string.Join("\n", violations));
    }

    // ------------------------------------------------------------------
    // Gate self-tests (regression rejection proof)
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Detects_RawSecretProperty_InDomainIntegrations()
    {
        // Explicit isolated violating fixture: a hypothetical Domain calendar
        // entity exposing the provider token as a raw string.
        ClassifyRawSecretProperty("CalendarCapability", "AccessToken", "String")
            .Should().NotBeNull("a raw string secret property must be flagged");

        ClassifyRawSecretProperty("CalendarCapability", "RefreshTokenValue", "String")
            .Should().NotBeNull("a raw string token property must be flagged");
    }

    [Fact]
    public void Gate_Allows_SanctionedSecretShapes_InDomainIntegrations()
    {
        ClassifyRawSecretProperty("IntegrationConnection", "CurrentSecretRef", "SecretRef")
            .Should().BeNull("SecretRef value objects are the sanctioned secret carrier");

        ClassifyRawSecretProperty("WebhookSubscription", "SecretHash", "WebhookSecretHash")
            .Should().BeNull("secret-hash value objects are sanctioned");

        ClassifyRawSecretProperty("IntegrationConnection", "CurrentSecretVersion", "String")
            .Should().BeNull("a version pointer is not secret material");

        ClassifyRawSecretProperty("IntegrationSecretRotatedDomainEvent", "Version", "String")
            .Should().BeNull("rotation events carry the version, never the secret");
    }

    [Fact]
    public void Gate_Detects_WebhookDelivery_InboundReuse()
    {
        var violating = "\n        var receipt = WebhookDelivery.Create(...);\n";
        DetectInboundReceiptViolation("Features/Integrations/Calendar/receipt.cs", violating, "WebhookDelivery")
            .Should().NotBeNull("reusing outbound delivery state as an inbound receipt must be flagged");

        var compliant = "\n        var delivery = WebhookDelivery.Create(...);\n";
        DetectInboundReceiptViolation("Data/Configurations/Integrations/WebhookDeliveryConfiguration.cs", compliant, "WebhookDelivery")
            .Should().BeNull("persistence-owned references are legitimate");
    }

    [Fact]
    public void Gate_Detects_SessionAuthenticated_RealWebhookHandler()
    {
        const string stub = "throw new NotImplementedException();";
        ClassifySessionAuthWebhook("Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs", stub)
            .Should().BeNull("the exact unimplemented M8-gap stub is baselined");

        const string realWithSignature =
            "public class HandleOtherWebhookCommandHandler : IRequestHandler<HandleOtherWebhookCommand, Result>\n" +
            "    => await _signatureService.VerifyAsync(payload);";
        ClassifySessionAuthWebhook("Features/Integrations/Other/Commands/HandleOtherWebhook/HandleOtherWebhook.cs", realWithSignature)
            .Should().BeNull("a signature-verified handler is compliant");

        const string realWithoutSignature =
            "public class HandleOtherWebhookCommandHandler : IRequestHandler<HandleOtherWebhookCommand, Result>\n" +
            "    => await _mediator.Send(new ProcessWebhook(request.Payload));";
        ClassifySessionAuthWebhook("Features/Integrations/Other/Commands/HandleOtherWebhook/HandleOtherWebhook.cs", realWithoutSignature)
            .Should().NotBeNull("a real handler without signature verification must be flagged");

        const string nonHandler = "public record HandleOtherWebhookCommand(string Payload) : ICommand<Result>;";
        ClassifySessionAuthWebhook("Features/Integrations/Other/Commands/HandleOtherWebhook/HandleOtherWebhook.cs", nonHandler)
            .Should().BeNull("command/validator surfaces are not handler authenticity evidence");
    }

    [Fact]
    public void Gate_Detects_PayloadTenantExtraction()
    {
        const string violating = "var workspaceId = payload.RootElement.GetProperty(\"workspaceId\").GetGuid();";
        DetectPayloadTenantExtraction(violating)
            .Should().NotBeNull("tenant ids extracted from a provider payload must be flagged");

        const string compliant = "var workspaceId = message.WorkspaceId;";
        DetectPayloadTenantExtraction(compliant)
            .Should().BeNull("envelope-carried scope is compliant");
    }

    [Fact]
    public void Gate_Detects_SecretRef_PersistenceViolation()
    {
        const string violating = "builder.Property(x => x.CurrentSecretRef).HasColumnName(\"current_secret_ref\");";
        DetectCurrentSecretRefPersistence(violating)
            .Should().NotBeNull("persisting CurrentSecretRef as the secret authority must be flagged");

        const string compliant = "builder.Ignore(x => x.CurrentSecretRef);";
        DetectCurrentSecretRefPersistence(compliant)
            .Should().BeNull("ignoring the in-memory pointer is the required shape");
    }

    [Fact]
    public void Gate_SessionAuthStubBaseline_IsExact_AndNonEmpty()
    {
        SessionAuthWebhookStubBaseline.Should().NotBeEmpty();
        SessionAuthWebhookStubBaseline.Should().OnlyContain(p =>
            p.StartsWith("Features/Integrations/", StringComparison.Ordinal) &&
            p.Contains("Webhook", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------
    // Detectors (pure functions so self-tests exercise rejection directly)
    // ------------------------------------------------------------------

    private static string? ClassifyRawSecretProperty(string typeName, string propertyName, string propertyTypeName)
    {
        var rawSecretPattern = new Regex(
            "secret|token|password|apikey|credential",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var pointerOrHashPattern = new Regex(
            "secretversion|version|secretref|secrethash|hash$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        if (!string.Equals(propertyTypeName, "String", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!rawSecretPattern.IsMatch(propertyName))
            return null;

        if (pointerOrHashPattern.IsMatch(propertyName))
            return null;

        return $"{typeName}.{propertyName} (string) — raw secret material in Domain integration type";
    }

    private static List<string> CollectRawSecretViolations()
    {
        var violations = new List<string>();

        var domainIntegrationsTypes = Assembly.Load("Notrelix.Domain")
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.Integrations", StringComparison.Ordinal) == true);

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var type in domainIntegrationsTypes)
        {
            foreach (var property in type.GetProperties(flags))
            {
                var violation = ClassifyRawSecretProperty(
                    type.Name,
                    property.Name,
                    property.PropertyType.Name);

                if (violation is not null)
                    violations.Add($"{type.FullName}: {violation}");
            }
        }

        return violations.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList();
    }

    private static string? DetectCurrentSecretRefPersistence(string configurationSource)
    {
        if (configurationSource.Contains("Property(x => x.CurrentSecretRef)", StringComparison.Ordinal))
            return "CurrentSecretRef is EF-mapped as durable secret persistence";

        return null;
    }

    private static string? DetectInboundReceiptViolation(
        string relativePath,
        string source,
        string marker)
    {
        if (!source.Contains(marker, StringComparison.Ordinal))
            return null;

        if (IsPersistenceOwnedPath(relativePath) || IsDomainPath(relativePath))
            return null;

        return $"{relativePath} references {marker} outside Domain/persistence — " +
               "potential outbound/inbound semantic reuse";
    }

    private static string? ClassifySessionAuthWebhook(string relativePath, string handlerSource)
    {
        var normalized = relativePath.Replace('\\', '/');

        if (handlerSource.Contains("NotImplementedException", StringComparison.Ordinal))
        {
            return SessionAuthWebhookStubBaseline.Contains(normalized)
                ? null
                : $"{normalized}: unimplemented webhook stub is not in the exact M8-gap baseline";
        }

        if (!handlerSource.Contains("IRequestHandler", StringComparison.Ordinal))
            return null;

        if (normalized.Contains("Webhook", StringComparison.Ordinal) &&
            !handlerSource.Contains("Signature", StringComparison.OrdinalIgnoreCase))
        {
            return $"{normalized}: real webhook handler verifies no provider signature — " +
                   "provider authenticity must not rely on the user session";
        }

        return null;
    }

    private static string? DetectPayloadTenantExtraction(string source)
    {
        var payloadTenantPattern = new Regex(
            "GetProperty\\(\\s*\"(workspaceId|accountId|workspace_id|account_id)\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return payloadTenantPattern.IsMatch(source)
            ? "tenant scope extracted directly from a provider payload"
            : null;
    }

    private static List<string> CollectSessionAuthWebhookViolations()
    {
        var violations = new List<string>();

        var webhookFiles = Directory
            .GetFiles(Path.Combine(GetApplicationPath(), "Features", "Integrations"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => Path.GetFileName(f).Contains("Webhook", StringComparison.OrdinalIgnoreCase));

        foreach (var file in webhookFiles)
        {
            var relative = Path.GetRelativePath(GetApplicationPath(), file).Replace('\\', '/');
            var violation = ClassifySessionAuthWebhook(relative, File.ReadAllText(file));

            if (violation is not null)
                violations.Add(violation);
        }

        return violations;
    }

    private static List<string> CollectPayloadTenantExtractionViolations()
    {
        var violations = new List<string>();

        foreach (var projectPath in new[] { GetApplicationPath(), GetInfrastructurePath() })
        {
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var violation = DetectPayloadTenantExtraction(File.ReadAllText(file));

                if (violation is not null)
                    violations.Add($"{Path.GetRelativePath(projectPath, file).Replace('\\', '/')}: {violation}");
            }
        }

        return violations;
    }

    private static List<string> ScanProductionSources(
        IReadOnlyList<string> markers,
        Func<string, bool> allowedPath,
        string gateId,
        string semantic)
    {
        var violations = new List<string>();

        foreach (var projectPath in new[] { GetApplicationPath(), GetInfrastructurePath(), GetApiPath() })
        {
            if (!Directory.Exists(projectPath))
                continue;

            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(GetSrcPath(), file).Replace('\\', '/');
                var content = File.ReadAllText(file);

                foreach (var marker in markers)
                {
                    if (!content.Contains(marker, StringComparison.Ordinal))
                        continue;

                    if (allowedPath(relative))
                        continue;

                    violations.Add($"{gateId}: {relative} references {marker} — {semantic}");
                }
            }
        }

        return violations;
    }

    private static bool IsPersistenceOwnedPath(string relativePath)
        => relativePath.Contains("Data/", StringComparison.Ordinal)
           || relativePath.EndsWith(
               "Abstractions/IIntegrationDbContext.cs",
               StringComparison.Ordinal);

    private static bool IsDomainPath(string relativePath)
        => relativePath.StartsWith("Notrelix.Domain/", StringComparison.Ordinal);

    private static string ReadSourceFileOrThrow(string relativeToInfrastructure)
    {
        var path = Path.Combine(GetInfrastructurePath(), relativeToInfrastructure);

        return File.Exists(path)
            ? File.ReadAllText(path)
            : throw new FileNotFoundException($"{GateId}: expected infrastructure source is missing: {path}");
    }
}
