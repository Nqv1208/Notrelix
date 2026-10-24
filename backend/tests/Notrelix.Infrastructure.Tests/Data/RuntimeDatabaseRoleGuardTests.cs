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
        bool? owns = false) =>
        new(current, session, currentSuper, currentBypass, sessionSuper, sessionBypass, owns);

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
    public async Task StartAsync_WhenOff_NeverTouchesTheDatabase()
    {
        // No ApplicationDbContext is registered: resolving it would throw.
        var guard = new RuntimeDatabaseRoleGuard(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new RlsOptions()),
            NullLogger<RuntimeDatabaseRoleGuard>.Instance);

        new RlsOptions().RoleEnforcement.Should().Be(RlsRoleEnforcementMode.Off, "off is the default until the role switch");
        await guard.StartAsync(CancellationToken.None);
    }
}
