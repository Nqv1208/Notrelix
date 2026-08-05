# Application Foundation

> Stable. Changes require architecture review.

## Execution model

- 19 MediatR pipeline behaviors in frozen registration order
- `RequestExecutionClassifier` derives profile from marker interfaces
- `RequestContractGuardBehavior` validates 9 forbidden marker combinations
- `IRequestDataSession` port: Application owns intent, Infrastructure owns mechanics

## Transaction contract

- One `IRequestDataSession.ExecuteAsync` per request
- Transactional: open → RLS → handler → SaveChanges → commit
- ReadOnly: open → RLS → handler → no SaveChanges → commit
- None: passthrough, no database

## Idempotency contract

- Scoped identity: Operation + Scope + Key + RequestHash
- `BeginAsync` / `CompleteAsync` pattern (no lock/cache)
- Completion participates in the same transaction as business state
- PayloadMismatch detected via SHA-256 request hash

## Concurrency contract

- `IExpectedVersionRequest` with `ExpectedVersion`
- Early pre-read for fast feedback (optional)
- EF concurrency token is authoritative
- `PreconditionFailedException` (code: `common.precondition-failed`)

## Error model

- `ApplicationError(Code, Message, Type, Target)`
- `ApplicationErrorType`: Validation, NotFound, Conflict, PreconditionFailed, BusinessRule
- `Result<T>.TypedErrors` (backward compatible with `string[] Errors`)
- Error codes: lowercase dotted kebab (`context.error-name`)

## Handler conventions

- Handlers do not call SaveChanges
- Handlers do not publish broker messages directly
- Handlers do not access HttpContext
- DTOs do not expose Domain entities

## Integration events

- Domain Event → versioned Integration Event via `IIntegrationEventMapper`
- `IContractRegistry` with name, version, classification
- Unmapped events are internal
