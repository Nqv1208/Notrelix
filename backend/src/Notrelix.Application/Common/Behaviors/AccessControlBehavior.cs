using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public sealed class AccessControlBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly AccessFacts NoFacts = new(
        false, false, false, null, false, null, false, null, null, false, [], false, false, null, null);

    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IExecutionContextReader _executionContext;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly PipelineMetrics _metrics;
    private readonly ILogger<AccessControlBehavior<TRequest, TResponse>> _logger;

    public AccessControlBehavior(
        IRequestDescriptorRegistry descriptors,
        IExecutionContextReader executionContext,
        IAccessFactsProvider factsProvider,
        IAccessPolicyEvaluator policy,
        PipelineMetrics metrics,
        ILogger<AccessControlBehavior<TRequest, TResponse>> logger)
    {
        _descriptors = descriptors;
        _executionContext = executionContext;
        _factsProvider = factsProvider;
        _policy = policy;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var descriptor = _descriptors.GetRequired(typeof(TRequest));
        var context = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException(
                $"Execution context is not resolved for {descriptor.RequestType.Name}.");
        var facts = descriptor.Access.RequiresDatastoreFacts
            ? await ResolveFactsAsync(descriptor, context, request, cancellationToken)
            : NoFacts;
        AccessDecision decision;
        using (PipelineActivitySource.Instance.StartActivity("policy.evaluate"))
        {
            decision = _policy.Evaluate(descriptor, context, facts, request);
        }

        RecordDecision(descriptor, decision, request);

        switch (decision.Kind)
        {
            case AccessDecisionKind.Allowed:
                return await next();
            case AccessDecisionKind.Unauthorized:
                throw new UnauthorizedException(decision.Message ?? "Authentication required.");
            case AccessDecisionKind.NotFound:
                if (request is IRequirePermission permission && permission.Resource is not null)
                {
                    throw new NotFoundException(permission.Resource.Kind.ToString(), permission.Resource.ResourceId);
                }

                throw new NotFoundException(descriptor.RequestType.Name, Guid.Empty);
            case AccessDecisionKind.SecurityMisconfiguration:
                throw new SecurityMisconfigurationException(decision.Message ?? "Invalid access contract.");
            default:
                throw new ForbiddenException(
                    decision.Message ?? "You do not have permission to perform this action.");
        }
    }

    private void RecordDecision(RequestDescriptor descriptor, AccessDecision decision, TRequest request)
    {
        _metrics.AuthorizationDecisions.Add(
            1,
            new KeyValuePair<string, object?>("decision.kind", decision.Kind.ToString()));

        if (decision.Kind == AccessDecisionKind.Allowed)
        {
            return;
        }

        // Denials are recorded with code-bounded categories and structural
        // identifiers only. The decision message is deliberately excluded: it is
        // policy prose that may describe the rule or resource and must not
        // become a log payload.
        var permission = request as IRequirePermission;

        _logger.LogWarning(
            "Authorization denied {RequestName} {PrincipalKind} {ResourceKind} {PermissionAction} {DecisionKind}",
            descriptor.RequestType.Name,
            descriptor.Principal.ToString(),
            permission?.Resource?.Kind.ToString() ?? "-",
            permission?.Action.ToString() ?? "-",
            decision.Kind.ToString());
    }

    private async Task<AccessFacts> ResolveFactsAsync(
            RequestDescriptor descriptor,
            ExecutionContextSnapshot context,
            TRequest request,
            CancellationToken cancellationToken)
    {
        using var stage = PipelineActivitySource.Instance.StartActivity("access_facts.query");
        return await _factsProvider.ResolveAsync(descriptor, context, request, cancellationToken);
    }
}
