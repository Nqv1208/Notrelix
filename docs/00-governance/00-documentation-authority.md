---
title: "Documentation Authority"
document_class: constitution
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Documentation Authority

## Purpose

Prevent two failure modes: local files overriding global invariants, and stale documentation overriding verified current behavior without investigation.

## Separate scope from role

Never model authority as one flat list where `AGENTS.md` can accidentally outrank `RULE.md`.

### Scope resolution for instructions

```text
explicit task instruction
→ nearest applicable AGENTS.md
→ parent AGENTS.md files
→ root AGENTS.md
```

A nearer `AGENTS.md` specializes workflow for its scope; it cannot relax a repository/technology rule.

### Semantic authority by role

```text
accepted product/architecture decision
→ repository/technology RULE constitution
→ canonical topic owner under docs/engineering
→ current CONTEXT / generated inventory
→ skill procedure
→ incidental local source pattern
```

## Source/tests/CI are evidence

Executable evidence answers **what the repository currently does**. Canonical docs answer **what the approved system is intended to do**. Existing code is not automatic precedent because it may be transitional debt or regression.

When evidence and documentation disagree:

1. identify the exact rule/fact in conflict;
2. inspect direct callers/consumers, tests, migrations/contracts and gates;
3. classify the mismatch as approved change, transitional exception, stale documentation, or regression;
4. inventory affected producers/consumers;
5. update source + canonical docs + gates together when the approved design changes;
6. never choose the cheaper side silently.

## Document classes

- **constitution** — small, high-stability invariants/semantics; changing it requires explicit review.
- **handbook** — normative implementation contract/mechanism.
- **context** — current state, non-normative unless explicitly stated.
- **adr** — accepted consequential decision and alternatives.
- **runbook** — operational procedure.
- **template** — reusable structure, not project truth.
- **historical** — retained evidence, never current authority.

## Rule change transaction

A rule is changed only by an explicit approved decision. The same transaction must update the canonical owner, affected tests/gates, compatibility/migration plan and relevant agent routing. Editing a local `AGENTS.md` is never sufficient to change architecture.
