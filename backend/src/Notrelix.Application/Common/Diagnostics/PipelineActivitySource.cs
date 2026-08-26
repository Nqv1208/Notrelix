using System.Diagnostics;

namespace Notrelix.Application.Common.Diagnostics;

/// <summary>
/// Stage-level activity source for the frozen seven-stage execution pipeline.
/// The root request activity is emitted by <see cref="Behaviors.ApplicationTracingBehavior{TRequest,TResponse}"/>;
/// nested stage activities (request.contract, context.resolve, access.facts,
/// access.evaluate, idempotency, data_session) are started by their owning behaviors.
/// </summary>
public static class PipelineActivitySource
{
    public const string SourceName = "Notrelix.Application.Pipeline";

    public static readonly ActivitySource Instance = new(SourceName, "1.0.0");
}
