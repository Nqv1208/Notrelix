# Freeze / Foundation Certification

This skill is a workflow only. Repository `RULE.md` and canonical engineering/product docs define architecture.

## Procedure

Define the exact SHA and freeze scope. Run all required quality, architecture, core/platform/API/integration/frontend/contract/build gates with non-zero execution evidence. Verify no approved critical exception remains unresolved for the frozen surface, generated/migration artifacts are clean and documentation matches the certified architecture. Record exact commands/jobs, SHA and failures. A previous green SHA or skipped/empty suite cannot certify the current revision.

## Stop conditions

Stop and surface an explicit decision when the task would require inventing product semantics, weakening tenant/security guarantees, breaking a public contract without migration, or suppressing a required gate.
