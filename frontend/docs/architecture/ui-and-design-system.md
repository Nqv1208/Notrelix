# UI and Design System

## Scope

Frontend implementation ownership for tokens, platform UI separation,
primitives, product components, vendor/shadcn code policy, accessibility and UI
states.

## Responsibility / Ownership

UI packages implement reusable primitives and design-system mechanisms. Product
packages own product-specific components and workflows.

## Current Architecture

Tokens are shared through the token package. Web and mobile UI packages are
separate platform implementations.

## Normative Contracts

- Token package is the shared design-token authority.
- Web UI and mobile UI remain separate.
- Generic UI primitives must not import features or product behavior.
- Product components live with the owning product/feature package.
- Vendor/generated components require clear ownership and accessibility review.
- Keyboard, pointer and touch interactions must match platform expectations.
- Loading, error, empty, permission and conflict states are first-class.
- Accessibility is part of the component contract.

## Allowed Design

Platform-specific UI primitives over shared tokens, product components composed
from UI primitives and focused a11y tests.

## Forbidden Design

No web DOM primitives in mobile UI packages, generic UI importing products, or
styling changes that bypass token ownership without rationale.

## Failure Modes

Product components become generic primitives with business logic, mobile UI
renders web-only elements, keyboard/touch interactions regress.

## Change Impact Rules

Primitive, token, accessibility, platform UI or reusable state changes require
component tests and affected host checks.

## Executable Evidence / Tests / Gates

UI package tests, Storybook/UI freeze tests where relevant, lint and typecheck.

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

Architecture manifest and UI package manifests.

## Explicit Non-responsibilities

This document does not own product semantics or backend contracts.
