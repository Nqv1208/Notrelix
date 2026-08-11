---
title: "Container and Build Artifacts"
document_class: handbook
normative: true
owner: infrastructure
maturity: STABILIZING
conformance: CANONICAL
applies_to: infrastructure
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Container and Build Artifacts

## INFRA-BUILD-101 — The release artifact is reproducible

Build from committed source, locked dependency manifests and declared toolchains. Restore/install uses frozen lock semantics where supported; code generation runs from source templates/contracts; generated drift causes failure rather than being silently modified inside the image.

## INFRA-BUILD-102 — Build context excludes secrets and developer state

`.gitignore`/container ignore and CI context avoid local `.env`, credentials, editor/cache directories and unrelated build outputs. Secret values may be provided to build steps only through mechanisms that do not persist them into layers/logs. Frontend public build configuration is reviewed separately from server secrets.

## INFRA-BUILD-103 — Runtime image contains only runtime needs

Use multi-stage/minimal runtime images where practical, non-root runtime identity, deterministic entrypoint and explicit health behavior. Development SDKs/test fixtures/secret files do not remain in final image unless operationally required. Base images/dependencies are scanned and upgraded through the dependency policy.

## Version identity

Artifacts carry source SHA/build identity so logs/deployments can be correlated to the exact revision whose gates passed. Avoid mutable “latest” as the only production identity. Frontend static bundles similarly have immutable/versioned deployment identity for rollback/cache invalidation.

## CI ordering

Docker/package build is downstream from required restore/quality/architecture/core/platform/API/integration/frontend/contract gates according to the repository workflow. A successful image build cannot compensate for skipped tests. Required jobs assert non-zero execution for critical suites.

## Verification

Build from clean checkout, inspect final artifact/image contents and run production-like startup/smoke tests. When build definition changes, test at least one representative runtime composition and confirm cache optimization does not reuse stale generated/lock outputs incorrectly.
