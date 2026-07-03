using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Abstractions.Rls;

namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly IOptions<RlsOptions> _options;
    private readonly ICurrentTenantContext _tenant;

    public RlsSessionContext(
        IOptions<RlsOptions> options,
        ICurrentTenantContext tenant)
    {
        _options = options;
        _tenant = tenant;
    }

    public async Task ApplyAsync(DatabaseFacade database, CancellationToken cancellationToken)
    {
        if (!_options.Value.SetSessionContext) return;

        var userId = _tenant.UserId?.ToString() ?? "";
        var accountId = _tenant.AccountId?.ToString() ?? "";
        var workspaceId = _tenant.WorkspaceId?.ToString() ?? "";
        var scope = _tenant.IsSystemContext ? "worker" : "app";
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? "";

        await database.ExecuteSqlInterpolatedAsync($@"
            SELECT set_config('app.current_user_id', {userId}, true);
            SELECT set_config('app.current_account_id', {accountId}, true);
            SELECT set_config('app.current_workspace_id', {workspaceId}, true);
            SELECT set_config('app.request_scope', {scope}, true);
            SELECT set_config('app.correlation_id', {correlationId}, true);
        ", cancellationToken);
    }
}
