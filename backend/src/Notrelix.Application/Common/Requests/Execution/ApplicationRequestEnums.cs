namespace Notrelix.Application.Common.Requests.Execution;

public enum ApplicationRequestKind
{
    Command,
    Query
}

public enum ApplicationPrincipalKind
{
    Anonymous,
    Authenticated,
    System
}

public enum ApplicationScopeKind
{
    Global,
    Account,
    Workspace,
    Resource,
    Token
}

public enum ApplicationDataAccessKind
{
    None,
    ReadOnly,
    Transactional
}

public enum ApplicationCacheKind
{
    None,
    Public,
    Authorized
}
