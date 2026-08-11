---
title: "Documentation V3 to V4 Migration"
document_class: runbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Documentation V3 to V4 Migration

V4 is not additive to V3. It changes authority topology, so applying new files without deleting superseded scoped documents would recreate ambiguity.

## MIG-DOC-101 — Apply as one authority transaction

The migration MUST update root routers/constitutions, canonical engineering docs, scoped AGENTS, docs governance scripts and delete superseded nested RULE/CONTEXT/boilerplate files in one branch/PR. Do not run V3 and V4 authority models side-by-side.

## Procedure

1. Start from the intended source SHA/branch and preserve unrelated working-tree changes.
2. Copy V4 files preserving paths.
3. Delete every path in `DELETE-MANIFEST.txt` that is still present and still represents the superseded V3 docs model. If a listed file has since gained new approved semantics, migrate those semantics to its V4 canonical owner before deletion.
4. Do not delete executable source, tests, manifests or unrelated project READMEs just because they are absent from this overlay.
5. Run `node scripts/docs/generate-rule-index.mjs`.
6. Run `node scripts/docs/check-docs.mjs`.
7. Search for links to deleted `RULE.md`/`CONTEXT.md` paths and repair them to canonical owners.
8. Run repository-native architecture/documentation checks if they coexist with V4; update generators to emit V4 topology rather than disabling either check.
9. Review the diff specifically for duplicate normative statements and stale frontend topology.
10. Merge only when documentation governance and repository required checks are green on the exact SHA.

## Conflict handling

When a repository file has evolved since the audited baseline, do not overwrite blindly. Classify the difference. Preserve newer accepted product/architecture facts, but keep V4 role boundaries: detailed semantics belong to canonical docs; deep scoped files should normally be AGENTS only.

## Completion proof

- no nested `RULE.md` outside `/`, `backend/`, `frontend/`;
- no `CONTEXT.md` outside `/`, `backend/`, `frontend/`;
- only admitted scoped AGENTS exist;
- unique rule IDs and no broken links;
- rule index regenerated with zero drift;
- current frontend descriptions use pnpm/Turborepo + current hosts/packages;
- repository-specific architecture/doc generators have been reconciled rather than bypassed.
