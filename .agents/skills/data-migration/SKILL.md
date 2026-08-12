# Perform Data Migration

This skill is a workflow only. Repository `RULE.md` and canonical engineering/product docs define architecture.

## Procedure

Read backend persistence/migration, delivery change-impact and operations data-recovery docs. Establish old/expanded/target schema and binary compatibility. Use expand/backfill/switch/contract where needed; backfill is tenant-safe, bounded, resumable and idempotent. Assess locks/index/RLS/constraints and whether rollback is actually safe after writes/events. Test upgrade on representative existing data and verify invariants before removing old path.

## Stop conditions

Stop and surface an explicit decision when the task would require inventing product semantics, weakening tenant/security guarantees, breaking a public contract without migration, or suppressing a required gate.
