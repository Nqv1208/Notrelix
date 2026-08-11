---
title: "Collaboration Context"
document_class: constitution
normative: true
owner: collaboration
maturity: FROZEN
conformance: CANONICAL
applies_to: collaboration
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Collaboration Context

## Mission

Collaboration owns human interaction around resources: comments/threads, mentions, reactions, notifications, user-facing activity, attachments metadata and ephemeral presence/cursors where assigned. It references resources from Work Management/Documents/etc.; it does not take ownership of those resources.

## COL-101 — Collaboration target is an explicit scoped resource

Every comment/thread/reaction/activity target carries stable resource type/id and workspace/account scope needed to authorize and route it. Creating or reading collaboration data validates target existence/access through Application/Governance contracts. A naked target ID or caller-supplied workspace is not trusted.

## COL-102 — ActivityLog and AuditLog are different products

Activity is user-facing narrative, may be grouped/aggregated and can evolve presentation. Audit is governance/security evidence, append-oriented with stronger retention/integrity requirements. Collaboration MUST NOT be used to mutate/delete Governance audit history.

## COL-103 — Notifications have an explicit recipient

A durable user notification identifies recipient user/principal, scope, type, referenced resource and read/delivery state as applicable. “Broadcast notification” requires an explicit fan-out/audience mechanism; do not store an ambiguous row without recipient ownership and resolve recipients at read time.

## COL-104 — Reaction uniqueness is deterministic

Define reaction identity (target + actor + reaction kind, or approved equivalent). Repeated create/delete calls are idempotent and do not produce duplicate counts/events.

## COL-105 — Attachments store metadata/object identity, not arbitrary binaries in Domain

Object storage/provider I/O is Infrastructure/Integration. Collaboration or the owning resource stores safe file metadata/reference and validates authorization, size/type/policy. Download/upload URLs are short-lived/provider boundary artifacts when applicable.

## Mentions and delivery

Mention parsing/normalization identifies stable users, validates workspace participation/access and creates notification/activity through durable application/event flow. Notification provider delivery (push/email) is a side effect tracked idempotently; notification creation remains authoritative independently of provider success.

## Presence

Presence/cursors/typing are ephemeral collaboration signals and can tolerate loss/reconnect. They are tenant/resource-scoped and authorized. They MUST NOT become a durable source of truth for document/item content.

## Lifecycle

Deleting/archiving a target resource defines whether comments become hidden/tombstoned/read-only or are retained. Comment edit/delete policy is explicit and auditable where required. Notification retention/read state is separate from source-resource lifecycle.

## Forbidden designs

- storing product resource object graphs inside comments;
- treating Activity as security Audit;
- notifications without recipient identity;
- provider email/push result as notification source of truth;
- unauthenticated resource-wide realtime channels;
- binary payloads in domain events/logs.

## Tests/change impact

Cover target authorization/cross-workspace rejection, reaction idempotency, recipient fan-out, mention validation, attachment metadata/security, provider retry and realtime duplicate/reconnect. New target resource types require Governance/resource-contract and frontend deep-link/render support review.
