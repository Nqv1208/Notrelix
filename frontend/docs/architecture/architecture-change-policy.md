# Architecture Change Policy

## Scope

Protected foundation, normal feature changes, architecture changes, ADR
requirements, public export/package graph changes, contract migration, review
expectations and proof requirements.

## Responsibility / Ownership

Architecture changes are deliberate changes to package ownership, runtime
boundaries, host composition, public contracts or generated dependency rules.

## Current Architecture

The protected foundation is the closed-world manifest, public export model,
host/runtime split, generated contracts, query/realtime conventions and
architecture/test gates.

## Normative Contracts

- Normal feature changes stay within existing ownership and public contracts.
- Architecture changes modify ownership, dependency graph, runtime boundaries,
  public exports, generated contract flow, host composition or gate topology.
- Consequential architecture changes require an ADR or explicit exception.
- Public export and package graph changes update manifest, generated docs and tests.
- Contract migrations define compatibility, rollout and drift checks.
- Do not reopen foundation for local convenience.
- Review includes affected package owners, host impact and proof commands.

## Allowed Design

Additive feature work inside approved boundaries, ADR-backed ownership changes
and temporary compatibility layers with owner/removal condition.

## Forbidden Design

No freeze/version/package-count claims as active architecture truth, deep imports
to avoid export review, hidden architecture changes or test-only patterns as
production precedent.

## Failure Modes

Product features silently change runtime ownership, ADR history is rewritten,
generated docs drift after manifest changes.

## Change Impact Rules

Any architecture change must update source, generated evidence, docs/ADR and
proof gates in the same transaction unless a staged plan is explicit.

## Executable Evidence / Tests / Gates

`pnpm check:architecture`, `pnpm check:architecture-docs`, generator tests and
affected host/package tests.

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

Architecture manifest, package manifests and generated package boundaries.

## Explicit Non-responsibilities

This policy does not approve backend product/API changes by itself.
