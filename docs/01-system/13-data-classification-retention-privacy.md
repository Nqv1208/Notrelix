---
title: "Data Classification, Retention and Privacy"
document_class: handbook
normative: true
owner: security
maturity: STABILIZING
conformance: CANONICAL
applies_to: data
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Data Classification, Retention and Privacy

## Classification

At minimum distinguish:

- public/marketing data;
- internal operational metadata;
- tenant business content;
- personal/account identity data;
- security-sensitive credentials/tokens/secrets;
- immutable/compliance-oriented audit/commercial records.

## SYS-PRIV-001 — Collect/store only what the capability needs

Do not add sensitive fields to events/logs/analytics “for future use”. Contract payload minimization is part of privacy/security.

## Retention

Retention/deletion policy follows the owning data class and business/legal decision. Generic soft delete is not a universal retention strategy. Audit/invoice/usage facts may require append-only or business-specific retention.

## Erasure/export

Identity/account deletion/export workflows must inventory cross-context owned data and derived systems. A frontend “delete account” action is not proof that provider logs, search indexes, analytics or backups satisfy policy.

Numeric retention windows are external decisions until approved; mechanisms should make policy explicit/configurable rather than inventing durations.


## Derived copies

Search indexes, caches, analytics projections, exports, backups and provider-held copies are derived retention surfaces. A deletion/erasure workflow must know which are rebuildable vs independently retained and how eventual purge is verified. Cache expiry is not a legal retention policy; backup immutability can require delayed erasure with documented operational/legal treatment.

## Contract minimization

Events, realtime payloads and telemetry include only fields needed by consumers. Do not propagate document text, access tokens or sensitive profile fields simply because the producer has them. Consumer need is reviewed before expanding a durable event because event history can outlive source rows.

## Access and export

Administrative export/audit operations are authorized and auditable. Export files receive explicit expiry/storage protection and are not exposed through permanent public URLs. Privacy behavior for a context must remain compatible with its business/audit retention obligations; if those conflict, an approved policy decision is required rather than an arbitrary cascade delete.
