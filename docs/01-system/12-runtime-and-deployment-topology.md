---
title: "Runtime and Deployment Topology"
document_class: context
normative: true
owner: infrastructure
maturity: STABILIZING
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Runtime and Deployment Topology

## Logical topology

```text
web/mobile clients
   ↓ HTTPS/realtime
API/backend deployment
   ├─ PostgreSQL
   ├─ Redis
   ├─ durable messaging/outbox consumers
   ├─ object storage
   └─ external providers

marketing host may deploy independently while sharing brand/design assets/contracts as approved.
```

## SYS-DEP-001 — Environment configuration is externalized

Secrets/endpoints/provider credentials and environment-specific capacity are configuration/deployment concerns, not committed product logic.

## SYS-DEP-002 — Deployments respect compatibility windows

Database/contract/event changes must support the planned rollout order. A deployment artifact is not valid merely because it builds independently.

## Health/readiness

Startup/readiness checks should distinguish mandatory dependencies from degradable optional capabilities. Do not make cache/provider outages silently corrupt behavior; either degrade explicitly or fail the affected capability.


## Process roles

The logical backend deployment can include API request handling plus background consumers/workers even when packaged from the same solution. Operational topology must make worker concurrency, queue/outbox ownership and health observable; one executable packaging choice does not erase transaction/consumer boundaries.

Frontend hosts can deploy independently. Contract/version compatibility therefore assumes a period where an older browser/mobile bundle can talk to a newer backend. Marketing deployment is independent of authenticated product runtime unless a future approved topology changes that.

## SYS-DEP-003 — Health is not correctness bypass

Readiness/liveness checks may remove an unhealthy instance from traffic, but application code must still handle dependency failure with the documented retry/degrade semantics. Marking a dependency “optional” cannot authorize stale authorization data or cross-tenant cache fallback.

## Deployment evidence

Exact provider/orchestrator/region/resource counts belong to infrastructure-as-code and environment configuration. Documentation records the compatibility/failure invariants that remain true if that topology changes.
