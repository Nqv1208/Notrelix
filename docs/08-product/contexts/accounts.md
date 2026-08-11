---
title: "Accounts Context"
document_class: constitution
normative: true
owner: accounts
maturity: FROZEN
conformance: CANONICAL
applies_to: accounts
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Accounts Context

## Mission

Accounts owns durable organization/customer administration above individual workspaces: the identity of the customer/account boundary, account lifecycle and account-scoped administrative facts that must not be faked as workspace state.

### Owns
Account identity/lifecycle and account-scoped administrative settings/facts explicitly assigned to this context.

### Does not own
User authentication/credentials (Identity), workspace collaboration/membership internals (Workspaces), permission evaluation (Governance), commercial subscription records (Billing), or product records inside a workspace.

## Ubiquitous language

**Account**: administrative/customer scope. **Account member/administrator** when the model supports account-level participation: a relationship at Account scope, not automatically a Workspace membership. **Account lifecycle**: explicit active/suspended/closed/deletion behavior, never inferred from a billing provider alone.

## ACC-101 — Account is not a Workspace child

Account-scoped commands, storage/cache keys, authorization resources and events MUST use account scope directly. Do not create a synthetic Workspace ID to reuse workspace-only code.

**Proof:** request/resource-scope tests; persistence/query tenant-scope tests where account-owned data is stored.

## ACC-102 — Account lifecycle effects are explicit

Suspending/closing an Account can affect workspace access, subscription behavior, integrations and sessions, but Accounts does not mutate those contexts' internals directly. The use case defines synchronous guards and durable downstream events/processes.

## ACC-103 — Account identity is stable across commercial changes

Plan/subscription upgrade/downgrade/cancellation does not replace the Account identity. Billing references Account by stable ID/contract.

## Authorization

Account administration requires account-scoped operations and subject facts. Workspace role alone is insufficient unless the canonical policy explicitly maps it. Backend is final authority; frontend hides/disables controls only for UX.

## Consistency and events

Account creation/name/settings/lifecycle changes are atomic within the Account aggregate/owner. Cross-context effects use stable events such as account lifecycle facts, only when a real consumer exists. Event payloads include account identity and safe changed facts, not secrets/full object graphs.

## Deletion/retention

Account deletion is a product/legal workflow, not generic soft delete. It must define retention, workspace/content consequences, billing closure, integrations and identity linkage before physical removal. Append-only audit/commercial evidence follows retention policy rather than cascading blindly.

## Forbidden designs

- treating Account and Workspace as synonyms;
- storing account-wide settings on an arbitrary “primary workspace”;
- allowing Billing/provider webhook to directly delete Account;
- using account membership to bypass workspace membership/authorization.

## Testing/change impact

Cover lifecycle guards, account-vs-workspace scope rejection, authorization, cross-context event emission/no-op behavior and migration of any account-scoped key. Any change to account lifecycle requires impact review for Workspaces, Identity, Governance, Billing and Integrations.
