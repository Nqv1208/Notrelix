---
title: "Architecture Exception Template"
document_class: template
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Architecture Exception Template

## Rule being violated
Exact rule ID and canonical owner.

## Scope
Exact files/packages/projects/runtime path. No broad “frontend” or “backend” scope unless unavoidable.

## Reason
Why conforming now is unsafe/impossible and why the exception is preferable to changing the rule.

## Risk / compensating controls
Security/tenant/compatibility/maintainability risk and tests/review/monitoring that contain it.

## Owner
Logical owner responsible for removal.

## Expiry/removal condition
Concrete date, release/capability condition or tracked dependency. “Later” is invalid.

## Validation
How CI/review prevents the exception from expanding beyond approved scope.
