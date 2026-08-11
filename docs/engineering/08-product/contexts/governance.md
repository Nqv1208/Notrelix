---
title: "Governance Context"
document_class: constitution
normative: true
owner: governance
maturity: FROZEN
conformance: CANONICAL
applies_to: governance
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Governance Context

## Mission

Governance owns authorization policy semantics, resource/subject permission representation, share/access policy and append-oriented administrative/security audit evidence. It converts authenticated principals plus membership/resource/context facts into server-side permission decisions.

### Owns
Permission action/resource/subject vocabulary, ACL/policy/matrix semantics where used, authorization decision contract, sharing rules, audit/security-event semantics.

### Does not own
Authentication credentials (Identity), membership lifecycle (Workspaces), product resource state (Work Management/Documents/etc.), subscription commercial truth (Billing).

## GOV-101 — Backend authorization is the final decision

Every material command/query over protected data is authorized server-side. Frontend guards are only UX. A query is not harmless: list/search/export/realtime subscription can leak data and requires the same resource/tenant semantics as mutation.

## GOV-102 — Policy is centralized; handlers do not hard-code roles

Handlers/endpoints declare operation/resource requirements and supply context. Authorization behavior/service evaluates canonical policy. Code such as `if role == Admin` inside arbitrary product handlers is forbidden unless the business rule genuinely belongs to that context and the policy abstraction cannot express it.

## GOV-103 — Owner, Admin, Member and Guest are not interchangeable

Workspace Owner retains privileged lifecycle/security responsibilities; Admin is operational and cannot silently gain final-owner/delete/security powers. Guest/external access is resource-limited by default and MUST NOT obtain workspace-wide enumeration merely because a shared resource is visible. Exact permission matrices evolve in Governance but these distinctions remain fail-closed.

## GOV-104 — Resource permission is an ACL fact, not the whole system

An ACL entry alone cannot represent inherited role, workspace membership, account policy, share link, entitlement and resource-specific constraints. Authorization decision considers required facts through explicit policy/matrix/service contracts.

## GOV-105 — Permission-sensitive cache is versioned/scoped

Caching an authorization decision must include account/workspace/user/subject and the authoritative permission/policy version or equivalent invalidation identity. Placeholder versions such as `default`/`unknown` are forbidden for protected cached decisions.

## GOV-106 — Permission changes are auditable

Grant/revoke resource access, membership/role changes, share-link lifecycle, sensitive visibility/security changes, export/delete and audit-log access produce append-oriented audit evidence with actor, operation, target/scope, time and safe decision/result metadata. Audit records are not ordinary editable activity items.

## Sharing

Share links are explicit principals/capabilities with resource scope, lifecycle, optional expiry/password/policy and revocation. A public-link resource does not make linked resources transitively public. When a document embeds a board/item, target authorization is evaluated independently unless a canonical sharing rule explicitly grants it.

## Cross-context interaction

Workspaces supplies membership facts. Product contexts expose resource identities/operation vocabulary and business guards. Billing may supply entitlement facts but does not directly grant product authorization. Identity supplies principal/session security facts. Governance returns a decision; it does not mutate product aggregates as a side effect.

## Events/realtime

Permission/membership changes may trigger cache invalidation/session/realtime access updates. Realtime subscription authorization is server enforced and can be revoked while a connection exists. Event payloads avoid leaking protected ACL details to unauthorized consumers.

## Deletion/retention

Audit/security records are append-oriented and governed by retention/privacy policy, not generic soft-delete symmetry. ACL/share entries may be revoked/expired; historical audit of that action remains separate.

## Forbidden designs

- role strings scattered in handlers/components;
- frontend-only permission enforcement;
- querying first and filtering protected results later in memory;
- guest enumeration of all workspace resources;
- caching decisions without authoritative scope/version;
- mutable/deletable AuditLog treated like ActivityLog;
- permission change without audit evidence where policy requires it.

## Testing/change impact

Test allow/deny, owner/admin/guest distinctions, query/list filtering, cross-workspace resource IDs, cache invalidation/version, share revocation and audit creation. Any new product resource/action must update permission vocabulary/policy tests and frontend UX handling without moving authority client-side.
