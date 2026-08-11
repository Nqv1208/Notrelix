---
title: "Documentation Lifecycle and Drift"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Documentation Lifecycle and Drift

## Documentation admission rule

**Folder existence does not imply documentation-file existence.** Create a scoped document only when at least one is true:

1. scope has distinct agent workflow/investigation needs;
2. scope owns a durable invariant not owned by parent/canonical topic;
3. ownership/lifecycle differs materially from parent;
4. public contract or runtime environment differs;
5. operational procedure differs;
6. a non-obvious dependency/exception cannot be expressed better in canonical docs/generated inventory.

## File-type admission

### `AGENTS.md`

May exist deep when local execution genuinely differs. Preferred scoped file because nearest-scope instructions reduce agent guesswork without duplicating architecture.

### `RULE.md`

Restricted to repository and technology constitutions (`/`, `backend/`, `frontend/`) unless an explicit governance decision admits another constitution boundary. Do not create per-project/package RULE files.

### `CONTEXT.md`

Restricted to compact repository/technology snapshots. Project/package inventories are generated/verified maps, not a tree of hand-maintained CONTEXT files.

### README

Create where humans need onboarding/usage/navigation. README must not become an independent architecture authority.

## Duplication rule

One semantic rule has one canonical owner. Other files link to it and describe local execution, proof, or current evidence without restating a second normative copy.

## Alias rule

Compatibility files such as `CLAUDE.md`, `DESIGN.md`, `MEMORY.md`, root `SKILL.md` are pointer-only. They MUST contain zero independent normative architecture/product rules.

## Machine-derived facts

Project/package lists, dependency graphs, scripts, generated contracts, public exports and test inventories SHOULD be generated or CI-verified from source. Markdown adds purpose/ownership/constraints, not a stale copy of machine facts.

## Lifecycle

- update canonical docs in the same change as approved behavior/architecture change;
- mark superseded/historical material clearly;
- remove obsolete scoped boilerplate rather than leaving empty shells;
- CI checks broken links, frontmatter, rule IDs, admitted scopes and stale maps where possible.
