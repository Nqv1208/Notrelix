# Add Frontend Capability

This skill is a workflow only. Repository `RULE.md` and canonical engineering/product docs define architecture.

## Procedure

Read frontend/capability AGENTS, owning product context and frontend capability playbook. Choose product vs feature ownership and only needed core/state/web/mobile slices. Use generated contracts; define scoped query keys, mutation patch/invalidation and realtime convergence; keep app routes as composition only. Cover loading/error/denied/concurrency and accessibility. Run package tests, dependency-rules, type/lint, codegen drift, tenant transition/realtime and web/mobile host gates as applicable.

## Stop conditions

Stop and surface an explicit decision when the task would require inventing product semantics, weakening tenant/security guarantees, breaking a public contract without migration, or suppressing a required gate.
