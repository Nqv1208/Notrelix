namespace Notrelix.Architecture.Tests.PlatformMessaging;

/// <summary>
/// TAC-PF-008 — Platform & Foundation reference pack completeness: the actual
/// delivery chain used by all production events is already proven by
/// dedicated owners (listed in the manifest below). This gate freezes that
/// ownership map so a mechanism cannot disappear silently and cannot be
/// re-implemented as a parallel teaching broker.
/// </summary>
public class PlatformReferenceCompletenessTests
{
    /// <summary>
    /// Mechanism → owning proof suite. Every entry must reference real test
    /// classes; delete the entry only when the mechanism itself is retired
    /// through its canonical owner.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> MechanismProofs =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            // Producer commit → durable outbox record (atomic enrollment).
            ["outbox-enrollment"] =
            [
                "Notrelix.Integration.Tests.Data.OutboxAtomicityTests",
                "Notrelix.Integration.Tests.Data.DomainEventInterceptorTests",
            ],

            // Envelope/message identity (message id, name, version, correlation).
            ["envelope-identity"] =
            [
                "Notrelix.Platform.Tests.Messaging.Contracts.DefaultTopicResolverTests",
                "Notrelix.Integration.Tests.Data.OutboxDispatchContractTests",
            ],

            // Consumer delivery state / dedup (duplicate delivery is safe).
            ["consumer-dedup"] =
            [
                "Notrelix.Integration.Tests.Messaging.DeduplicationConsumeFilterIntegrationTests",
                "Notrelix.Integration.Tests.Messaging.DeduplicationConsumeFilterFullIntegrationTests",
            ],

            // Technical retry & terminal failure (poison/dead-letter owner).
            ["retry-and-poison"] =
            [
                "Notrelix.Platform.Tests.Messaging.Reliability.PoisonDetectorTests",
                "Notrelix.Platform.Tests.Messaging.Consumers.ConsumerHostDeliveryContractTests",
            ],

            // Broker/provider SDK confinement and semantic purity.
            ["platform-boundary"] =
            [
                "Notrelix.Architecture.Tests.InfrastructureLayer.PlatformBoundaryTests",
                "Notrelix.Architecture.Tests.ApplicationLayer.ApplicationTransportBoundaryTests",
            ],
        };

    [Fact]
    public void MechanismProofs_CoverTheCanonicalDeliveryChain()
    {
        MechanismProofs.Keys.Should().BeEquivalentTo(
        [
            "outbox-enrollment",
            "envelope-identity",
            "consumer-dedup",
            "retry-and-poison",
            "platform-boundary",
        ],
        "the PF pack owns the delivery mechanism map — adding a mechanism requires adding its proof owner");
    }

    [Fact]
    public void MechanismProofs_ContainNoWildcardOrEmptyOwners()
    {
        foreach (var (mechanism, owners) in MechanismProofs)
        {
            owners.Should().NotBeEmpty($"{mechanism} must name at least one proof suite");
            owners.Should().OnlyContain(owner =>
                    owner.StartsWith("Notrelix.", StringComparison.Ordinal) &&
                    owner.Contains(".Tests.", StringComparison.Ordinal),
                $"{mechanism}: owners must be real test classes");
        }
    }
}
