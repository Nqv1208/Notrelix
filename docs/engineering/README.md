---
title: "Notrelix Engineering Documentation"
document_class: context
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Notrelix Engineering Documentation

This tree contains the **canonical engineering knowledge** for Notrelix. It is organized by topic authority rather than by source-folder depth.

```text
00-governance     documentation/decision authority, exceptions, maturity
01-system         cross-stack architecture and contracts
02-backend        backend implementation contracts
03-frontend       frontend implementation contracts
04-quality        engineering quality/security/testing/review
05-delivery       ownership, change delivery and rollout
06-operations     incident/readiness/recovery runbooks
07-infrastructure deployment/runtime infrastructure
08-product        product model and bounded-context constitutions
adr               accepted consequential architecture decisions
templates         reusable decision/change artifacts
```

## Reading rule

Do not read the whole tree for every task. Use `CONTEXT-MAP.md`, the nearest `AGENTS.md`, and the Topic Authority Map to find the minimum authoritative set.

## Normative vs evidence

Canonical docs state intended engineering/product truth. Source, tests, generated manifests and CI are executable evidence of current behavior. A mismatch must be classified and resolved; neither side silently wins by convenience.
