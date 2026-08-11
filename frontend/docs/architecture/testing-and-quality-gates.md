# Testing and Quality Gates

## Scope

Node tests, web/component tests, mobile tests, integration tests, E2E,
architecture tests, package-boundary drift, codegen drift, typecheck/lint/format,
Docker packaging where relevant, `validate:fast` vs `validate` and non-zero-work
evidence.

## Responsibility / Ownership

Frontend tests prove runtime and package contracts. Architecture gates protect
dependency boundaries and generated evidence.

## Current Architecture

Root frontend scripts in `package.json` define authoritative commands.

## Normative Contracts

- `test:node` covers framework-neutral logic.
- `test:web` covers web/component behavior.
- `test:mobile` covers React Native/mobile behavior.
- `test:integration` covers cross-package workflows.
- E2E covers deployed/production-like web paths.
- Architecture checks enforce manifest and dependency rules.
- Architecture-doc checks enforce generated package-boundary drift.
- Codegen checks prevent generated public contract drift.
- Typecheck, lint and format are required quality gates.
- Guarded test scripts assert non-zero test work.
- `validate:fast` is the fast local contract; `validate` adds broader coverage.

## Allowed Design

Focused test files during implementation followed by affected broader gates.

## Forbidden Design

No removing tests to hide drift, zero-test success as proof, hand-editing
generated boundaries, or weakening architecture checks for convenience.

## Failure Modes

Package changes pass unit tests but break manifest; generated contracts drift;
mobile-only regressions are missed by web tests.

## Change Impact Rules

Run targeted tests for changed packages and relevant architecture/codegen gates.

## Executable Evidence / Tests / Gates

```bash
pnpm install --frozen-lockfile
pnpm check:architecture
pnpm check:architecture-docs
pnpm codegen:check
pnpm typecheck
pnpm lint
pnpm test
pnpm validate
```

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

`package.json`, `turbo.json`, `vitest.workspace.ts`, Playwright configs and the
architecture manifest.

## Explicit Non-responsibilities

This document does not define backend gates.
