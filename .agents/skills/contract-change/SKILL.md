# Change Cross-Boundary Contract

This skill is a workflow only. Repository `RULE.md` and canonical engineering/product docs define architecture.

## Procedure

Read system contract/versioning/event/REST/realtime docs and both producer/consumer handbooks. Identify stable logical identity, scope/authz, compatibility and supported old consumers. Prefer additive evolution; breaking change requires migration/deployment order and removal condition. Regenerate artifacts rather than hand editing. Run producer tests, codegen/drift, consumer compile/tests and integration compatibility. Report rollout and exact consumer inventory.

## Stop conditions

Stop and surface an explicit decision when the task would require inventing product semantics, weakening tenant/security guarantees, breaking a public contract without migration, or suppressing a required gate.
