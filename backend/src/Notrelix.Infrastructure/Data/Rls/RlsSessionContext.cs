using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Abstractions.Rls;

namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly IOptions<RlsOptions> _options;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentWorkspace _currentWorkspace;

    public RlsSessionContext(
        IOptions<RlsOptions> options,
        ICurrentUser currentUser,
        ICurrentWorkspace currentWorkspace)
    {
        _options = options;
        _currentUser = currentUser;
        _currentWorkspace = currentWorkspace;
    }

    public async Task ApplyAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (!_options.Value.SetSessionContext) return;

        var userId = _currentUser.IsAuthenticated ? _currentUser.UserId.ToString() ?? "" : "";
        var workspaceId = _currentWorkspace.IsSet && _currentWorkspace.WorkspaceId.HasValue
            ? _currentWorkspace.WorkspaceId.Value.ToString()
            : "";
        var scope = _currentWorkspace.IsSystemContext ? "system" : "api";
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? "";

        await context.Database.ExecuteSqlInterpolatedAsync($@"
            SELECT set_config('app.current_user_id', {userId}, true);
            SELECT set_config('app.current_workspace_id', {workspaceId}, true);
            SELECT set_config('app.request_scope', {scope}, true);
            SELECT set_config('app.correlation_id', {correlationId}, true);
        ", cancellationToken);
    }
}
