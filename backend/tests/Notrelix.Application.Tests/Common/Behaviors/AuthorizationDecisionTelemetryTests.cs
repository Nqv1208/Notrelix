using System.Diagnostics.Metrics;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Common.Behaviors;

/// <summary>
/// CERT-OBS-001 / BE-OBS-001 — the pipeline access-control stage must emit a
/// real, code-bounded authorization decision signal.
///
/// The protected properties are:
///
/// <list type="number">
/// <item>every authorization decision is recorded exactly once on the
/// authorization_decisions counter, labelled only by the code-bounded
/// decision category;</item>
/// <item>a denial is logged at Warning so denials are separable from generic
/// pipeline failures;</item>
/// <item>the denial log carries structural identifiers only — never the
/// decision message or any policy payload.</item>
/// </list>
/// </summary>
/// <summary>
/// <para>
/// Serializes only the tests that probe the pipeline Meter.
/// </para>
/// <para>
/// The shared resource is process-global: <see cref="MeterListener"/> observes
/// every <c>authorization_decisions</c> instrument published on
/// <see cref="PipelineMetrics.MeterName"/>, not only the instrument created by
/// the test that installed the listener. While a probe is active, a
/// concurrently running test that records the same counter is captured by it
/// and breaks the "recorded exactly once" assertion. Do not widen this to the
/// rest of the suite: unrelated tests share no state and stay parallel.
/// </para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PipelineMeterProbeCollection
{
    public const string Name = "PipelineMeterProbe";
}

[Collection(PipelineMeterProbeCollection.Name)]
public sealed class AuthorizationDecisionTelemetryTests
{
    public sealed record AnonymousRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest, INoDataRequest;

    public sealed record PermissionedRequest
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, INoDataRequest, IRequirePermission
    {
        public Guid WorkspaceId => Guid.Parse("11111111-1111-1111-1111-111111111111");

        public PermissionAction Action => PermissionAction.InviteMember;

        public ResourceRef Resource =>
            ResourceRef.Create(
                ResourceKind.Create("workspaces.workspace"),
                WorkspaceId,
                Guid.Parse("22222222-2222-2222-2222-222222222222"));
    }

    [Fact]
    public async Task Allowed_decision_records_counter_and_emits_no_denial_log()
    {
        using var measurements = new CounterProbe();
        var logger = new LoggerProbe<AccessControlBehavior<PermissionedRequest, string>>();

        var behavior = CreateBehavior<PermissionedRequest>(AccessDecisionKind.Allowed, logger.Logger);

        var result = await behavior.Handle(
            new PermissionedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        measurements.Measurements.Should().ContainSingle();
        measurements.Measurements[0].Value.Should().Be(1);
        measurements.Measurements[0].Tags.Should().Contain(
            new KeyValuePair<string, object?>("decision.kind", "Allowed"));
        logger.Entries.Should().BeEmpty("an allowed decision is not a denial signal");
    }

    [Fact]
    public async Task Forbidden_decision_records_counter_and_logs_denial_at_warning()
    {
        using var measurements = new CounterProbe();
        var logger = new LoggerProbe<AccessControlBehavior<PermissionedRequest, string>>();

        var behavior = CreateBehavior<PermissionedRequest>(
            AccessDecisionKind.Forbidden, logger.Logger, "You do not have permission to invite members.");

        var act = () => behavior.Handle(
            new PermissionedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();

        measurements.Measurements.Should().ContainSingle();
        measurements.Measurements[0].Tags.Should().Contain(
            new KeyValuePair<string, object?>("decision.kind", "Forbidden"));

        logger.Entries.Should().ContainSingle();
        var entry = logger.Entries[0];
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Rendered.Should().Contain("Forbidden");
        entry.Rendered.Should().Contain("PermissionedRequest");
        entry.Rendered.Should().Contain("InviteMember");
        entry.Rendered.Should().Contain("workspaces.workspace");
    }

    [Fact]
    public async Task Denial_log_never_carries_the_decision_message_or_policy_payload()
    {
        const string sensitivePolicyMessage =
            "Member 22222222-2222-2222-2222-222222222222 lacks InviteMember because plan=enterprise-secret";

        using var measurements = new CounterProbe();
        var logger = new LoggerProbe<AccessControlBehavior<PermissionedRequest, string>>();

        var behavior = CreateBehavior<PermissionedRequest>(
            AccessDecisionKind.Forbidden, logger.Logger, sensitivePolicyMessage);

        var act = () => behavior.Handle(
            new PermissionedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        // The message reaches the thrown exception (the public contract) but
        // must never be promoted into telemetry.
        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage($"*{sensitivePolicyMessage}*");

        var entry = logger.Entries.Should().ContainSingle().Subject;
        entry.Rendered.Should().NotContain(sensitivePolicyMessage);
        entry.Rendered.Should().NotContain("plan=enterprise-secret");
        entry.Rendered.Should().NotContain("22222222-2222-2222-2222-222222222222");

        measurements.Measurements.Should().ContainSingle();
        measurements.Measurements[0].Tags.Select(tag => tag.Key).Should().BeEquivalentTo(["decision.kind"]);
    }

    [Theory]
    [InlineData(AccessDecisionKind.Allowed)]
    [InlineData(AccessDecisionKind.Unauthorized)]
    [InlineData(AccessDecisionKind.Forbidden)]
    [InlineData(AccessDecisionKind.NotFound)]
    [InlineData(AccessDecisionKind.SecurityMisconfiguration)]
    public async Task Every_decision_kind_is_recorded_exactly_once(AccessDecisionKind kind)
    {
        using var measurements = new CounterProbe();
        var logger = new LoggerProbe<AccessControlBehavior<AnonymousRequest, string>>();

        var behavior = CreateBehavior<AnonymousRequest>(kind, logger.Logger);

        try
        {
            await behavior.Handle(new AnonymousRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        }
        catch (Exception ex) when (ex is UnauthorizedException or NotFoundException
                                      or ForbiddenException or SecurityMisconfigurationException)
        {
            kind.Should().NotBe(AccessDecisionKind.Allowed);
        }

        measurements.Measurements.Should().ContainSingle(
            "the decision must be recorded at the decision point, before exception translation");
        measurements.Measurements[0].Value.Should().Be(1);
        measurements.Measurements[0].Tags.Should().Contain(
            new KeyValuePair<string, object?>("decision.kind", kind.ToString()));

        var expectedDenials = kind == AccessDecisionKind.Allowed ? 0 : 1;
        logger.Entries.Should().HaveCount(expectedDenials);
        logger.Entries.Should().AllSatisfy(entry => entry.Level.Should().Be(LogLevel.Warning));
    }

    [Fact]
    public void Counter_is_published_under_the_canonical_pipeline_meter()
    {
        using var metrics = new PipelineMetrics();

        metrics.AuthorizationDecisions.Name.Should().Be("authorization_decisions");
        metrics.AuthorizationDecisions.Meter.Name.Should().Be(PipelineMetrics.MeterName);
    }

    private static AccessControlBehavior<TRequest, string> CreateBehavior<TRequest>(
        AccessDecisionKind kind,
        ILogger<AccessControlBehavior<TRequest, string>> logger,
        string? message = null)
        where TRequest : IRequest<string>
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(TRequest));
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest))).Returns(descriptor);

        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(reader => reader.Snapshot).Returns(new ExecutionContextSnapshot(
            descriptor.Principal == ApplicationPrincipalKind.Anonymous ? null : Guid.NewGuid(),
            null, null, null, descriptor.Principal, descriptor.Scope, Guid.NewGuid().ToString("D")));

        var policy = new Mock<IAccessPolicyEvaluator>();
        policy.Setup(evaluator => evaluator.Evaluate(
                It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(),
                It.IsAny<AccessFacts>(), It.IsAny<object>()))
            .Returns(new AccessDecision(kind, message));

        return new AccessControlBehavior<TRequest, string>(
            descriptors.Object,
            executionContext.Object,
            new Mock<IAccessFactsProvider>().Object,
            policy.Object,
            new PipelineMetrics(),
            logger);
    }

    private sealed record Measurement(long Value, IReadOnlyList<KeyValuePair<string, object?>> Tags);

    /// <summary>Captures authorization_decisions measurements from the pipeline meter.</summary>
    private sealed class CounterProbe : IDisposable
    {
        private readonly MeterListener _listener = new();

        public List<Measurement> Measurements { get; } = [];

        public CounterProbe()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == PipelineMetrics.MeterName
                    && instrument.Name == "authorization_decisions")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
                Measurements.Add(new Measurement(measurement, tags.ToArray())));
            _listener.Start();
        }

        public void Dispose() => _listener.Dispose();
    }

    private sealed record LogEntry(LogLevel Level, string Rendered);

    private sealed class LoggerProbe<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public ILogger<T> Logger { get; }

        public LoggerProbe()
        {
            var mock = new Mock<ILogger<T>>();
            mock.Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var level = (LogLevel)invocation.Arguments[0];

                    // FormattedLogValues.ToString() is the message the logging
                    // infrastructure actually renders, so the denial assertion
                    // checks real emitted output rather than the template.
                    var rendered = invocation.Arguments[2]?.ToString() ?? string.Empty;

                    Entries.Add(new LogEntry(level, rendered));
                }));

            Logger = mock.Object;
        }
    }
}