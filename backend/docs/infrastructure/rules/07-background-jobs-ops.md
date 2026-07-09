# 07 — Background Jobs and Ops Rules

## 1. Background job role

Background jobs handle technical asynchronous work:

- Outbox dispatch.
- Queued jobs.
- Scheduled maintenance.
- Projection rebuilds.
- Cleanup/TTL jobs.
- Import/export workers.

They must not become hidden use case handlers without Application contracts.

## 2. Scope rule

Hosted service is singleton. It must create a scoped service provider per batch/work item.

Correct:

```csharp
await using var scope = _scopeFactory.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
```

Wrong:

```csharp
public Worker(ApplicationDbContext db) // singleton captures scoped DbContext
```

## 3. Tenant/system context rule

Every job must declare scope:

```txt
System job
Account-scoped job
Workspace-scoped job
User-scoped job
```

Before DB access:

- Set system context for system job.
- Set account/workspace context for scoped job.
- Apply RLS if non-system or if DB policy needs session variables.

Clear tenant context in `finally`.

## 4. Transaction rule

If job writes DB:

```txt
Begin transaction
Apply context/RLS
Perform work
SaveChanges
Commit
```

If job performs external side effects, record durable intent first if side effect must not be lost.

## 5. Idempotency rule

Every job must be safe to retry.

Use one of:

- job lock table;
- idempotency key;
- processed event table;
- natural unique constraint;
- outbox state machine.

Never rely on "worker usually runs once".

## 6. Job lock rule

Long-running or scheduled jobs need lock:

```txt
job_name + partition_key
```

Lock must have timeout/heartbeat to recover from crash.

## 7. Outbox dispatcher rule

Outbox dispatcher is special:

- It owns claiming outbox messages.
- It may use system context for infrastructure processing.
- It provides at-least-once dispatch.
- Consumers must remain idempotent.

Do not add business-specific logic to OutboxDispatcher.

## 8. Error handling rule

Background job must:

- Catch top-level exceptions.
- Log with job name and correlation if available.
- Avoid crashing host for recoverable batch errors.
- Respect cancellation token.
- Use retry/backoff where applicable.

## 9. Metrics rule

Long-running workers should emit metrics:

- pending count;
- failed count;
- processed count;
- duration;
- retry count;
- dead-letter count.

## 10. Queue rule

If using in-memory queue, it is development/single-instance only unless explicitly designed for production.

Production durable jobs should use DB queue, message broker, or outbox.

## 11. Testing rule

Required tests:

- cancellation respected;
- retry path;
- failure does not poison entire batch;
- idempotent rerun;
- lock prevents concurrent duplicate job;
- tenant context cleared after job;
- transaction rollback on failure.
