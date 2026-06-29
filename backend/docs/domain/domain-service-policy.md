# Domain Service Policy

Domain services are allowed only for pure business rules that do not naturally
belong to one aggregate and require no IO.

## Use Domain Service For

Use a Domain service or policy when:

- an invariant spans multiple aggregates;
- Application can load the facts, but the rule itself is business logic;
- the result is deterministic from supplied inputs;
- no repository, clock, provider, HTTP, cache, broker, or database access is
  needed.

Notrelix examples:

- `WorkspaceOwnerRules` using supplied active owner count;
- `PageTreeRules` using supplied parent-chain lookup;
- `BlockTreeRules` using supplied parent-chain lookup;
- `BoardItemRules.EnsureNoCycle` using supplied ancestor facts;
- future `ResourceCapabilityPolicy` using a static resource registry;
- future formula syntax/reference policy that validates expression shape but
  does not execute queries.

## Use Application Service Or Handler For

Application owns orchestration:

- loading aggregates and counts;
- permission checks through Governance services;
- current user/workspace context;
- transaction boundaries;
- idempotency request handling;
- calling domain methods;
- mapping DTOs;
- cache invalidation;
- scheduling side effects;
- dispatching integration work.

Application may not invent core business invariants when a Domain rule should
exist. If no Domain rule exists, stop and harden Domain first.

## Use Infrastructure Service For

Infrastructure owns IO and runtime systems:

- EF Core, repositories, DbContext;
- Redis, caches, rate limiting, realtime transport;
- search indexing clients;
- file/object storage;
- SMTP/Resend/email/SMS providers;
- payment provider SDKs;
- n8n/webhook dispatch;
- background workers and queues;
- outbox/idempotency/job lock persistence.

## Anti-Patterns

Forbidden Domain service patterns:

- `IBoardRepository` inside Domain;
- `IClock` injected into an aggregate;
- `HttpClient` or provider clients in Domain;
- `PermissionEvaluator` reading database state inside Domain;
- automation action execution from aggregate methods;
- search indexing from aggregate methods;
- generic "DomainManager" or broad service that hides aggregate ownership.

## Design Checklist

Before adding a Domain service, document:

1. invariant name;
2. bounded context owner;
3. aggregates involved;
4. facts Application must load;
5. pure method signature;
6. exceptions thrown on invalid state;
7. tests that prove valid/invalid cases;
8. why the rule does not belong to a single aggregate.
