---
title: "Team and Capability Ownership"
document_class: handbook
normative: true
owner: engineering-delivery
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Team and Capability Ownership

## DLV-OWN-101 — Logical owner is stable; staffing is not architecture

Every durable capability/mechanism has a logical owner such as Work Management, Documents, Identity, backend-platform, frontend-architecture or engineering-quality. Actual people/teams may change. Do not encode temporary team names into package/domain boundaries.

## Vertical ownership

A product capability change may touch Domain/Application/Infrastructure/API/contracts/frontend product state/web/mobile. Those are implementation layers of one semantic change, not separate products. Platform/foundation teams own reusable mechanisms but do not absorb business policy.

## Cross-owner changes

The initiating change identifies producers/consumers and obtains review from affected logical owners. Ownership does not mean unilateral write access; security/tenant/contract changes can require architecture/security review.

## Escalation

When ownership is ambiguous, choose the business vocabulary owner first. If no existing owner fits, record an architecture decision before creating a generic shared module. The ownership matrix lists durable topic owners, not GitHub handles.
