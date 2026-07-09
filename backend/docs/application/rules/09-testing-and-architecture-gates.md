# Testing and Architecture Gates

## 1. Test layers

Use the right test type:

```txt
Domain.Tests          -> aggregate invariants, value objects, domain events
Application.Tests     -> handlers, validators, behaviors, request contracts
Infrastructure.Tests  -> EF mappings, RLS/session, outbox, external adapters
API.Tests             -> endpoint binding/response contracts
Integration.Tests     -> full pipeline, DB, tenant isolation, outbox, cache/security
Architecture.Tests    -> dependency rules, folder rules, marker rules, forbidden dependencies
```

## 2. Architecture tests required

### Layer rules

```txt
Domain must not reference Application/Infrastructure/API.
Application must not reference Infrastructure/API.
Infrastructure must not reference API.
API must not contain business logic.
```

### Folder rules

```txt
New use cases must live under Features/{Context}/{Module}/Commands|Queries/{UseCase}.
No new use case in legacy Features/{Context}/Commands|Queries/{Module} path.
No new request marker under Common/CQRS.
```

### Request marker rules

```txt
Workspace/resource/account scoped requests must implement IRequirePermission unless system-internal whitelist.
Mutation commands must implement ITransactionalRequest.
IExpectedVersionRequest must expose supported ResourceRef and positive version.
Public cacheable request must not be tenant-scoped or permissioned.
Authorized cacheable request must not expose raw cache key.
```

### Handler boundary rules

Handlers must not inject:

```txt
ApplicationDbContext concrete
ICurrentTenantContext
HttpContext
IPublishEndpoint / IBus
External concrete clients
Raw Redis client
```

Handlers should use:

```txt
Bounded context DbContext abstraction
ICurrentRequestContext
Application service abstraction
```

### Side-effect rules

Handlers must not directly:

```txt
Send email
Publish bus message
Send webhook
Call external API for durable side effect
```

## 3. Security tests

Minimum matrix:

```txt
User A workspace A cannot read workspace B resource.
User A workspace A cannot update workspace B resource.
List query excludes workspace B data.
Resource-scoped request resolves correct workspace before permission.
Denied user does not receive cached response.
```

Entities:

```txt
Board
BoardItem
BoardField
Comment
Page
ShareLink
Search projection if active
```

## 4. Pipeline tests

Must prove:

```txt
Authorization before authorized cache.
Concurrency inside DB/RLS scope.
SaveChanges before commit.
Post-commit flush after commit.
Rollback clears post-commit queue.
```

## 5. Cache tests

Must prove:

```txt
Public cache rejects tenant-scoped request.
Workspace cache key differs by workspace/account.
User cache key differs by user.
Permissioned cache key differs by permission version.
Permission version changes when permission state changes.
Denied user cannot hit authorized cache populated by allowed user.
```

## 6. Concurrency tests

Must prove:

```txt
Expected version match -> success.
Mismatch -> conflict.
Unsupported resource -> fail fast.
Missing version/current resource -> fail fast/not found.
DbUpdateConcurrencyException -> conflict mapping.
```

## 7. Outbox/consumer tests

Must prove:

```txt
Outbox persists only on committed transaction.
Dispatcher retries failed publish.
Duplicate consumer delivery is idempotent.
Concurrent duplicate delivery does not duplicate side effect.
```

## 8. CI gate recommendation

CI should run:

```bash
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test backend/tests/Notrelix.Domain.Tests
dotnet test backend/tests/Notrelix.Application.Tests
dotnet test backend/tests/Notrelix.Architecture.Tests
dotnet test backend/tests/Notrelix.Infrastructure.Tests
dotnet test backend/tests/Notrelix.Integration.Tests
```

Adjust command paths to actual solution layout.
