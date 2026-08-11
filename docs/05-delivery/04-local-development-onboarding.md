---
title: "Local Development and Onboarding"
document_class: handbook
normative: true
owner: engineering-delivery
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Local Development and Onboarding

The repository README routes setup; this document defines onboarding quality.

A new engineer should be able to discover prerequisites, restore/install dependencies, configure non-secret local settings, start required infrastructure, run backend/frontend hosts, apply migrations/codegen and execute focused/full quality gates without private tribal instructions.

## DLV-DEV-101 — Setup is reproducible

Canonical package managers/tool versions are declared by repository manifests. Setup commands do not rely on globally installed mutable tools when a pinned local tool exists. Example secrets are placeholders/test values only.

## DLV-DEV-102 — Local shortcuts cannot redefine architecture

In-memory provider/mocked auth/dev bypasses must be explicit and incapable of silently becoming production defaults. Local data bootstrap should preserve multi-tenant assumptions sufficiently to exercise scope boundaries.

Onboarding docs are updated in the same change when prerequisites/commands materially change.


## Minimum reproducible workflow

Onboarding should identify the repository-selected .NET SDK/tooling, Node/pnpm version, required dependency services, local configuration/secret placeholders, database migration/bootstrap command, contract/codegen command and the commands that correspond to focused vs repository validation. Scripts should fail with actionable messages when prerequisites are absent.

## Local data

Seed/sample data should exercise more than one workspace/account when tenant-sensitive behavior is under development. Avoid a single global developer tenant that makes missing scope appear correct. Destructive reset commands must be clearly local-only and never share production connection defaults.

## Drift prevention

When a command or toolchain changes, update README/onboarding plus CI/generator configuration in the same change. Do not maintain a second wiki-only setup path that can diverge from repository automation.
