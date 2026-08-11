---
title: "Workspaces Context"
document_class: constitution
normative: true
owner: workspaces
maturity: FROZEN
conformance: CANONICAL
applies_to: workspaces
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Workspaces Context

## Mission

Workspaces owns the collaboration tenant structure in which most product data lives: workspace lifecycle, membership/invitations and organizational containers such as spaces/teams where assigned. It establishes who participates in a workspace; Governance decides operation-level authorization.

## WSP-101 — Workspace membership is a security boundary

A principal must not read/write workspace-scoped resources merely because the resource ID exists or an Account relationship exists. Membership/access facts are checked through authoritative server mechanisms before product operations.

## WSP-102 — Tenant identity propagates explicitly

Workspace-owned and workspace-child contracts carry `workspaceId` (or equivalent canonical scope). Persistence/RLS/cache/query/realtime keys include the scope where required. A current-workspace singleton is not sufficient for durable identity.

## WSP-103 — Membership lifecycle protects last-owner/critical-admin invariants

Removing/leaving/demoting a member uses Domain rules plus Application-loaded facts when the invariant depends on counts/external memberships. Rejecting the mutation leaves state/version/events unchanged. Do not solve cross-member invariants with a repository callback inside Domain.

## WSP-104 — Invitation is not membership

Pending invitation, accepted membership, suspended/removed membership and guest/external participation are distinct lifecycle concepts. Accepting/revoking an invitation is idempotent under retry and cannot silently create duplicate memberships.

## WSP-105 — Workspace switch is a client/server scope transition

Frontend must dispose old-workspace realtime/subscriptions and prevent stale cache/responses from surfacing in the new workspace before loading child data. Backend never trusts client “active workspace” alone; each operation establishes/validates scope.

## Spaces/teams

If Spaces/Teams organize resources, their parent path and tenant identity are validated. They do not become authorization shortcuts unless Governance policy explicitly defines that relationship. Hierarchies prevent cycles/cross-workspace parentage.

## Cross-context

Work Management/Documents/Collaboration reference Workspace identity, not Workspace aggregate objects. Billing Account entitlement may constrain workspace creation/features but Billing does not own membership. Governance consumes workspace membership facts to evaluate operations. Account lifecycle may trigger workspace access changes through explicit workflow/events.

## Lifecycle/deletion

Archive/suspend/delete semantics must state what happens to memberships, product resources, integrations, billing association and retention. Generic cascade deletion across contexts is forbidden. Workspace deletion is a process with retention/export/legal consequences, not a single ORM cascade.

## Forbidden designs

- workspace role stored independently in every product resource;
- fake workspace IDs for account/global operations;
- direct cross-context DB queries from product Domain to inspect membership;
- treating an invitation as active access;
- switching workspace by only changing a frontend variable.

## Tests/change impact

Cover membership/invitation lifecycle, last-owner guards, cross-workspace rejection, hierarchy, tenant key propagation, RLS/authorization integration and frontend stale-scope transitions. Membership semantic changes require Governance and all workspace-scoped product consumers review.
