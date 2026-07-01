using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Abstractions.Rls;

namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly IOptions<RlsOptions> _options;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentWorkspace _currentWorkspace;
    private readonly ICurrentAccount _currentAccount;

    public RlsSessionContext(
        IOptions<RlsOptions> options,
        ICurrentUser currentUser,
        ICurrentWorkspace currentWorkspace,
        ICurrentAccount currentAccount)
    {
        _options = options;
        _currentUser = currentUser;
        _currentWorkspace = currentWorkspace;
        _currentAccount = currentAccount;
    }

    public async Task ApplyAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (!_options.Value.SetSessionContext) return;

        var userId = _currentUser.IsAuthenticated ? _currentUser.UserId.ToString() ?? "" : "";
        var accountId = _currentAccount.IsSet && _currentAccount.AccountId.HasValue
            ? _currentAccount.AccountId.Value.ToString()
            : "";
        var workspaceId = _currentWorkspace.IsSet && _currentWorkspace.WorkspaceId.HasValue
            ? _currentWorkspace.WorkspaceId.Value.ToString()
            : "";
        var scope = _currentWorkspace.IsSystemContext ? "system" : "api";
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? "";

        await context.Database.ExecuteSqlInterpolatedAsync($@"
            SELECT set_config('app.current_user_id', {userId}, true);
            SELECT set_config('app.current_account_id', {accountId}, true);
            SELECT set_config('app.current_workspace_id', {workspaceId}, true);
            SELECT set_config('app.request_scope', {scope}, true);
            SELECT set_config('app.correlation_id', {correlationId}, true);
        ", cancellationToken);
    }
}
