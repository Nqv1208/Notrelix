---
title: "Normative Language and Rule Format"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Normative Language and Rule Format

## Keywords

- **MUST / MUST NOT**: required for conformance; violation is defect or explicit exception.
- **SHOULD / SHOULD NOT**: default design; deviation requires documented reason in the change.
- **MAY**: explicitly optional.
- **CURRENTLY**: current evidence, not permanent rule.

Avoid “always”, “never”, “should probably” and “recommended” when enforcement level matters.

## Rule ID format

Important durable rules receive stable IDs by authority domain:

```text
NRX-* repository constitution
SYS-* system/cross-stack
BE-*  backend
FE-*  frontend
QLT-* quality/security/delivery
WM-*  work management
DOC-* documents
COL-* collaboration
WSP-* workspaces
ID-*  identity
GOV-* governance
AUT-* automation
INT-* integrations
BIL-* billing
ANA-* analytics
ACC-* accounts
```

Rule IDs are semantic anchors for tests, architecture gates, ADRs and change-impact reports. Do not renumber an existing rule for cosmetic ordering.

## Strong rule shape

For consequential enforceable rules prefer:

```text
ID + concise name
Scope
Rule
Why / protected property
Allowed pattern
Forbidden pattern
Proof / enforcement
Exception path
Related rules
```

Not every paragraph needs an ID. Give IDs to invariants whose violation can cause architecture drift, security/tenant failure, compatibility break, or repeated implementation ambiguity.

## Proof requirement

Every realistically enforceable MUST should declare how it is proven:

- architecture/dependency test;
- unit/behavior test;
- integration/RLS/contract test;
- generated-drift check;
- review-enforced when automation is not practical.

A MUST with no owner and no plausible proof is usually immature documentation.
