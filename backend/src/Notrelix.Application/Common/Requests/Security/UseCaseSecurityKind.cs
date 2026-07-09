namespace Notrelix.Application.Common.Requests.Security;

public enum UseCaseSecurityKind
{
    Anonymous = 0,
    AuthenticatedUser = 1,
    AccountScoped = 2,
    WorkspaceScoped = 3,
    SystemInternal = 4
}
