---
title: "Threat Model"
document_class: handbook
normative: true
owner: security
maturity: STABILIZING
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Threat Model

## Primary assets

Tenant business data, user identities/sessions, permissions, provider credentials, billing state, audit integrity, contract/runtime availability.

## Key trust boundaries

1. untrusted browser/mobile client → API;
2. authenticated actor → resource authorization;
3. Application → database/RLS;
4. outbox/message producer → consumer;
5. backend → external provider;
6. generated contract → independently evolving frontend;
7. operator/admin tooling → tenant data.

## Threat classes

### Cross-tenant access

Mitigation: scoped identity, Application authorization, resource verification, RLS, tenant-safe cache/search/realtime keys.

### Privilege escalation

Mitigation: centralized permission semantics; distinguish owner/admin/member/guest rules; permission changes audited.

### Replay/duplicate commands/messages

Mitigation: idempotency/concurrency and consumer deduplication with stable operation identity.

### Secret leakage

Mitigation: secret manager/config boundaries, payload/log minimization, no raw secret events.

### Mass assignment / model overexposure

Mitigation: API contracts separate from EF/Domain entities; Application/Domain own allowed mutation.

### Supply-chain/generated drift

Mitigation: dependency policy, lockfiles, codegen drift, CI and vulnerability checks.

## Review

Any new external ingress (webhook/import/public share), new auth/session mechanism or cross-tenant administrative capability updates this model.
