---
title: "Logical Ownership Matrix"
document_class: handbook
normative: true
owner: engineering-delivery
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Logical Ownership Matrix

| Area | Canonical logical owner | Primary authority |
|---|---|---|
| repository/system architecture | architecture | `00-governance`, `01-system` |
| backend Domain/Application/Infrastructure/API | backend architecture + owning product context | `02-backend`, `08-product/contexts` |
| platform messaging/idempotency | backend-platform | `02-backend/09-*` |
| frontend package/state/realtime | frontend architecture + owning product context | `03-frontend`, `08-product/contexts` |
| Work Management | work-management | `08-product/contexts/work-management.md` |
| Documents | documents | `08-product/contexts/documents.md` |
| Collaboration | collaboration | `08-product/contexts/collaboration.md` |
| Automation | automation | `08-product/contexts/automation.md` |
| Identity/Accounts/Workspaces/Governance | matching context owner | matching context doc |
| Billing/Integrations/Analytics | matching context owner | matching context doc |
| testing/quality/security standards | engineering-quality / security | `04-quality`, `01-system/03,14` |
| CI/release | engineering-delivery | `04-quality`, `05-delivery` |
| runtime operations/infrastructure | operations/infrastructure | `06-operations`, `07-infrastructure` |

This is logical ownership, not a CODEOWNERS file. Do not invent GitHub users/teams. When real handles are approved, `.github/CODEOWNERS` becomes executable review ownership and this matrix remains semantic ownership.
