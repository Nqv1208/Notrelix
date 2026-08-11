---
title: "Notrelix Product Model"
document_class: constitution
normative: true
owner: product
maturity: FROZEN
conformance: CANONICAL
applies_to: product
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Notrelix Product Model

Notrelix is an enterprise work-management workspace OS: teams organize tenants/workspaces, model work on flexible boards/tables/views, create documents, collaborate, automate workflows, integrate providers and govern access/commercial usage.

## PROD-101 — Work Management is not “Kanban CRUD”

A Board is a work database/table with dynamic schema. BoardItems are records; BoardFields define schema; BoardViews project/filter/sort/group the same item data. Kanban is one view configuration, not the data model. BoardGroup is an organizational grouping concept and MUST NOT be treated as the universal status column.

## PROD-102 — Views do not own duplicate work data

Table/Kanban/Calendar/Timeline and future views are configurations/projections over shared board items/field values. Changing an item through one view changes the underlying item, subject to version/authorization, and other views converge on that state.

## PROD-103 — Documents are a separate capability with intentional links

Pages/blocks own document hierarchy/content. Work Management owns work records/schema. Relationships between them use stable cross-context links/contracts rather than merging document blocks into board tables or storing board internals inside Documents.

## PROD-104 — Account, identity and workspace are distinct scopes

An Account is an administrative/commercial ownership scope, Identity authenticates people/service principals, and Workspace is a collaboration tenant scope. Do not invent fake Workspace IDs for account/global operations or model identity as a mutable Workspace child.

## PROD-105 — Governance is cross-cutting authority, not scattered role checks

Permission/audit/security semantics are centralized. Product contexts declare resources/operations and business constraints; backend authorization decides access server-side using governance/membership/entitlement facts. UI permission guards improve UX only.

## PROD-106 — Automation/integrations react through durable contracts

Automation rules respond to approved triggers/events and execute actions through application/integration mechanisms with idempotency. Integrations own provider connections/webhooks/credentials references. A business aggregate must not call provider SDKs directly.

## Product extension test

A proposed feature must answer: whose vocabulary/lifecycle is it; which tenant/account scope owns it; which aggregate/resource is authoritative; what authorization applies; what cross-context contracts are required; what frontend server-state owner exists; what migration/events/realtime result; and what proves correctness. A new screen/table/team is not sufficient reason for a new context.
