---
title: "Incident Report Template"
document_class: template
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Incident Report Template

## Summary / impact
Affected capability/tenants, start/end/detection time, data/security impact and user-visible behavior.

## Timeline
UTC/local timestamps for detection, containment, hypotheses, mitigations, recovery and verification.

## Root cause
Technical cause plus why the system allowed it; separate contributing factors from trigger.

## Detection/control gaps
Which SLI/alert/test/gate/runbook should have caught or limited it?

## Recovery
What was changed, rollback/forward-fix/data repair, how correctness/tenant safety was verified.

## Follow-ups
Concrete owner, priority and acceptance proof. Do not use vague “be more careful” actions.
