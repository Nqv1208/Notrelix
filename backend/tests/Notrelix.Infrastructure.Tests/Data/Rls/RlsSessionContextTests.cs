using Notrelix.Application.Common.Context;

namespace Notrelix.Infrastructure.Tests.Data.Rls;

public class RlsSessionContextTests
{
    private static RlsSessionContext CreateContext(
        bool setSessionContext,
        ICurrentTenantContext tenant)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new RlsOptions
            {
                Enabled = true,
                SetSessionContext = setSessionContext,
                ApplyPoliciesOnStartup = false
            });
        return new RlsSessionContext(options, tenant);
    }

    private static Mock<ICurrentTenantContext> CreateTenant(
        bool isSystem = false,
        Guid? accountId = null,
        Guid? workspaceId = null,
        Guid? userId = null)
    {
        var mock = new Mock<ICurrentTenantContext>();
        mock.Setup(x => x.IsSystemContext).Returns(isSystem);
        mock.Setup(x => x.AccountId).Returns(accountId);
        mock.Setup(x => x.WorkspaceId).Returns(workspaceId);
        mock.Setup(x => x.UserId).Returns(userId);
        return mock;
    }

    [Fact]
    public async Task ApplyAsync_SetSessionContextFalse_NonSystem_Throws()
    {
        var tenant = CreateTenant(isSystem: false, accountId: Guid.NewGuid());
        var context = CreateContext(setSessionContext: false, tenant.Object);

        var act = () => context.ApplyAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SetSessionContext*disabled*");
    }

    [Fact]
    public async Task ApplyAsync_SetSessionContextFalse_System_DoesNotThrow()
    {
        var tenant = CreateTenant(isSystem: true);
        var context = CreateContext(setSessionContext: false, tenant.Object);

        var act = () => context.ApplyAsync(null!, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyAsync_AccountIdMissing_NonSystem_Throws()
    {
        var tenant = CreateTenant(isSystem: false, accountId: null, workspaceId: Guid.NewGuid());
        var context = CreateContext(setSessionContext: true, tenant.Object);

        var act = () => context.ApplyAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AccountId*required*");
    }

    [Fact]
    public async Task ApplyAsync_AccountIdPresent_NonSystem_PassesValidation()
    {
        var tenant = CreateTenant(isSystem: false, accountId: Guid.NewGuid(), workspaceId: Guid.NewGuid());
        var context = CreateContext(setSessionContext: true, tenant.Object);

        var act = () => context.ApplyAsync(null!, CancellationToken.None);

        var ex = await Record.ExceptionAsync(act);
        ex.Should().NotBeNull("should fail because database is null");
        ex.Should().NotBeOfType<InvalidOperationException>("tenant validation should pass");
    }
}
