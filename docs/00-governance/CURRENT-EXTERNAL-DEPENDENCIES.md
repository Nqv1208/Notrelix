---
title: "Current External and Organizational Decisions"
document_class: context
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Current External / Organizational Decisions

Do not invent these values in implementation/documentation merely to eliminate a TODO.

| Decision | Why external | Safe current behavior |
|---|---|---|
| numeric SLI/SLO/error-budget targets | requires product/operations capacity decision | instrument measurable signals; do not hard-code fake objectives |
| RPO/RTO targets | requires business risk/backup policy | preserve recoverability and test restore mechanics |
| final CODEOWNERS handles/team names | organization ownership data | use logical owner labels in docs until actual handles are approved |
| native authenticated API/session details if not contractually frozen | security/product contract | mobile stops at approved current contract; no invented token/storage scheme |
| long-term retention windows | legal/product/compliance decision | keep classification/retention mechanism explicit and configurable |
