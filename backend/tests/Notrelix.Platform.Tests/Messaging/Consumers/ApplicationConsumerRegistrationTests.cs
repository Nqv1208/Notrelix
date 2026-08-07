using System.Collections.Concurrent;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Requests;
using Notrelix.Platform.Messaging.Consumers;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Consumers;

/// <summary>
/// FZ-IDEM-06 (spec 3.4): typed Application consumers create a fresh DI scope per
/// delivery, bind the execution key from EventEnvelope.Id with source Message, and
/// dispatch the command through ISender — the same Application idempotency behavior
/// the HTTP path uses.
/// </summary>
public sealed class ApplicationConsumerRegistrationTests
{
    private sealed record TestCommand(Guid EnvelopeId) : ICommand, IIdempotentRequest;

    private sealed class ScopeProbe;

    private sealed class CapturedDispatch
    {
        public required string Key { get; init; }
        public required IdempotencyExecutionSource Source { get; init; }
        public required object Request { get; init; }
        public required ScopeProbe ScopeProbe { get; init; }
    }

    private static (ServiceProvider Provider, ConcurrentQueue<CapturedDispatch> Dispatches) BuildHost()
    {
        var dispatches = new ConcurrentQueue<CapturedDispatch>();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<Notrelix.Platform.Messaging.Observability.MessagingMetrics>(_ =>
            new Notrelix.Platform.Messaging.Observability.MessagingMetrics("test"));
        services.AddSingleton<Notrelix.Platform.Messaging.Observability.IDiagnosticEventPublisher>(
            Mock.Of<Notrelix.Platform.Messaging.Observability.IDiagnosticEventPublisher>());

        services.AddScoped<IIdempotencyExecutionContext, IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            (IIdempotencyExecutionContextWriter)sp.GetRequiredService<IIdempotencyExecutionContext>());
        services.AddScoped<ScopeProbe>();

        services.AddScoped<ISender>(sp =>
        {
            var context = sp.GetRequiredService<IIdempotencyExecutionContext>();
            var probe = sp.GetRequiredService<ScopeProbe>();
            var mock = new Mock<ISender>();
            mock.Setup(s => s.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest<Unit>, CancellationToken>((request, _) =>
                    dispatches.Enqueue(new CapturedDispatch
                    {
                        Key = context.RequireKey(),
                        Source = context.Source,
                        Request = request,
                        ScopeProbe = probe,
                    }))
                .ReturnsAsync(Unit.Value);
            return mock.Object;
        });

        services.AddApplicationConsumer<TestCommand, Unit>(
            "test.application.event",
            envelope => new TestCommand(envelope.Id));

        return (services.BuildServiceProvider(), dispatches);
    }

    private static EventEnvelope BuildEnvelope(Guid id) => new()
    {
        Id = id,
        EventName = "test.application.event",
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = ReadOnlyMemory<byte>.Empty,
        ContentType = "application/json",
    };

    [Fact]
    public async Task Dispatch_BindsEnvelopeIdKey_AndSendsCommandThroughSender()
    {
        var (provider, dispatches) = BuildHost();
        await using var _ = provider;

        var envelope = BuildEnvelope(Guid.NewGuid());
        var host = provider.GetRequiredService<IConsumerHost>();

        await host.DispatchAsync(envelope);

        dispatches.Should().ContainSingle();
        var dispatch = dispatches.Single();
        dispatch.Key.Should().Be(envelope.Id.ToString("N"),
            "the message execution key is the EventEnvelope id in N format");
        dispatch.Source.Should().Be(IdempotencyExecutionSource.Message);
        dispatch.Request.Should().Be(new TestCommand(envelope.Id));
    }

    [Fact]
    public async Task EachDelivery_UsesAFreshScope()
    {
        var (provider, dispatches) = BuildHost();
        await using var _ = provider;

        var host = provider.GetRequiredService<IConsumerHost>();
        await host.DispatchAsync(BuildEnvelope(Guid.NewGuid()));
        await host.DispatchAsync(BuildEnvelope(Guid.NewGuid()));

        dispatches.Should().HaveCount(2);
        var probes = dispatches.Select(d => d.ScopeProbe).ToList();
        probes[0].Should().NotBeSameAs(probes[1],
            "every delivery must create its own DI scope");
        dispatches.Select(d => d.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Registration_IsAppliedToTheConsumerHost_FromDiConfiguration()
    {
        var (provider, _) = BuildHost();
        await using var _ = provider;

        var host = provider.GetRequiredService<IConsumerHost>();

        host.GetRegistrations()
            .Should().Contain(r => r.EventName == "test.application.event",
                "DI-configured registrations must be applied to the host");
    }
}
