# Notrelix Domain Layer Rules — README

This documentation pack defines the **Domain layer rules** for Notrelix. It is designed to be used by coding agents and human developers.

The Domain layer is the core of the system. It owns business language, aggregate invariants, value objects, domain events, lifecycle rules, tenant/account/workspace scoping markers, and domain exceptions. It must remain pure and must not depend on Application, Infrastructure, API, EF Core, MediatR, MassTransit, Redis, HTTP, files, clocks, or external services.

## Current Domain context

The current Domain project is organized by bounded context and shared primitives:

```txt
Notrelix.Domain/
  Accounts/
  Analytics/
  Automation/
  Billing/
  Collaboration/
  Common/
  Documents/
  Governance/
  Identity/
  Integrations/
  SharedKernel/
  WorkManagement/
  Workspaces/
```

`Common/` contains the base primitives used by aggregates and events:

```txt
Common/
  AggregateRoot.cs
  Entity.cs
  AuditableEntity.cs
  SoftDeletableEntity.cs
  DomainEvent.cs
  IDomainEvent.cs
  IDurableDomainEvent.cs
  ILocalDomainEvent.cs
  IWorkspaceScoped.cs
  IAccountScoped.cs
  Guard.cs
  Exceptions/
```

`SharedKernel/` contains cross-context value objects and enums such as `ResourceRef`, `ResourceType`, `Email`, `Money`, `Slug`, `FractionalIndex`, `JsonValue`, `Url`, `SecretRef`, and similar primitives.

## Documents in this pack

| File | Purpose |
|---|---|
| `01-domain-layer-rules.md` | Non-negotiable Domain rules for coding agents. |
| `02-folder-structure-and-boundaries.md` | Where code belongs in Domain and where it must not go. |
| `03-aggregate-root-entity-value-object.md` | Aggregate, entity, value object modeling rules. |
| `04-invariants-and-business-rules.md` | How to encode business rules and guard conditions. |
| `05-domain-events-and-outbox-boundary.md` | Domain event rules and event/outbox boundary. |
| `06-scoping-audit-soft-delete-versioning.md` | Tenant/account/workspace scope, audit, soft delete, version rules. |
| `07-bounded-contexts-and-shared-kernel.md` | Bounded context ownership and shared kernel rules. |
| `08-domain-services-policies-specifications.md` | When to use domain services/specifications/policies. |
| `09-testing-and-architecture-gates.md` | Domain tests and architecture gates. |
| `10-code-review-checklist.md` | Review checklist for Domain PRs. |
| `11-domain-hardening-plan.md` | Remaining weaknesses and hardening plan. |
| `RULE-domain-layer-patch.md` | Short, strict patch for `backend/RULE.md`. |

## One-sentence rule

> Domain code must express business truth and protect invariants. It must not know how persistence, HTTP, messaging, caching, authentication, authorization, background jobs, or external integrations are implemented.
