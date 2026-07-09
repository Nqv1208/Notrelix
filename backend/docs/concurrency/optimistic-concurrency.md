# Optimistic Concurrency

## Overview

Optimistic concurrency is enforced at the application pipeline level via `ConcurrencyBehavior`. The behavior intercepts any request implementing `IExpectedVersionRequest` and verifies that the stored resource version matches the client's expected version before allowing the handler to execute.

## Behavior

```
Request (IExpectedVersionRequest) → ConcurrencyBehavior → Handler
                                            │
                                    ResourceVersionReader
                                    (raw SQL SELECT version)
```

1. Behavior detects `IExpectedVersionRequest` marker
2. Validates `ExpectedVersion > 0` (fail-fast)
3. Calls `IResourceVersionReader.GetVersionAsync(resource, ct)` to get current version
4. If resource not found (`currentVersion == null`) → throws `NotFoundException`
5. If version mismatch → throws `ConflictException`
6. If version match → proceeds to handler
7. On `NotSupportedException` (unsupported resource type) → throws immediately (fail-fast)

## Fail-Fast Rules

| Condition | Exception | Rationale |
|---|---|---|
| `ExpectedVersion <= 0` | `ValidationException` | Misconfiguration — request implements marker but provides negative/zero version |
| `currentVersion == null` | `NotFoundException` | Resource was authorized but no longer exists |
| Version mismatch | `ConflictException` | Stale data — client must re-read and retry |
| Unsupported resource type | `NotSupportedException` | `ResourceVersionReader` cannot process this resource type |

Concurrency checks are never silently skipped.

## ResourceVersionReader

Maps `ResourceType` to schema/table:

| ResourceType | Table |
|---|---|
| Board | `work.boards` |
| BoardItem | `work.board_items` |
| BoardField | `work.board_fields` |
| Page | `docs.pages` |
| Form | `work.forms` |
| ... | ... |

Uses raw SQL `SELECT version FROM {schema}.{table} WHERE id = @id` to avoid EF query plan cache issues.

## Version Column

All versioned aggregates use an `xmin` (PostgreSQL snapshot visibility) or an explicit `version` column mapped with `.IsConcurrencyToken()`. EF's `DbUpdateConcurrencyException` is mapped to `ConflictException` in `ExceptionMappingBehavior`.

## Rule

Any request implementing `IExpectedVersionRequest` must provide a positive `ExpectedVersion` and a supported `ResourceRef`. The system must fail fast if the resource version cannot be verified. Concurrency checks must never be silently skipped.
