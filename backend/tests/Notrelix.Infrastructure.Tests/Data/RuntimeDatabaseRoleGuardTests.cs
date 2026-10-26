using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Notrelix.Infrastructure.Tests.Data;

public class RuntimeDatabaseRoleGuardTests
{
    private static RuntimeDatabaseRoleFacts Facts(
        string current = "notrelix_app",
        string session = "notrelix_app",
        bool currentSuper = false,
        bool currentBypass = false,
        bool sessionSuper = false,
        bool sessionBypass = false,
        bool? owns = false,
        bool workerMember = false) =>
        new(current, session, currentSuper, currentBypass, sessionSuper, sessionBypass, owns, workerMember);

    [Fact]
    public void Evaluate_PlainApplicationRole_HasNoViolations() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts()).Should().BeEmpty();

    [Fact]
    public void Evaluate_ApplicationRoleBeforeTheTablesExist_HasNoViolations() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(owns: null)).Should().BeEmpty();

    [Fact]
    public void Evaluate_Superuser_IsRejected() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(current: "postgres", session: "postgres", currentSuper: true))
            .Should().ContainSingle().Which.Should().Contain("superuser");

    [Fact]
    public void Evaluate_BypassRlsRole_IsRejected() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(currentBypass: true))
            .Should().ContainSingle().Which.Should().Contain("BYPASSRLS");

    [Fact]
    public void Evaluate_TableOwner_IsRejected() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(owns: true))
            .Should().ContainSingle().Which.Should().Contain("owns the tenant tables");

    [Fact]
    public void Evaluate_SetRoleFromASuperuserLogin_IsRejectedBecauseTheLoginCanResetTheRole() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(session: "postgres", sessionSuper: true))
            .Should().ContainSingle().Which.Should().Contain("RESET ROLE");

    [Fact]
    public void Evaluate_SetRoleFromABypassRlsLogin_IsRejected() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(session: "login", sessionBypass: true))
            .Should().ContainSingle().Which.Should().Contain("BYPASSRLS");

    [Fact]
    public void Evaluate_ReportsEveryViolation() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(
                current: "postgres", session: "postgres", currentSuper: true, currentBypass: true, owns: true))
            .Should().HaveCount(3);

    [Fact]
    public void Evaluate_ApplicationLoginThatIsAWorkerRoleMember_IsRejected() =>
        RuntimeDatabaseRoleGuard.Evaluate(Facts(workerMember: true))
            .Should().ContainSingle().Which.Should().Contain("notrelix_worker");

    [Fact]
    public void EvaluateWorker_PlainWorkerRoleMember_HasNoViolations() =>
        RuntimeDatabaseRoleGuard.EvaluateWorker(Facts(current: "notrelix_worker", session: "notrelix_worker", workerMember: true))
            .Should().BeEmpty();

    [Fact]
    public void EvaluateWorker_LoginThatIsNotAWorkerRoleMember_IsRejected() =>
        RuntimeDatabaseRoleGuard.EvaluateWorker(Facts(workerMember: false))
            .Should().ContainSingle().Which.Should().Contain("not a member of notrelix_worker");

    [Fact]
    public void EvaluateWorker_PrivilegedWorkerLogin_IsRejectedLikeTheApplicationLogin() =>
        RuntimeDatabaseRoleGuard.EvaluateWorker(Facts(
                current: "postgres", session: "postgres", currentSuper: true, currentBypass: true, owns: true, workerMember: true))
            .Should().HaveCount(3);

    [Fact]
    public async Task StartAsync_WhenOff_NeverTouchesTheDatabase()
    {
        // No ApplicationDbContext is registered: resolving it would throw.
        new RlsOptions().RoleEnforcement.Should().Be(RlsRoleEnforcementMode.Enforce, "the default fails closed; opting out is explicit");
        var off = new RuntimeDatabaseRoleGuard(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new RlsOptions { RoleEnforcement = RlsRoleEnforcementMode.Off }),
            NullLogger<RuntimeDatabaseRoleGuard>.Instance);
        await off.StartAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void ShippedAppsettings_ResolveToEnforce_ForEveryNonTestEnvironment(string environment)
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
            current = Path.GetDirectoryName(current);
        var api = Path.Combine(current ?? throw new DirectoryNotFoundException("backend.slnx"), "src", "Notrelix.API");

        // Later files override earlier ones, as in the host's configuration order.
        var options = new RlsOptions();
        foreach (var file in new[] { "appsettings.json", $"appsettings.{environment}.json" })
        {
            var path = Path.Combine(api, file);
            if (!File.Exists(path)) continue;
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("Rls", out var rls)
                && rls.TryGetProperty("RoleEnforcement", out var mode))
            {
                options.RoleEnforcement = Enum.Parse<RlsRoleEnforcementMode>(mode.GetString()!, ignoreCase: true);
            }
        }

        options.RoleEnforcement.Should().Be(RlsRoleEnforcementMode.Enforce);
    }
}
