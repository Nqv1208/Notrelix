# Add or Change Domain Capability

This skill is a workflow only. Repository `RULE.md` and canonical engineering/product docs define architecture.

## Procedure

Read Domain AGENTS, Domain Modeling, Shared Kernel and owning context. Identify aggregate consistency boundary and supplied external facts. Specify success/rejection/no-op/version/events before code. Mutations validate before commit; rejection leaves state/audit/version/events unchanged. Do not introduce repository/provider callbacks or typed IDs/base abstractions mechanically. Add behavior tests for success, rejection atomicity, no-op, lifecycle, version and events; run architecture and affected outward consumers if the public Domain contract changed.

## Stop conditions

Stop and surface an explicit decision when the task would require inventing product semantics, weakening tenant/security guarantees, breaking a public contract without migration, or suppressing a required gate.
