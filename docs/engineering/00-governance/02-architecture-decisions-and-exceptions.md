---
title: "Architecture Decisions and Exceptions"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Architecture Decisions and Exceptions

## ADR required when

Use an ADR for a consequential durable choice, including:

- project/package dependency direction or new production project/package class;
- bounded-context ownership changes;
- new cross-context integration pattern;
- persistence/contract versioning model;
- foundation/platform mechanism that many capabilities will depend on;
- replacing a frozen architecture rule;
- security/tenant model changes;
- technology choice with meaningful migration/operational cost.

Routine feature design does not need an ADR when canonical rules already determine the solution.

## Architecture exception

An exception is permission for current code to violate a rule temporarily. It is not a second architecture.

Every exception MUST include:

```text
ID
violated rule
exact scope/files
reason the compliant path cannot be completed now
risk
owner
removal condition
expiry/review trigger
new usage prohibited? yes/no
verification that prevents spread
```

No owner/removal condition = no exception.

## Decision states

- Proposed — not authority.
- Accepted — normative and must update canonical owner.
- Superseded — preserved historically with pointer to replacement.
- Rejected — useful rationale, never authority.

## Same-transaction rule

When an ADR changes an existing rule, update in the same delivery transaction:

1. canonical handbook/constitution;
2. affected RULE if constitution-level;
3. tests/gates;
4. migration/compatibility handling;
5. agent route if workflow changes;
6. decision index.
