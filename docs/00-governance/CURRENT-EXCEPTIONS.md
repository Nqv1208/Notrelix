---
title: "Current Architecture Exceptions"
document_class: context
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Current Architecture Exceptions

This file lists only **known approved/current exceptions** that must not become precedent. Remove entries when the exception is eliminated.

## EX-BE-APP-EF-001 — Application EF package reference

- **Rule affected:** BE-ARCH-001 / persistence ownership intent.
- **Current evidence:** `Notrelix.Application.csproj` currently references `Microsoft.EntityFrameworkCore`.
- **Constraint:** package presence does not authorize new direct persistence implementation in handlers. New code follows Application ports/pipeline boundaries.
- **Removal condition:** Application no longer requires EF types for approved abstractions/transition and reference can be removed without contract break.
- **Owner:** backend architecture.

## External unresolved decisions

External/organizational values such as numeric production SLO/RPO/RTO are not architecture exceptions; they remain unresolved configuration/policy decisions and are tracked in `CURRENT-EXTERNAL-DEPENDENCIES.md`.
