# Perform Architecture Review

This skill is a workflow only. Repository `RULE.md` and canonical engineering/product docs define architecture.

## Procedure

Resolve product owner and canonical rules first. Inspect dependency graph, public contracts, persistence/event/realtime boundaries and current source/tests/gates. Classify discrepancies as accepted evolution, transition/exception, stale docs or regression. Score/findings are secondary to evidence. Every blocking finding names rule ID, exact path/consumer, failure mode, required design and proof. Do not propose large rewrites without showing why a smaller boundary-preserving repair is insufficient.

## Stop conditions

Stop and surface an explicit decision when the task would require inventing product semantics, weakening tenant/security guarantees, breaking a public contract without migration, or suppressing a required gate.
