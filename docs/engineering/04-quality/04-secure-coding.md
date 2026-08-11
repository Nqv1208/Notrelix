---
title: "Secure Coding Contract"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Secure Coding Contract

Security is cross-stack and tenant-aware. The detailed threat model lives in `../01-system/14-threat-model.md`.

## QLT-SEC-101 — Treat external input as untrusted

Validate shape, size, type/discriminator and business constraints at the owning boundary. Normalize deliberately. Never construct SQL/HTML/URLs/commands from unchecked input. Use parameterized queries and framework-safe encoding primitives.

## QLT-SEC-102 — Authorization is server enforced

Frontend visibility is UX, not permission. Backend checks tenant/resource scope and operation authorization using the canonical pipeline/service. Avoid endpoint/handler-specific ad-hoc permission shortcuts that bypass central semantics.

## QLT-SEC-103 — Secrets never enter source/log/client artifacts

Credentials, tokens, signing keys and provider secrets come from approved secret/config channels. Logs/telemetry redact them. Generated fixtures use obviously non-secret test values. Do not “test” secret scanners by committing realistic active-looking keys unless the scanner's documented test mechanism is used.

## QLT-SEC-104 — SSRF/file/upload/integration boundaries are constrained

Outbound destinations, redirects/webhooks and file handling need explicit allow/validation policy, size/type limits and authorization. Provider responses remain untrusted external data.

## QLT-SEC-105 — Sensitive errors fail closed

Do not expose resource existence, raw stack traces, SQL/provider details, token claims or permission internals to untrusted clients. Preserve correlation identifiers for operators.

## Dependencies

Security-sensitive dependency upgrades follow the dependency policy and run affected test/build gates. Known vulnerable packages are assessed for reachability and remediation, not ignored solely because the scanner cannot prove exploitability.
