---
title: "System Glossary"
document_class: context
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# System Glossary

- **Account:** customer/account-level administrative/commercial boundary; not interchangeable with Workspace.
- **Workspace:** tenant collaboration/resource scope in which most work content lives.
- **Space:** optional organizational container under a workspace; not a universal parent requirement for every resource.
- **Board:** Work Management work table/database and schema owner.
- **BoardField:** dynamic schema field/column definition.
- **BoardItem:** work row/task/item; “card” is a view presentation, not canonical domain identity.
- **BoardGroup:** table section/group; not automatically Kanban status.
- **BoardView:** saved validated configuration/projection over board data.
- **Page / Block:** Documents hierarchy/content units.
- **Domain event:** internal completed business fact from Domain behavior.
- **Integration event:** durable cross-boundary event for independent consumers.
- **Realtime event:** client freshness/interaction contract; not persistence source of truth.
- **RLS:** PostgreSQL row-level-security defense-in-depth for tenant scope.
- **Outbox:** transactional recording of durable side-effect/integration intent, dispatched after commit.
- **Idempotency:** stable operation/message identity preventing duplicate side effects under retry.
- **Semantic no-op:** valid request that produces no state/audit/version/event change.
- **Frozen:** architecture/public contract protected by impact/migration review, not immutable forever.
