namespace Notrelix.Application.Common.Security;

public interface IPermissionVersionProvider
{
    ValueTask<string> GetVersionAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken);
}
