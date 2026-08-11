---
title: "ADR-0001 Documentation Authority and Scope"
document_class: adr
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# ADR-0001 — Documentation Authority and Scope

## Status
Accepted.

## Context
The previous documentation generation used a mandatory `AGENTS.md + RULE.md + CONTEXT.md` triple at every admitted boundary. It made scope resolution explicit but produced dozens of shallow files and repeated the same architectural invariant across technology, project and package scopes. The precedence model became more complicated than the knowledge itself.

## Decision
Use semantic-role ownership instead of structural symmetry: repository and technology `RULE.md` files hold constitutions; canonical engineering docs hold detailed architecture/product truth; `CONTEXT.md` exists only at repository/backend/frontend current snapshots; deep scopes may have `AGENTS.md` only when local execution workflow materially differs; inventory is generated/verified from source; aliases are pointer-only.

`AGENTS.md` never overrides `RULE.md`. Existing source is executable evidence, not automatic precedent against an accepted canonical decision.

## Consequences
Fewer files exist at deep scopes, but remaining canonical docs must be substantially deeper. A new folder no longer implies a documentation file. Docs CI must prevent reintroduction of nested RULE/CONTEXT boilerplate and check unique rule IDs/links/metadata.

## Migration
Delete V3 nested RULE/CONTEXT files outside repository/backend/frontend, preserve useful semantics by moving them to canonical topic owners, and keep only justified scoped AGENTS.

## Proof
`scripts/docs/check-docs.mjs` enforces allowed scope locations and metadata; V4 validation reports structural counts and duplicate/broken-link checks.
