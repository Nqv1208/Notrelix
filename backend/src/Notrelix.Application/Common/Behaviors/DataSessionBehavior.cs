using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public sealed class DataSessionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IExecutionContextReader _executionContext;
    private readonly IRequestDataSession _dataSession;

    public DataSessionBehavior(
        IRequestDescriptorRegistry descriptors,
        IExecutionContextReader executionContext,
        IRequestDataSession dataSession)
    {
        _descriptors = descriptors;
        _executionContext = executionContext;
        _dataSession = dataSession;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var stage = PipelineActivitySource.Instance.StartActivity("data_session");

        var descriptor = _descriptors.GetRequired(typeof(TRequest));
        var access = descriptor.DataAccess switch
        {
            ApplicationDataAccessKind.Write => RequestDataAccess.Transactional,
            ApplicationDataAccessKind.Read => RequestDataAccess.ReadOnly,
            ApplicationDataAccessKind.None when descriptor.Access.RequiresDatastoreFacts => RequestDataAccess.ReadOnly,
            _ => RequestDataAccess.None,
        };

        if (access == RequestDataAccess.None)
        {
            return await next();
        }

        var snapshot = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException(
                $"Execution context is not resolved for {descriptor.RequestType.Name}.");
        var tenantScoped = snapshot.Scope is
            ApplicationScopeKind.Account or ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource;
        var expectedVersion = request is IExpectedVersionRequest versioned
            ? new ExpectedVersionConstraint(typeof(TRequest), versioned.Resource, versioned.ExpectedVersion)
            : null;

        return await _dataSession.ExecuteAsync(
            new RequestDataSessionOptions(
                access,
                ApplyTenantScope: tenantScoped,
                ApplyResourceScope: snapshot.Scope == ApplicationScopeKind.Resource,
                ExpectedVersion: expectedVersion),
            _ => next(),
            cancellationToken);
    }
}
