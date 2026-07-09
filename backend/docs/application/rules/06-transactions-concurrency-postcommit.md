# Transactions, Optimistic Concurrency, and Post-Commit Rules

## 1. Transaction boundary

Mutation commands must implement:

```csharp
ITransactionalRequest
```

The handler does not call `SaveChangesAsync`.

`DbRequestScopeBehavior` owns:

```txt
Open transaction
Apply RLS session
Call handler
Save changes
Commit or rollback
```

## 2. Handler rule

Bad:

```csharp
await _context.SaveChangesAsync(cancellationToken);
```

Good:

```csharp
_context.Boards.Add(board);
return Result<BoardDto>.Success(dto);
```

Pipeline commits.

## 3. Optimistic concurrency

Use `IExpectedVersionRequest` for updates where user edits stale resource.

Rules:

```txt
ExpectedVersion must be positive.
ResourceRef must be supported by IResourceVersionReader.
ConcurrencyBehavior must fail fast if version cannot be verified.
Version mismatch returns conflict.
EF concurrency token remains race protection at SaveChanges.
```

Do not silently skip concurrency check.

Bad behavior:

```txt
ExpectedVersion = 0 -> continue
Unsupported resource type -> log warning and continue
Current version null -> continue
```

Correct behavior:

```txt
ExpectedVersion <= 0 -> invalid request/configuration
Unsupported resource type -> security/configuration failure
Current version null -> not found or cannot verify conflict
Version mismatch -> conflict
```

## 4. Post-commit side effects

Post-commit means:

```txt
DB commit succeeded first.
Only then side effect runs.
```

Use post-commit for best-effort side effects:

```txt
Realtime publish
Cache invalidation if enabled
Non-critical notification enqueue
Activity projection if safe
```

Use outbox for durable side effects:

```txt
Integration event
Webhook
Billing event
External sync
Email that must not be lost
```

## 5. Post-commit failure handling

If post-commit action fails:

```txt
Log error
Continue next action
Clear queue at the end
Do not rollback already committed transaction
```

If handler or SaveChanges fails:

```txt
Rollback transaction
Clear post-commit queue
Do not flush
```

## 6. Tests required

Pipeline tests must prove:

```txt
Handler success -> SaveChanges -> Commit -> PostCommit Flush
Handler throws -> Rollback -> No flush
SaveChanges throws -> Rollback -> No flush
Post-commit action throws -> other actions continue -> queue clears
```

Concurrency tests must prove:

```txt
Expected version match -> handler runs
Expected version mismatch -> conflict
Unsupported resource type -> fail fast
Missing current version -> fail fast/not found
ExpectedVersion <= 0 -> fail fast
DbUpdateConcurrencyException -> conflict response
```
