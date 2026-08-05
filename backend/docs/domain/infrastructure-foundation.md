# Infrastructure Foundation

> Stable. Changes require architecture review.

## Transaction model

- `EfRequestDataSession`: owns EF transaction, RLS, SaveChanges
- One SaveChanges per transactional request
- Domain Events cleared only after successful SaveChanges
- Failed SaveChanges: events retained, generated outbox entries detached

## Outbox delivery

- At-least-once external delivery
- Short claim transaction (FOR UPDATE SKIP LOCKED) → commit lease
- Publish outside database transaction (no held locks during broker I/O)
- Short completion/failure transaction
- Expired lease can be reclaimed

## Consumer deduplication

- Identity: ConsumerName + MessageId
- Inbox record + business mutation commit atomically
- `IDateTimeProvider` injected (no ambient `DateTimeOffset.UtcNow`)

## Realtime channels

- Tenant-qualified: `workspace:{workspaceId}:board:{boardId}`
- Subscription authorization verifies workspace membership
- `RealtimeEnvelope<T>` with ResourceVersion for stale detection

## Cache

- `CacheKeyFactory` in Application: `notrelix:v{N}:{env}:{scope}:{namespace}:{key}`
- Post-commit invalidation only
- Authorized cache keys include authorization partition

## Provider exception translation

- Infrastructure translates provider exceptions to Application exceptions
- API does not reference Npgsql or EF exception types

## Options validation

- All provider options use `ValidateOnStart`
- Custom validators: OAuth, DataProtection, CacheKey, RLS

## Critical ports

- `IRequestDataSession` → `EfRequestDataSession`
- `IRlsSessionContext` → `RlsSessionContext`
- `IApplicationDbContext` → `ApplicationDbContext`
- `IDateTimeProvider` → `DateTimeProvider`
- `IIdempotencyStore` → `DevNullIdempotencyStore` / database-backed
