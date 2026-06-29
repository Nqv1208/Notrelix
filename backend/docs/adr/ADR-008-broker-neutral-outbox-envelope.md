# ADR-008: Broker-Neutral Outbox Envelope

**Date:** 2026-06-29
**Status:** Accepted
**Deciders:** Tech Lead

## Context

The Notrelix outbox currently stores integration events with metadata sufficient for MassTransit InMemory transport. When the system migrates to RabbitMQ or Kafka, the outbox must provide transport-specific routing information without requiring a full schema rewrite.

Current `OutboxMessage` fields:
- EventId, SourceEventId, MessageName, SchemaVersion, MessageType
- WorkspaceId, ActorUserId, CorrelationId, CausationId
- PayloadJson, Status, RetryCount, NextAttemptAt, etc.

Missing fields needed for broker routing:
- Topic (Kafka topic, RabbitMQ exchange)
- PartitionKey (Kafka partition assignment)
- RoutingKey (RabbitMQ routing key)
- HeadersJson (transport-specific metadata)
- PayloadContentType (serialization format)

## Decision

1. **Do not migrate OutboxMessage schema now.** The current schema is sufficient for InMemory/MassTransit.

2. **Every new IntegrationEvent must have stable `MessageName` + `SchemaVersion`.**
   - Convention: `{context}.{aggregate}.{action}` (e.g., `identity.user-registered`)
   - Version via `SchemaVersion` property, NOT embedded in `MessageName`
   - Breaking changes create v2 events, never mutate v1

3. **When RabbitMQ/Kafka is implemented, OutboxMessage will add:**
   ```sql
   topic VARCHAR(255) NOT NULL DEFAULT '',
   partition_key VARCHAR(255) NULL,
   routing_key VARCHAR(255) NULL,
   headers_json TEXT NULL,
   payload_content_type VARCHAR(100) NOT NULL DEFAULT 'application/json'
   ```

4. **`IMessageTopologyResolver` interface will be created in Application layer:**
   ```csharp
   public interface IMessageTopologyResolver
   {
       MessageTopology Resolve(IIntegrationEvent integrationEvent);
   }

   public sealed record MessageTopology(
       string Topic,
       string? PartitionKey,
       string? RoutingKey);
   ```

5. **Topic convention:** One topic per bounded context initially.
   ```
   notrelix.identity.events.v1
   notrelix.workspaces.events.v1
   notrelix.billing.events.v1
   notrelix.work.events.v1
   notrelix.collaboration.events.v1
   ```

6. **PartitionKey convention:** Use `WorkspaceId` for workspace-scoped events, `UserId` for identity events. This ensures ordering per workspace/user within a partition.

7. **Do not put MassTransit types in Application layer.**
   - Application knows `IIntegrationEvent`, `IMessageTopologyResolver`
   - Infrastructure implements transport-specific publishers/consumers
   - Consumer adapters live in `Infrastructure/Messaging/Consumers/{Transport}/`

## Rejected Alternatives

- **Add all columns now:** Premature. Schema migration has cost; do it when RabbitMQ/Kafka is actually implemented.
- **No topology info:** Impossible. Kafka requires topic + partition key for ordering guarantees.
- **Single global topic:** Violates isolation. One topic per BC is simpler to operate and reason about.

## Consequences

- Every integration event from now on has a stable, versioned `MessageName`
- When RabbitMQ adapter is added: OutboxMessage migration adds topology columns, `IMessageTopologyResolver` provides routing
- When Kafka adapter is added: Same migration, plus partition key logic for ordering
- Existing InMemory transport ignores topology fields (defaults work)
