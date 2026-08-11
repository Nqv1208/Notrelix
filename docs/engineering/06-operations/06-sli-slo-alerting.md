---
title: "SLI, SLO and Alerting Model"
document_class: handbook
normative: true
owner: operations
maturity: FROZEN
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# SLI, SLO and Alerting Model

This document defines the model, not unapproved numeric objectives.

## OPS-SLO-101 — SLI follows user-visible capability

Candidate SLIs include successful authenticated API request rate/latency, critical mutation success excluding valid client rejection, realtime delivery/convergence delay, background processing age/backlog, web/mobile critical journey availability and data freshness. Dependency metrics are diagnostics, not substitutes for user-impact SLIs.

## SLO approval
Numerical targets, windows, error budgets, RPO/RTO and paging thresholds require product/operations/business approval and deployment evidence. Until approved, keep them explicitly `TBD` rather than inventing enterprise-looking numbers.

## Alerting
Page on actionable symptoms with clear runbook/owner. Prefer burn/user-impact signals over noisy raw CPU. Ticket/non-page trends for capacity/debt. Every alert should state likely affected capability, diagnostic dashboard/log entry and immediate safe actions.


## Objective design

When numerical objectives are approved, define measurement source, numerator/denominator, valid exclusion (for example user validation rejection), aggregation window and service/capability scope. Do not use averages that hide tail latency or aggregate all endpoints when one critical mutation is failing.

## Error budgets

If error-budget policy is adopted, spending should influence rollout/risk decisions rather than become an excuse to ignore individual security/data incidents. Security, tenant isolation and data integrity failures can require immediate action regardless of availability budget.

## Alert validation

Every paging alert is testable in staging/synthetic or controlled failure where practical, has an owner/runbook and is reviewed for stale/noisy behavior after incidents/architecture changes.
