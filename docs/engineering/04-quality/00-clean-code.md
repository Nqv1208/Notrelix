---
title: "Clean Code and Maintainability Contract"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Clean Code and Maintainability Contract

Clean code in Notrelix means the next engineer can infer ownership, invariants and failure behavior from structure without reverse-engineering hidden conventions.

## QLT-CODE-101 — Ownership is visible

A type/function/module belongs to one architectural owner. Names and location reflect the product or mechanism it serves. “Common”, “Helper”, “Manager” and “Utils” are warning signs when they hide mixed responsibility.

## QLT-CODE-102 — Public API is smaller than implementation

Expose only stable consumer needs. Internal implementation is free to change. Do not make a method/type public merely to make a test or another package/project reach it; test through behavior or create a justified contract.

## QLT-CODE-103 — Invariants are centralized

A business invariant has one authoritative implementation owner. API/client validation may improve UX but does not replace Domain/server enforcement. Avoid duplicating formulas/state-transition rules across handlers/components.

## QLT-CODE-104 — Failure behavior is explicit

Methods that can reject distinguish validation, authorization, not-found, concurrency and transient infrastructure failure where callers need different recovery. Do not use exceptions for ordinary branch logic inside pure code, and do not catch broad exceptions merely to return success/default values.

## Complexity

Prefer cohesive functions/classes over arbitrary line limits. Split when a unit has multiple reasons to change, mixes abstraction levels or cannot be tested without unrelated setup. Avoid abstraction introduced only to remove three repeated lines when semantics may diverge.

## Comments and TODOs

Comments capture why/invariant/compatibility constraint. TODO/FIXME must reference tracked debt/exception and removal condition for non-trivial architectural work. Dead commented code is deleted; git is history.

## Review proof

Formatter/linter handles style. Review focuses on ownership, invariants, failure atomicity, dependency direction, security/tenant scope, concurrency, compatibility and tests. A beautifully formatted architectural violation is still a defect.
