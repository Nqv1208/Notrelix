using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Notrelix.Application.Common.Diagnostics;

namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed record ObservabilityAnonymousRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest, INoDataRequest;
public sealed record ObservabilityTaggedRequest : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, INoDataRequest;
public sealed record ObservabilityVerifiedRequest
    : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, INoDataRequest, IRequireVerifiedEmail;
public sealed record ObservabilityWriteRequest(Guid WorkspaceId)
    : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IWriteRequest;
[IdempotencyOperation("test.observability.write.v1")]
public sealed record ObservabilityIdempotentWriteRequest(Guid WorkspaceId)
    : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IWriteRequest, IIdempotentRequest;

/// <summary>
/// Phase 13 observability proof: the frozen pipeline emits a root request
/// activity with descriptor tags plus stage-level activities, so tracing can
/// distinguish pipeline stages (request.contract, context.resolve,
/// access.facts, access.evaluate, idempotency, data_session).
/// </summary>
public sealed class PipelineObservabilityTests
{
    private const string SourceName = PipelineActivitySource.SourceName;

    [Fact]
    public async Task Tracing_behavior_emits_root_activity_with_descriptor_tags()
    {
        using var recorder = new ActivityRecorder();
        var behavior = CreateTracingBehavior<ObservabilityTaggedRequest>();

        await behavior.Handle(new ObservabilityTaggedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        var root = recorder.Started.Single(activity => activity.OperationName == "pipeline.request");
        root.GetTagItem("request.name").Should().Be(nameof(ObservabilityTaggedRequest));
        root.GetTagItem("request.kind").Should().Be(nameof(ApplicationRequestKind.Command));
        root.GetTagItem("principal.kind").Should().Be(nameof(ApplicationPrincipalKind.Authenticated));
        root.GetTagItem("scope.kind").Should().Be(nameof(ApplicationScopeKind.Global));
        root.GetTagItem("data_access.kind").Should().Be(nameof(ApplicationDataAccessKind.None));
        root.GetTagItem("deployment.environment").Should().Be("Testing");
        root.GetTagItem("pipeline.outcome").Should().Be("success");
    }

    [Fact]
    public async Task Tracing_behavior_does_not_emit_handler_span()
    {
        // Canonical ownership moved: handler.execute is emitted by the innermost
        // IdempotencyBehavior around the actual invocation only.
        var recorder = new ActivityRecorder();
        var behavior = CreateTracingBehavior<ObservabilityWriteRequest>();

        await behavior.Handle(new ObservabilityWriteRequest(Guid.NewGuid()),
            _ => Task.FromResult("ok"), CancellationToken.None);

        recorder.Names.Should().NotContain("handler.execute");
        recorder.Names.Should().Contain("pipeline.request");
    }

    [Fact]
    public async Task Tracing_behavior_tags_failed_outcome_with_exception_type()
    {
        using var recorder = new ActivityRecorder();
        var behavior = CreateTracingBehavior<ObservabilityTaggedRequest>();

        var act = () => behavior.Handle(
            new ObservabilityTaggedRequest(),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        var root = recorder.Started.Single(activity => activity.OperationName == "pipeline.request");
        root.GetTagItem("pipeline.outcome").Should().Be("failure:internal_error");
    }

    [Fact]
    public async Task Access_control_behavior_emits_facts_and_evaluate_stages()
    {
        using var recorder = new ActivityRecorder();
        var provider = new Mock<IAccessFactsProvider>();
        provider.Setup(p => p.ResolveAsync(
                It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(userExists: true, emailVerified: true));

        var behavior = CreateAccessBehavior<ObservabilityVerifiedRequest>(provider.Object);

        await behavior.Handle(new ObservabilityVerifiedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        recorder.Names.Should().Contain("access_facts.query");
        recorder.Names.Should().Contain("policy.evaluate");
    }

    [Fact]
    public async Task Data_session_behavior_DelegatesSpanOwnership_ToDataSession()
    {
        // Canonical ownership: `data_session.*` spans are emitted by the
        // Infrastructure IRequestDataSession, not by the behavior wrapper.
        var recorder = new ActivityRecorder();
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(It.IsAny<Type>()))
            .Returns(RequestDescriptorValidator.Create(typeof(ObservabilityWriteRequest)));
        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(reader => reader.Snapshot)
            .Returns(new ExecutionContextSnapshot(
                Guid.NewGuid(), Guid.NewGuid(), null, null,
                ApplicationPrincipalKind.Authenticated,
                ApplicationScopeKind.Workspace,
                Guid.NewGuid().ToString("D")));

        var behavior = new DataSessionBehavior<ObservabilityWriteRequest, string>(
            descriptors.Object,
            executionContext.Object,
            new Mock<IRequestDataSession>().Object);

        await behavior.Handle(new ObservabilityWriteRequest(Guid.NewGuid()), _ => Task.FromResult("ok"), CancellationToken.None);

        recorder.Names.Should().NotContain(name => name.StartsWith("data_session", StringComparison.Ordinal),
            "the behavior must not duplicate stage spans owned by the data session");
    }

    [Fact]
    public async Task Request_contract_behavior_emits_contract_stage()
    {
        using var recorder = new ActivityRecorder();
        var behavior = CreateContractBehavior<ObservabilityAnonymousRequest>();

        await behavior.Handle(new ObservabilityAnonymousRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        recorder.Names.Should().Contain("request.contract");
    }

    [Fact]
    public async Task Execution_context_behavior_emits_resolve_stage()
    {
        using var recorder = new ActivityRecorder();
        var behavior = CreateExecutionContextBehavior<ObservabilityAnonymousRequest>();

        await behavior.Handle(new ObservabilityAnonymousRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        recorder.Names.Should().Contain("execution_context.resolve");
    }

    [Fact]
    public async Task Idempotency_behavior_emits_acquire_and_complete_stages()
    {
        using var recorder = new ActivityRecorder();

        var store = new Mock<IIdempotencyStore>();
        store.Setup(s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null));

        var fingerprint = new Mock<IIdempotencyRequestFingerprint>();
        fingerprint.Setup(f => f.Compute(It.IsAny<ObservabilityIdempotentWriteRequest>(), It.IsAny<Type>()))
            .Returns("request-hash");

        var executionContext = new Mock<IIdempotencyExecutionContext>();
        executionContext.Setup(context => context.RequireKey()).Returns("raw-idempotency-key");

        var tenant = new Mock<ICurrentTenantContext>();
        tenant.SetupGet(context => context.AccountId).Returns(Guid.NewGuid());
        tenant.SetupGet(context => context.WorkspaceId).Returns((Guid?)null);

        var behavior = new IdempotencyBehavior<ObservabilityIdempotentWriteRequest, string>(
            store.Object,
            fingerprint.Object,
            new Mock<IIdempotencyReplayPolicy>().Object,
            new IdempotencyPartitionFactory(tenant.Object),
            executionContext.Object,
            new Mock<IIdempotencyExecutionContextWriter>().Object,
            new Mock<ILogger<IdempotencyBehavior<ObservabilityIdempotentWriteRequest, string>>>().Object,
            new PipelineMetrics());

        await behavior.Handle(
            new ObservabilityIdempotentWriteRequest(Guid.NewGuid()),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        recorder.Names.Should().Contain("idempotency.acquire");
        recorder.Names.Should().Contain("idempotency.complete");
    }

    private static ApplicationTracingBehavior<TRequest, string> CreateTracingBehavior<TRequest>()
        where TRequest : IRequest<string>
    {
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest)))
            .Returns(RequestDescriptorValidator.Create(typeof(TRequest)));
        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(reader => reader.CorrelationId).Returns(Guid.NewGuid());
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(environment => environment.EnvironmentName).Returns("Testing");

        return new ApplicationTracingBehavior<TRequest, string>(
            descriptors.Object,
            new Mock<ILogger<ApplicationTracingBehavior<TRequest, string>>>().Object,
            executionContext.Object,
            hostEnvironment.Object,
            new PipelineMetrics());
    }

    private static AccessControlBehavior<TRequest, string> CreateAccessBehavior<TRequest>(
        IAccessFactsProvider provider)
        where TRequest : IRequest<string>
    {
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest)))
            .Returns(RequestDescriptorValidator.Create(typeof(TRequest)));
        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(reader => reader.Snapshot).Returns(new ExecutionContextSnapshot(
            Guid.NewGuid(), null, null, null,
            RequestDescriptorValidator.Create(typeof(TRequest)).Principal,
            RequestDescriptorValidator.Create(typeof(TRequest)).Scope,
            Guid.NewGuid().ToString("D")));

        return new AccessControlBehavior<TRequest, string>(
            descriptors.Object, executionContext.Object, provider, new AccessPolicyEngine(), new PipelineMetrics());
    }

    private static DataSessionBehavior<TRequest, string> CreateDataSessionBehavior<TRequest>(
        IRequestDataSession session)
        where TRequest : IRequest<string>
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(TRequest));
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest))).Returns(descriptor);
        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(reader => reader.Snapshot).Returns(new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            descriptor.Principal, descriptor.Scope, Guid.NewGuid().ToString("D")));

        return new DataSessionBehavior<TRequest, string>(descriptors.Object, executionContext.Object, session);
    }

    private static RequestContractBehavior<TRequest, string> CreateContractBehavior<TRequest>()
        where TRequest : IRequest<string>
    {
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest)))
            .Returns(RequestDescriptorValidator.Create(typeof(TRequest)));

        return new RequestContractBehavior<TRequest, string>(
            descriptors.Object,
            Array.Empty<IValidator<TRequest>>(),
            new Mock<IIdempotencyExecutionContext>().Object);
    }

    private static ExecutionContextBehavior<TRequest, string> CreateExecutionContextBehavior<TRequest>()
        where TRequest : IRequest<string>
    {
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest)))
            .Returns(RequestDescriptorValidator.Create(typeof(TRequest)));
        var executionContext = new Mock<IExecutionContextAccessor>();
        var tenant = new Mock<ICurrentTenantContext>();
        var credential = new Mock<ICurrentCredentialContext>();

        return new ExecutionContextBehavior<TRequest, string>(
            descriptors.Object,
            executionContext.Object,
            tenant.Object,
            credential.Object,
            new Mock<IResourceLocator>().Object,
            new Mock<ITenantBootstrapStore>().Object);
    }

    private static AccessFacts Facts(bool userExists = false, bool emailVerified = false) => new(
        userExists, emailVerified, false, null, false, null, false, null, null, false, [], false, null, false, null, null);

    private sealed class ActivityRecorder : IDisposable
    {
        private readonly ActivityListener _listener;

        public ActivityRecorder()
        {
            Started = [];
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => Started.Add(activity),
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public List<Activity> Started { get; }

        public IEnumerable<string> Names =>
            Started.Select(activity => activity.OperationName);

        public void Dispose() => _listener.Dispose();
    }
}
