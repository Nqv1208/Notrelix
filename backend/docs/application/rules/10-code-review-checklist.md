# Application Layer Code Review Checklist

Use this checklist for every PR that touches `Notrelix.Application`.

## 1. Folder and naming

- [ ] Use case is under `Features/{BoundedContext}/{Module}/Commands|Queries/{UseCase}`.
- [ ] Namespace matches folder.
- [ ] No new use case under legacy path.
- [ ] No new marker under `Common/CQRS`.
- [ ] Files are placed by responsibility, not convenience.

## 2. Request markers

- [ ] Command/query implements correct `ICommand<T>` or `IQuery<T>`.
- [ ] Mutation implements `ITransactionalRequest`.
- [ ] Workspace/resource/account request implements correct scope marker.
- [ ] Scoped request implements `IRequirePermission`, unless explicitly system-internal.
- [ ] Concurrency-sensitive mutation implements `IExpectedVersionRequest`.
- [ ] Cacheable request uses correct public/authorized cache marker.
- [ ] Anonymous/system/internal markers are not abused.

## 3. Security and tenancy

- [ ] Permission action/resource are correct and specific.
- [ ] Resource-scoped request resolves via `ResourceRef`.
- [ ] Handler does not inject `ICurrentTenantContext` directly.
- [ ] Handler uses `ICurrentRequestContext` when current actor/scope is needed.
- [ ] No raw SQL/IgnoreQueryFilters without whitelist and tests.
- [ ] Cross-tenant behavior is tested for sensitive resource.

## 4. Handler implementation

- [ ] Handler uses bounded context DbContext abstraction, not concrete `ApplicationDbContext`.
- [ ] Handler does not call `SaveChangesAsync`.
- [ ] Handler calls domain methods/factories instead of setting state directly.
- [ ] Handler does not publish bus messages directly.
- [ ] Handler does not send durable side effects directly.
- [ ] Handler returns `Result`/DTO cleanly.

## 5. Transaction/concurrency

- [ ] Mutation is transactional.
- [ ] Expected version is positive and supported.
- [ ] Version mismatch maps to conflict.
- [ ] No concurrency check is silently skipped.
- [ ] Tests cover stale update if applicable.

## 6. Cache

- [ ] Query does not expose raw cache key.
- [ ] `CacheIdentity` includes all response-changing parameters.
- [ ] Cache scope is correct: public/account/workspace/user/permissioned.
- [ ] Permissioned cache uses real `IPermissionVersionProvider` version.
- [ ] Authorized cache cannot bypass permission.

## 7. Events and side effects

- [ ] Cross-context communication uses integration event/outbox.
- [ ] Post-commit side effects are best-effort only.
- [ ] Durable side effects are outbox-backed.
- [ ] Consumer idempotency is handled by selected central mechanism.
- [ ] No manual duplicate check inside individual consumer if central mechanism owns it.

## 8. Tests

- [ ] Validator tests exist if validation is non-trivial.
- [ ] Handler tests cover main success/failure paths.
- [ ] Behavior tests updated if marker/pipeline behavior changed.
- [ ] Architecture tests updated if a new rule is introduced.
- [ ] Integration tests added for tenant/RLS/cache/outbox/concurrency changes.

## 9. Docs/RULE updates

- [ ] `backend/RULE.md` updated if a new rule or folder is introduced.
- [ ] `/docs/backend` or `/docs/application` updated for non-trivial architecture decision.
- [ ] ADR added for major trade-off decisions.

## 10. Final PR question

Before merge, answer:

```txt
Can a coding agent copy this pattern safely for the next similar use case?
```

If not, improve the structure/tests/docs before merging.
