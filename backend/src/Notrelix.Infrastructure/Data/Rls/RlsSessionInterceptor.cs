using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionInterceptor : DbCommandInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentWorkspace _currentWorkspace;

    public RlsSessionInterceptor(
        ICurrentUser currentUser,
        ICurrentWorkspace currentWorkspace)
    {
        _currentUser = currentUser;
        _currentWorkspace = currentWorkspace;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        PrependSessionParameters(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        PrependSessionParameters(command);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        PrependSessionParameters(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PrependSessionParameters(command);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override InterceptionResult<object?> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object?> result)
    {
        PrependSessionParameters(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object?>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object?> result,
        CancellationToken cancellationToken = default)
    {
        PrependSessionParameters(command);
        return new ValueTask<InterceptionResult<object?>>(result);
    }

    private void PrependSessionParameters(DbCommand command)
    {
        var sql = BuildSetSql();
        if (string.IsNullOrEmpty(sql)) return;

        command.CommandText = sql + "\n" + command.CommandText;
    }

    private string BuildSetSql()
    {
        var cmds = new List<string>();

        if (_currentUser.IsAuthenticated)
            cmds.Add($"SET LOCAL app.current_user_id = '{_currentUser.UserId}'");
        else
            cmds.Add("SET LOCAL app.current_user_id = ''");

        if (_currentWorkspace.IsSet && _currentWorkspace.WorkspaceId.HasValue)
            cmds.Add($"SET LOCAL app.current_workspace_id = '{_currentWorkspace.WorkspaceId.Value}'");
        else
            cmds.Add("SET LOCAL app.current_workspace_id = ''");

        cmds.Add($"SET LOCAL app.request_scope = '{(_currentWorkspace.IsSystemContext ? "system" : "api")}'");

        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrEmpty(activityId))
            cmds.Add($"SET LOCAL app.correlation_id = '{activityId}'");

        return cmds.Count > 0 ? string.Join("; ", cmds) + "; " : string.Empty;
    }
}
