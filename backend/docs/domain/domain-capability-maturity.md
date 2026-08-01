# Domain Capability Maturity

> Documentation only. Not consumed by tests, snapshots, or build.

## Maturity levels

| Level | Meaning |
|---|---|
| **Stable** | Foundation is settled. Changes require contract review. |
| **Stabilizing** | Active hardening. Public surface may still adjust. |
| **Experimental** | Early exploration. May change without notice. |

## Capability status

| Capability | Status | Context | Reason | Promotion condition |
|---|---|---|---|---|
| Accounts | Stable | Accounts | Core tenant/account model settled | — |
| Identity | Stable | Identity | User/auth model settled | — |
| Workspaces | Stable | Workspaces | Workspace/member/space model settled | — |
| WorkManagement.Boards | Stable | WorkManagement | Board/field/item/group core settled | — |
| WorkManagement.Views | Stable | WorkManagement | View configuration settled | — |
| WorkManagement.Relations | Stable | WorkManagement | Relation model settled | — |
| Documents | Stable | Documents | Page/block tree settled | — |
| Collaboration.Comments | Stable | Collaboration | Comment lifecycle settled | — |
| Governance | Stable | Governance | Permission/audit model settled | — |
| Billing | Stable | Billing | Plan/subscription model settled | — |
| Analytics | Stable | Analytics | Reporting/dashboard model settled | — |
| Automation.Rules | Stable | Automation | Rule definition/activation settled | — |
| Automation.Scheduled | Stabilizing | Automation | Scheduling model still hardening | Production usage validation |
| Automation.Templates | Stabilizing | Automation | Template model still hardening | Production usage validation |
| Collaboration.Reactions | Stabilizing | Collaboration | Reaction model still hardening | Production usage validation |
| Collaboration.Watchers | Stabilizing | Collaboration | Watcher model still hardening | Production usage validation |
| Integrations.Calendar | Stabilizing | Integrations | Calendar sync model still hardening | Production usage validation |
| Integrations.Webhooks | Stabilizing | Integrations | Webhook delivery model still hardening | Production usage validation |
| Integrations.Sync | Stabilizing | Integrations | Sync engine model still hardening | Production usage validation |
| WorkManagement.Approvals | Experimental | WorkManagement | Early exploration | Design review + tests |
| WorkManagement.Formulas | Experimental | WorkManagement | Early exploration | Design review + tests |
| WorkManagement.Rollups | Experimental | WorkManagement | Early exploration | Design review + tests |
| WorkManagement.Workload | Experimental | WorkManagement | Early exploration | Design review + tests |
| Collaboration.Presence | Experimental | Collaboration | Early exploration | Design review + tests |

## Rules

- New namespaces do not require registry code.
- Status changes are a documentation decision, not a code change.
- Experimental capabilities may evolve independently.
- Stable capabilities follow the contract change procedure in `domain-foundation.md`.
