---
title: "Coding Agent Execution Contract"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Coding Agent Execution Contract

This contract defines the minimum deterministic workflow for an automated coding agent operating in Notrelix.

## QLT-AGENT-101 — Resolve authority before editing

Read root `AGENTS.md` and `RULE.md`, nearest scoped `AGENTS.md`, owning canonical topic docs and relevant bounded-context semantics. Source is evidence, not permission to ignore a contrary normative decision.

## QLT-AGENT-102 — Inspect the full change surface

Before modifying code inventory callers/consumers, public contracts, persistence/migrations, events/realtime, authorization/tenant scope and tests/gates. Search the repository rather than assuming a symbol is isolated.

## QLT-AGENT-103 — Do not invent material decisions

If product semantics, ownership, security policy or compatibility strategy is truly unspecified, use the narrowest reversible implementation only when the request permits it; otherwise record the blocker/decision need. Do not choose a new bounded context, lifecycle rule or authorization model silently.

## QLT-AGENT-104 — Smallest complete transaction

Implement code, required migration/generated artifacts, tests, docs and gates together when they form one semantic change. Do not “fix” tests by weakening assertions or suppressing architecture checks.

## QLT-AGENT-105 — Completion report is evidence-based

Report scope/owner, changed contracts/schema/events, exact tests/gates run and remaining approved blockers/exceptions. Never claim validation that was not executed.

Agent-specific provider files/skills may add workflow ergonomics but cannot relax repository rules or canonical architecture.
