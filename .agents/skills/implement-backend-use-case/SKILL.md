# Implement Backend Use Case

This skill is a workflow only. Repository `RULE.md` and canonical engineering/product docs define architecture.

## Procedure

Read root/backend AGENTS and the owning product context, then backend vertical-slice/application-pipeline/authz docs. Inventory aggregate, request contract/markers, external facts, authorization/tenant resource, persistence/migration, API/event/realtime consumers and tests. Implement the smallest complete vertical slice; never put business decisions in API/Infrastructure or call `SaveChangesAsync` from a handler when pipeline owns commit. Run focused project tests, architecture and affected API/integration gates. Report contract/schema/event changes and exact evidence.

## Stop conditions

Stop and surface an explicit decision when the task would require inventing product semantics, weakening tenant/security guarantees, breaking a public contract without migration, or suppressing a required gate.
