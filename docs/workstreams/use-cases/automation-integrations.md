---
document_id: WRK-UC-AUTOMATION-INTEGRATIONS
document_type: workstream-use-case-catalog
status: active
owner: automation-integrations-team
applies_to:
  - automation
  - integrations
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/product/automation.md
  - docs/product/integrations.md
  - docs/workstreams/teams/automation-integrations.md
  - docs/workstreams/executions/automation-integrations/automation-integrations.spec.md
  - docs/workstreams/use-cases/README.md
review_on:
  - capability-state-change
  - product-semantic-change
  - team-capability-decomposition-change
---

# Use cases: Automation and Integrations

This file follows [README.md](README.md). It holds two contexts, each with its own capability letters: Automation (code AUT) and Integrations (code INT). Product meaning stays in [automation.md](../../product/automation.md) and [integrations.md](../../product/integrations.md). The capability grouping comes from [the team spec](../teams/automation-integrations.md). Requirement ids (AIREQ...) belong to [the execution spec](../executions/automation-integrations/automation-integrations.spec.md); the certified evidence is in [the certification](../executions/automation-integrations/automation-integrations.certification.md) and [the test plan](../executions/automation-integrations/automation-integrations.tests.md).

Test path shorthand used in Evidence lines:

- T-AD = `backend/tests/Notrelix.Domain.Tests/Automation/**`
- T-AA = `backend/tests/Notrelix.Application.Tests/Features/Automation/**`
- T-AI = `backend/tests/Notrelix.Integration.Tests/Automation/**` and `Messaging/AutomationWorkEventRuntimeChainIntegrationTests.cs`
- T-ID = `backend/tests/Notrelix.Domain.Tests/Integrations/**`
- T-II = `backend/tests/Notrelix.Integration.Tests/Integrations/**`, `Messaging/CalendarWebhookProcessingRuntimeTests.cs`, `Messaging/WebhookFanOutRuntimeChainIntegrationTests.cs`
- FE-BS = `frontend/packages/product/work-management/web/src/components/board-settings/` (the real board automation UI)
- FE-WA = `frontend/packages/product/work-management/state/src/api/workspace-automations.api.ts`
- FE-INT = `frontend/apps/web/src/routes/workspaces/$workspaceId/integrations.tsx` and `integrations/`
- E2E-AUT = `frontend/e2e/real/automation.real.e2e.spec.ts`; E2E-INT = `frontend/e2e/real/integrations.real.e2e.spec.ts`; MOCK-INT = `frontend/e2e/mock/integrations.mock.e2e.spec.ts`

Design artboards (canvas `18L4jUXGfdXh7Wn9YQvA2x`): `Final-Automation` (rule editor, validation preview, run log, executor), `Final-M-Automation` (phone: rules, run log, rerun), `Final-Integrations` (connection list and detail, reauthorization, held operations), `Ecosystem` (where Automation and Integrations sit).

# Part 1. Assessment

## 1.1 Summary

Automation runs end to end for board rules whose trigger is a Work Management fact (item created, moved to a group, member assigned) and whose actions are MoveItem, ChangeStatus, AssignPerson, UpdateField, NotifyMember, SendEmail or an n8n Webhook. Rules are workspace routes with a `boardId`, run as one bound executor, take a new version on every edit, and write an execution with steps. One real e2e spec proves create, enable, trigger, edit and delete on a board. Schedules, templates, agents, manual runs, rerun of failed steps, realtime progress and a workspace-wide run feed exist only as domain code, mock handlers or design.

Integrations runs end to end for the outgoing webhook (connect with one-time signing secret, test delivery, pause, resume, disconnect, SSRF guard) and has a real OAuth redirect flow for Google Calendar and Slack that is proven only up to "not configured". The calendar webhook intake is verified, deduplicated and tenant-routed on the backend. Calendar sync itself (connect binding, manual sync, cursor, backfill, conflict) is mostly domain code; the manual sync handler throws `NotImplementedException`. There is no marketplace or plugin concept in the product docs.

No use case is DONE: none has an owner check or a 1 px design diff.

## 1.2 Capabilities and counts

Automation (letters follow the team spec order AUT-001 to AUT-014):

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | AUT-001 Rule lifecycle | 8 | 0 | 3 | 1 | 4 | 0 | 0 |
| B | AUT-002 Trigger model | 8 | 2 | 3 | 2 | 1 | 0 | 0 |
| C | AUT-003 Condition model | 3 | 1 | 1 | 1 | 0 | 0 | 0 |
| D | AUT-004 Action model | 10 | 0 | 3 | 1 | 5 | 0 | 1 |
| E | AUT-005 Enable/disable lifecycle | 7 | 1 | 2 | 2 | 2 | 0 | 0 |
| F | AUT-006 Source-event subscription/matching | 3 | 0 | 0 | 1 | 2 | 0 | 0 |
| G | AUT-007 Execution creation | 6 | 1 | 1 | 3 | 1 | 0 | 0 |
| H | AUT-008 Execution state machine | 5 | 0 | 2 | 1 | 2 | 0 | 0 |
| I | AUT-009 Business retry/failure policy | 4 | 0 | 2 | 2 | 0 | 0 | 0 |
| J | AUT-010 Execution history/query | 5 | 2 | 1 | 1 | 1 | 0 | 0 |
| K | AUT-011 Automation authoring frontend | 8 | 1 | 3 | 2 | 2 | 0 | 0 |
| L | AUT-012 Realtime execution status | 2 | 0 | 2 | 0 | 0 | 0 | 0 |
| M | AUT-013 Entitlement integration | 4 | 1 | 1 | 2 | 0 | 0 | 0 |
| N | AUT-014 Hardening/observability | 6 | 2 | 2 | 2 | 0 | 0 | 0 |
| | Total | 79 | 11 | 26 | 21 | 20 | 0 | 1 |

Integrations (letters follow INT-001 to INT-014):

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | INT-001 Connector catalog | 3 | 2 | 0 | 0 | 1 | 0 | 0 |
| B | INT-002 Connection lifecycle | 7 | 1 | 2 | 0 | 4 | 0 | 0 |
| C | INT-003 Provider authorization/OAuth | 5 | 1 | 1 | 3 | 0 | 0 | 0 |
| D | INT-004 Connection configuration | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| E | INT-005 Credential reference/storage contract | 5 | 0 | 2 | 2 | 1 | 0 | 0 |
| F | INT-006 Outbound provider operation | 7 | 2 | 3 | 1 | 1 | 0 | 0 |
| G | INT-007 Inbound webhook verification | 4 | 0 | 0 | 4 | 0 | 0 | 0 |
| H | INT-008 Inbound event mapping | 8 | 1 | 7 | 0 | 0 | 0 | 0 |
| I | INT-009 Connection health | 4 | 2 | 1 | 0 | 1 | 0 | 0 |
| J | INT-010 Provider failure normalization | 1 | 0 | 0 | 1 | 0 | 0 | 0 |
| K | INT-011 Integration management frontend | 4 | 1 | 1 | 0 | 2 | 0 | 0 |
| L | INT-012 Rate/retry/provider limits | 2 | 0 | 2 | 0 | 0 | 0 | 0 |
| M | INT-013 Entitlement integration | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| N | INT-014 Hardening/security | 2 | 1 | 0 | 0 | 1 | 0 | 0 |
| | Total | 55 | 13 | 20 | 11 | 11 | 0 | 0 |

Calendar sync and mapping use cases sit in INT capability H (Inbound event mapping) because the team spec has no separate sync capability. Marketplace and plugin use cases sit in INT capability A (Connector catalog).

## 1.3 The ten most important gaps

1. Competing writes carry no expected version on rule edit and enable/disable: a stale editor silently overwrites a newer rule (UC-AUT-E01, UC-AUT-A04).
2. Only three trigger facts have a consumer (ItemCreated, ItemMovedToGroup, ItemAssigned). ItemUpdated, FieldChanged, ItemDeleted, FormSubmitted and ScheduleTrigger validate and save, then never fire (UC-AUT-B04, UC-AUT-B05).
3. `SlackMessage` is accepted, enabled and counted as an integration action but has no executor and no Integrations operation behind it (UC-AUT-D09, UC-INT-F03).
4. No scheduler runtime: Schedule intent, time zone and missed-run policy exist as design and `ScheduledJob` domain code only (UC-AUT-B05, UC-AUT-K08).
5. No rerun of a failed step, no cancel, no workspace run feed, no realtime producer for `automation.execution.*` (UC-AUT-I03, UC-AUT-H04, UC-AUT-J03, UC-AUT-L01).
6. Calendar sync is not reachable: `TriggerCalendarSyncCommandHandler` throws, no endpoint, no job, and an OAuth connection is not linked to a `CalendarIntegration` binding (UC-INT-H01, UC-INT-H04, UC-INT-H05).
7. Provider authorization has no PKCE, no reauthorization UI, no scope-upgrade re-consent, and disconnect does not revoke the grant at the provider (UC-INT-C01, UC-INT-C04, UC-INT-C05, UC-INT-B04).
8. Outbound webhook retry is 3 attempts (1 s, 5 s, 30 s) against the owner decision of up to 8 with backoff; there is no delivery log, no unknown-outcome reconciliation, no secret regeneration (UC-INT-F05, UC-INT-F06, UC-INT-F07, UC-INT-E02).
9. Frontend contract islands: `frontend/packages/product/automation/**` models a different vocabulary (`card_status_changed`, `archive_card`), shows demo rules and has a placeholder mobile screen; `frontend/packages/features/integrations/**` calls endpoints that do not exist. The real UI lives in Work Management board settings and the web integrations route (UC-AUT-K06, UC-INT-K01).
10. No plan-downgrade policy, no audit of rule and connection changes in Governance, no admin pause-all switch, no failure notification to admins (UC-AUT-M03, UC-AUT-J05, UC-AUT-N06, UC-INT-I04, UC-INT-N02).

## 1.4 Defects in shipped code

| # | Defect | Where |
|---|---|---|
| DEF-1 | Enable/disable has no idempotency key and no expected version; the stored `ConfigurationVersion` is returned but never checked on write | `backend/src/Notrelix.API/Endpoints/Automation/Rules/Commands/SetAutomationRuleEnabledEndpoint.cs`, `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/SetAutomationRuleEnabled/SetAutomationRuleEnabled.cs` |
| DEF-2 | Rule edit has an idempotency key but no expected version: last write wins | `.../Rules/Commands/UpdateAutomationRule/UpdateAutomationRule.cs` |
| DEF-3 | Rule list is unbounded; execution list pages by offset with no total or cursor | `.../Rules/Queries/GetWorkspaceAutomations/GetWorkspaceAutomations.cs`, `.../Executions/Queries/GetAutomationExecutions/GetAutomationExecutions.cs` |
| DEF-4 | `SlackMessage` validates and enables but never runs | `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationActionValidator.cs`, `.../Rules/Services/AutomationActionKinds.cs` |
| DEF-5 | Triggers with no consumer: ItemUpdated, FieldChanged, ItemDeleted, FormSubmitted, ScheduleTrigger | `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationTriggerValidator.cs`, `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/` |
| DEF-6 | Create consumes plan capacity while the rule is still a Draft; owner decision says only enabled rules count | `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs` |
| DEF-7 | Dead code: `TriggerCalendarSyncCommandHandler` throws `NotImplementedException`; `CalendarSyncJob` has no user; its permission marker is ManageWorkspaceSettings while all other integration commands use ManageIntegrations | `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/TriggerCalendarSync/TriggerCalendarSync.cs`, `.../Calendar/Jobs/CalendarSyncJob.cs` |
| DEF-8 | Disconnect revokes the connection locally but never calls the provider to revoke the grant | `backend/src/Notrelix.Application/Features/Integrations/Connections/Commands/DisconnectIntegration/DisconnectIntegration.cs` |
| DEF-9 | OAuth start uses a protected `state` (10 minute lifetime) but no PKCE | `backend/src/Notrelix.Infrastructure/Integrations/Connections/IntegrationOAuthStateProtector.cs`, `IntegrationOAuthClient.cs` |
| DEF-10 | Connect webhook has no idempotency key; a retry after a lost response creates a second connection and the first one-time secret is lost | `backend/src/Notrelix.API/Endpoints/Integrations/Connections/IntegrationConnectionEndpoints.cs` |
| DEF-11 | Calendar connect takes a raw `AccessToken` in the request body and has no frontend; calendar disconnect and connect are not idempotent | `backend/src/Notrelix.API/Endpoints/Integrations/Calendar/CalendarEndpoints.cs`, `.../Calendar/Commands/ConnectCalendar/ConnectCalendar.cs` |
| DEF-12 | Outbound webhook redelivery gives up after intervals of 1 s, 5 s, 30 s | `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Integrations/IntegrationWebhookDispatchRequestedConsumerDefinition.cs` |
| DEF-13 | Mock-only or dead frontend: `AutomationsTab` shows built-in demo rules; `MobileAutomationScreen` is a title and workspace id; `AutomationRuleRepository` has no production adapter; `integrations.service.ts` calls `/webhooks` and `/connections` routes that the backend does not have | `frontend/packages/product/automation/web/src/components/automations-tab.tsx`, `.../mobile/src/components/mobile-automation-screen.tsx`, `.../state/src/data/repositories.ts`, `frontend/packages/features/integrations/src/core/api/integrations.service.ts` |
| DEF-14 | The board rule UI cannot show or edit Webhook, SlackMessage or CreateItem actions (`toAction` returns null for them) | `frontend/packages/product/work-management/state/src/api/workspace-automations.api.ts` |
| DEF-15 | Realtime adapter for `automation.execution.*` exists but no backend producer publishes those events | `frontend/packages/product/automation/state/src/realtime/execution-adapter.ts` |
| DEF-16 | Execution `Skipped` does not exist in `AutomationExecutionStatus` (Queued, Running, Succeeded, Failed, Cancelled), although design and AUT-011 show "condition not met" as a skipped run | `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecutionStatus.cs` |

Authorization is applied in the pipeline for every Automation and connection endpoint (marker `IRequirePermission`); no endpoint was found without a permission marker. The calendar webhook route is anonymous by design and is signature-authenticated.

## 1.5 Enterprise baseline

Compared with Jira Automation (rule actor, audit log with failed and queued-for-retry statuses, execution limits per plan; community and tracker sources: [Atlassian community thread on retries](https://community.atlassian.com/forums/Automation-articles/Retrying-automation-rules-for-enhanced-reliability/ba-p/2960443), [Atlassian tracker AUTO-1171](https://jira.atlassian.com/browse/AUTO-1171)), Slack (signed requests, OAuth scopes granted by the installer: [Verifying requests from Slack](https://api.slack.com/docs/verifying-requests-from-slack), [Slack authentication](https://api.slack.com/authentication)), and the common pattern of Zapier, Microsoft Power Automate and ServiceNow (rule owner, run history with per-step results, re-run of failed runs, connection owner and reauthorization):

| Baseline capability | Notrelix today | Catalog use case |
|---|---|---|
| Rule runs as a named actor and stops when the actor loses access | Done in the backend (executor, auto-disable with reason) | UC-AUT-E03, UC-AUT-G02 |
| Run history with status per step and the rule version used | Done (steps, `ruleVersion`) | UC-AUT-J01, UC-AUT-J02 |
| Statuses include skipped and queued-for-retry | Missing `Skipped`; retry state is not shown | UC-AUT-C02, UC-AUT-I01 |
| Re-run a failed run or step | Missing | UC-AUT-I03 |
| Workspace-wide run feed with filters | Missing | UC-AUT-J03 |
| Per-plan limits and a visible limit state | Backend gate exists | UC-AUT-M01 to UC-AUT-M04 |
| Admin kill switch for all rules | Missing | UC-AUT-N06 |
| Audit of rule changes in the workspace audit log | Missing | UC-AUT-J05 |
| Connections owned by an admin, reauthorization without losing config | Backend merges on reauthorize; no UI | UC-INT-C04 |
| Signed outbound webhooks with retry, delivery log, secret rotation | Signed, 3 retries, no log, no rotation | UC-INT-F01, UC-INT-F05, UC-INT-F06, UC-INT-E02 |
| Signature-verified inbound webhooks with replay protection | Done for calendar | UC-INT-G01 to UC-INT-G04 |
| Least-privilege scopes and re-consent on scope change | Scopes fixed; no re-consent | UC-INT-C05 |
| App directory or marketplace with admin allow-list | Not in the product docs | UC-INT-A03 |
| Failure notices to admins when a connection breaks | Missing | UC-INT-I04 |
| Audit of connect and disconnect | Missing | UC-INT-N02 |

# Part 2. Index

Automation capability letters (team spec capability, in listed order): A Rule lifecycle (AUT-001), B Trigger model (AUT-002), C Condition model (AUT-003), D Action model (AUT-004), E Enable/disable lifecycle (AUT-005), F Source-event subscription and matching (AUT-006), G Execution creation (AUT-007), H Execution state machine (AUT-008), I Business retry and failure policy (AUT-009), J Execution history and query (AUT-010), K Automation authoring frontend (AUT-011), L Realtime execution status (AUT-012), M Entitlement integration (AUT-013), N Hardening and observability (AUT-014).

Integrations capability letters: A Connector catalog (INT-001), B Connection lifecycle (INT-002), C Provider authorization and OAuth (INT-003), D Connection configuration (INT-004), E Credential reference and storage (INT-005), F Outbound provider operation (INT-006), G Inbound webhook verification (INT-007), H Inbound event mapping, with calendar sync (INT-008), I Connection health (INT-009), J Provider failure normalization (INT-010), K Integration management frontend (INT-011), L Rate, retry and provider limits (INT-012), M Entitlement integration (INT-013), N Hardening and security (INT-014).

Column values: BE, FE, Design and Real e2e use Y (present), P (partial) or N (none).

## Automation index

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-AUT-A01 | Create a board rule | A Rule lifecycle | board Manager (ManageBoard on the board) | Y | Y | Y | Y | HARDENING | no cross-tenant negative test on create found; no axe or 1 px design proof; no owner check |
| UC-AUT-A02 | Create a workspace-level rule (no board) | A Rule lifecycle | workspace Owner and Admin (ManageWorkspaceSettings) | Y | N | N | N | IMPLEMENTING | backend only; no UI, no e2e |
| UC-AUT-A03 | List the rules of a board or workspace | A Rule lifecycle | board Manager (ManageBoard) with `boardId` | Y | Y | Y | Y | HARDENING | unbounded read (DEF-3); no pagination contract; no cross-tenant test |
| UC-AUT-A04 | Edit a rule into a new version | A Rule lifecycle | as A01 | Y | Y | Y | Y | HARDENING | no expected version (DEF-2); no no-op proof; no conflict UI |
| UC-AUT-A05 | Delete a rule | A Rule lifecycle | as A01 (ManageBoard or ManageWorkspaceSettings) | Y | Y | Y | Y | HARDENING | pending executions and scheduled jobs on delete are not proven; no purge job (J04) |
| UC-AUT-A06 | Save a rule as a draft | A Rule lifecycle | as A01 | Y | Y | Y | N | INTEGRATING | capacity rule conflicts with the decision (DEF-6); no e2e for draft |
| UC-AUT-A07 | Restore a deleted rule | A Rule lifecycle | board Manager or Owner and Admin, same as delete | P | N | N | N | IMPLEMENTING | domain method only; no command, endpoint or UI |
| UC-AUT-A08 | Create a rule from a template | A Rule lifecycle | as A01 | P | N | N | N | IMPLEMENTING | domain and table only; deferred by decision |
| UC-AUT-B01 | Fire a rule when an item is created | B Trigger model | set up by a board Manager (A01) | Y | Y | Y | N | INTEGRATING | the real e2e covers only the moved trigger |
| UC-AUT-B02 | Fire a rule when an item moves to a group | B Trigger model | as B01 | Y | Y | Y | Y | HARDENING | no axe or design diff; no owner check |
| UC-AUT-B03 | Fire a rule when a person is assigned | B Trigger model | as B01 | Y | P | Y | N | INTEGRATING | no real e2e |
| UC-AUT-B04 | Triggers on update, field or status change, delete, form submit | B Trigger model | as A01 | P | P | Y | N | IMPLEMENTING | validators only; cannot run end to end |
| UC-AUT-B05 | Run a rule on a schedule | B Trigger model | as A01 | P | P | Y | N | IMPLEMENTING | no scheduler runtime; the validator accepts cron or interval while the UI sends daily or weekly |
| UC-AUT-B06 | Run a rule by hand on an item | B Trigger model | any actor who can edit the item | N | N | N | N | DEFINED | deferred by the owner until the item-page contract exists |
| UC-AUT-B07 | An unknown or changed trigger type fails closed | B Trigger model | system | Y | N | N | N | IMPLEMENTING | domain proof only; no UI for invalid rules |
| UC-AUT-B08 | Start a rule from an inbound provider event | B Trigger model | system, after Integrations has verified and translated the event | N | N | N | N | DEFINED | deferred until verified provider intake exists |
| UC-AUT-C01 | Add all-of equals conditions | C Condition model | as A01 | Y | Y | Y | N | INTEGRATING | the real e2e has no condition; no condition-false end-to-end proof |
| UC-AUT-C02 | A condition not met is shown as skipped, not failed | C Condition model | any reader of run history (J01 permission) | P | P | Y | N | IMPLEMENTING | no Skipped status or record |
| UC-AUT-C03 | Actor and time conditions, OR and nesting | C Condition model | as A01 | N | N | N | N | DEFINED | deferred by decision OD4 |
| UC-AUT-D01 | Move the item to a group | D Action model | executor (bound creator) holding the MoveItem decision on the item | Y | Y | Y | Y | HARDENING | no owner check; no 1 px diff |
| UC-AUT-D02 | Change the item status | D Action model | executor with edit permission on the item | Y | Y | Y | Y | HARDENING | as D01 |
| UC-AUT-D03 | Assign a person | D Action model | executor with permission to assign on the item | Y | Y | Y | Y | HARDENING | as D01 |
| UC-AUT-D04 | Set a field value | D Action model | executor with edit permission on the item | Y | Y | Y | N | INTEGRATING | no real e2e; the frontend sends the value as a string for every field type |
| UC-AUT-D05 | Notify a member or send an email | D Action model | executor | Y | Y | Y | Y | HARDENING | the email delivery provider is not proven in a real run |
| UC-AUT-D06 | Run up to five actions in order | D Action model | as A01 | Y | Y | Y | Y | HARDENING | the "Not run" step state is not in the DTO (steps map to succeeded or failed only) |
| UC-AUT-D07 | Create an item | D Action model | executor with create permission on the target board | P | N | N | N | IMPLEMENTING | validates and saves, never runs; must be refused until an executor exists |
| UC-AUT-D08 | Call an n8n webhook | D Action model | ManageIntegrations on create and enable (workspace path) | Y | N | N | N | IMPLEMENTING | no UI to author or see it; no real e2e |
| UC-AUT-D09 | Send a Slack message | D Action model | ManageIntegrations | P | N | N | N | BLOCKED | waits on UC-INT-F03 (Slack outbound operation); today the rule saves and never runs (DEF-4) |
| UC-AUT-D10 | A deleted or archived target blocks enable or fails the step | D Action model | the actor enabling | P | P | Y | N | IMPLEMENTING | the enable check covers `rule.Configuration.Action` (first action) only; no structured broken-reference error |
| UC-AUT-E01 | Enable a rule | E Enable/disable lifecycle | board Manager (ManageBoard) | Y | Y | Y | Y | HARDENING | no expected version or idempotency (DEF-1) |
| UC-AUT-E02 | Disable a rule | E Enable/disable lifecycle | ManageBoard or ManageWorkspaceSettings only (kill switch | Y | Y | Y | Y | HARDENING | as E01 |
| UC-AUT-E03 | A rule stops when its runner loses access | E Enable/disable lifecycle | system reacting to Workspaces facts | Y | P | Y | N | INTEGRATING | no real e2e |
| UC-AUT-E04 | Change the person a rule runs as | E Enable/disable lifecycle | a board Manager picks a person who can edit the board | P | P | Y | N | IMPLEMENTING | no explicit runner command; implicit takeover only |
| UC-AUT-E05 | A rule stops after repeated failures | E Enable/disable lifecycle | system | Y | P | N | N | INTEGRATING | no notice to the owner when it happens |
| UC-AUT-E06 | Pause and resume rules with the board | E Enable/disable lifecycle | system on board archive and unarchive | N | N | N | N | DEFINED | no consumer |
| UC-AUT-E07 | Check a rule before enabling it | E Enable/disable lifecycle | as E01 | N | P | Y | N | IMPLEMENTING | mock-only UI path; no backend check endpoint |
| UC-AUT-F01 | One fact starts each matching rule once | F Source-event subscription/matching | system | Y | N | N | Y | HARDENING | a durable uniqueness constraint (AIREQ028) is not confirmed in the migrations |
| UC-AUT-F02 | Filter by group on the trigger | F Source-event subscription/matching | as A01 | Y | Y | Y | Y | HARDENING | as B02 |
| UC-AUT-F03 | A fact from another workspace never matches | F Source-event subscription/matching | system | Y | N | N | N | INTEGRATING | no dedicated cross-tenant integration test for the Automation consumers found |
| UC-AUT-G01 | Each execution freezes the rule version | G Execution creation | system | Y | Y | Y | Y | HARDENING | as A04 |
| UC-AUT-G02 | The runner is re-checked on every execution | G Execution creation | system as executor | Y | N | Y | N | INTEGRATING | no real e2e |
| UC-AUT-G03 | Chains and runaway rules are bounded | G Execution creation | system | Y | N | N | N | INTEGRATING | a stopped trigger is silent to users |
| UC-AUT-G04 | Concurrent claims never duplicate work | G Execution creation | system | P | N | N | N | IMPLEMENTING | claim proven for n8n only; MoveItem and work actions rely on operation ids |
| UC-AUT-G05 | An automated change names the rule and the runner | G Execution creation | any viewer of the changed item | Y | P | Y | N | INTEGRATING | the rule label in the item history UI is not proven |
| UC-AUT-G06 | Test-run a rule | G Execution creation | as A01 | N | P | N | N | DEFINED | deferred by the P5 decisions |
| UC-AUT-H01 | An execution moves Queued, Running, then ends | H Execution state machine | system | Y | Y | Y | Y | HARDENING | as D01 |
| UC-AUT-H02 | See each step of a run | H Execution state machine | as J01 | Y | Y | Y | Y | HARDENING | step status is stored in the `LastResponse` JSON, not in `AutomationExecutionStep` rows |
| UC-AUT-H03 | Partial failure keeps what already happened | H Execution state machine | as J01 | Y | P | Y | N | INTEGRATING | no real e2e of a failing step |
| UC-AUT-H04 | Cancel a queued execution | H Execution state machine | board Manager or Owner and Admin | P | N | N | N | IMPLEMENTING | domain only |
| UC-AUT-H05 | An unknown outcome is held, never re-fired | H Execution state machine | system | Y | N | N | N | IMPLEMENTING | no UI state; no governed reconciliation action |
| UC-AUT-I01 | Retryable failures retry within one budget | I Business retry/failure policy | system | Y | N | N | N | INTEGRATING | no UI for the retrying state |
| UC-AUT-I02 | A terminal failure shows a safe, useful reason | I Business retry/failure policy | as J01 | Y | Y | Y | N | INTEGRATING | failure classes are free text, not a stable enum |
| UC-AUT-I03 | Rerun a failed step | I Business retry/failure policy | board Manager | N | P | Y | N | IMPLEMENTING | mock-only UI path |
| UC-AUT-I04 | A gone target or revoked permission is terminal | I Business retry/failure policy | system | P | N | N | N | IMPLEMENTING | the terminal versus retryable class is not a stable contract |
| UC-AUT-J01 | Read the recent runs of a rule | J Execution history/query | ManageBoard (with `boardId`) or ManageWorkspaceSettings | Y | Y | Y | Y | HARDENING | offset paging, no total or cursor (DEF-3); the payload may hold item data and its redaction is not proven |
| UC-AUT-J02 | Open one run | J Execution history/query | as J01 | P | Y | Y | N | INTEGRATING | triggerer and item reference come from the payload only |
| UC-AUT-J03 | See all runs of a workspace and filter them | J Execution history/query | Owner and Admin (ManageWorkspaceSettings) | N | N | N | N | DEFINED | (enterprise baseline) |
| UC-AUT-J04 | Keep and purge history | J Execution history/query | system | P | N | N | N | IMPLEMENTING | read cutoff only; no purge |
| UC-AUT-J05 | Rule changes appear in the workspace audit log | J Execution history/query | Owner and Admin read the audit log | N | N | N | N | DEFINED | (enterprise baseline) |
| UC-AUT-K01 | See the rules of a board | K Automation authoring frontend | board Manager | Y | Y | Y | Y | HARDENING | scope-transition behaviour not proven |
| UC-AUT-K02 | Compose a rule: trigger, conditions, actions | K Automation authoring frontend | board Manager | Y | Y | Y | Y | HARDENING | no conflict state; no axe proof; 1 px diff not measured |
| UC-AUT-K03 | Read the run log of a rule | K Automation authoring frontend | as J01 | Y | Y | Y | P | INTEGRATING | the real e2e reads the list but does not assert the log screen |
| UC-AUT-K04 | See why a rule cannot be enabled | K Automation authoring frontend | board Manager | P | P | Y | N | IMPLEMENTING | structured reasons are not in the API (see UC-AUT-D10) |
| UC-AUT-K05 | A non-manager sees a denied state | K Automation authoring frontend | Member, Guest, Viewer | Y | P | Y | N | INTEGRATING | no real e2e of the denied path |
| UC-AUT-K06 | Phone: read rules, switch them, read the run log | K Automation authoring frontend | board Manager | Y | P | Y | N | IMPLEMENTING | the screen is a placeholder |
| UC-AUT-K07 | Workspace automation page | K Automation authoring frontend | Owner and Admin | P | N | N | N | DEFINED | (enterprise baseline) |
| UC-AUT-K08 | Edit a schedule | K Automation authoring frontend | board Manager | N | P | Y | N | IMPLEMENTING | blocked on the B05 runtime |
| UC-AUT-L01 | Watch a run progress live | L Realtime execution status | as J01 | N | P | N | N | IMPLEMENTING | no backend producer |
| UC-AUT-L02 | Recover after a realtime gap | L Realtime execution status | as J01 | N | P | N | N | IMPLEMENTING | the keys exist in the island package; the real UI uses Work Management keys |
| UC-AUT-M01 | Creating a rule respects the plan limit | M Entitlement integration | as A01 | Y | P | N | N | INTEGRATING | no limit-reached UI |
| UC-AUT-M02 | Deleting a rule frees its slot | M Entitlement integration | as A05 | Y | N | N | N | INTEGRATING | the retry-after-delete path is not proven |
| UC-AUT-M03 | What happens to rules on a plan downgrade | M Entitlement integration | system on Billing facts | N | N | N | N | DEFINED | the rule text is in Part 5 |
| UC-AUT-M04 | Only enabled rules count toward the limit | M Entitlement integration | as A01 | P | N | N | N | IMPLEMENTING | code contradicts the decision |
| UC-AUT-N01 | Operators see backlog and failure metrics | N Hardening/observability | operators | N | N | N | N | DEFINED | no metrics, alerts or dashboards exist |
| UC-AUT-N02 | Follow one fact to its execution by correlation id | N Hardening/observability | operators | P | N | N | N | IMPLEMENTING | not asserted end to end |
| UC-AUT-N03 | A stuck provider claim is recovered under control | N Hardening/observability | operators with a governed, audited procedure | P | N | N | N | IMPLEMENTING | no operator action exists |
| UC-AUT-N04 | No reusable secret in a rule | N Hardening/observability | as A01 | Y | N | N | N | INTEGRATING | secret-like field rejection is not confirmed by a test |
| UC-AUT-N05 | Tenant isolation of rules and executions | N Hardening/observability | system | Y | N | N | N | INTEGRATING | no Automation-specific cross-tenant integration test with Postgres RLS found |
| UC-AUT-N06 | Pause all automation of a workspace | N Hardening/observability | Owner and Admin | N | N | N | N | DEFINED | (enterprise baseline) |

## Integrations index

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-INT-A01 | See what can be connected | A Connector catalog | workspace Owner and Admin (ManageIntegrations) | Y | Y | Y | Y | HARDENING | the list is unbounded (small set); no axe or 1 px proof |
| UC-INT-A02 | Show planned providers as "coming soon" | A Connector catalog | Owner and Admin | N | N | Y | N | DEFINED | no catalog flag, card or Billing gate exists |
| UC-INT-A03 | Marketplace and plugins (enterprise baseline) | A Connector catalog | Owner and Admin install | N | N | N | N | DEFINED | (enterprise baseline); needs a product decision to schedule |
| UC-INT-B01 | Connect an outgoing webhook | B Connection lifecycle | Owner and Admin (ManageIntegrations on the workspace) | Y | Y | Y | Y | HARDENING | no idempotency key (DEF-10); no cross-tenant negative test |
| UC-INT-B02 | Pause a connection | B Connection lifecycle | Owner and Admin | Y | Y | Y | Y | HARDENING | no expected version; no idempotency |
| UC-INT-B03 | Resume a connection | B Connection lifecycle | Owner and Admin | Y | Y | Y | Y | HARDENING | as B02 |
| UC-INT-B04 | Disconnect a connection | B Connection lifecycle | Owner and Admin | Y | Y | Y | Y | HARDENING | no provider-side revoke (DEF-8); no impact counts in the dialog (B05) |
| UC-INT-B05 | See the impact before disconnecting | B Connection lifecycle | Owner and Admin | N | N | Y | N | DEFINED | planned as slice 4 of the integrations plan |
| UC-INT-B06 | Connect again after a disconnect | B Connection lifecycle | Owner and Admin | P | N | N | N | IMPLEMENTING | provider account change confirmation is not built |
| UC-INT-B07 | Read one connection in detail | B Connection lifecycle | Owner and Admin | N | P | Y | N | IMPLEMENTING | no detail endpoint |
| UC-INT-C01 | Start provider authorization | C Provider authorization/OAuth | Owner and Admin (ManageIntegrations) | Y | Y | Y | P | INTEGRATING | no PKCE (DEF-9); no real provider round trip |
| UC-INT-C02 | Complete authorization on the callback | C Provider authorization/OAuth | the signed-in member returning from the provider | Y | Y | Y | N | INTEGRATING | no real e2e; no provider simulator |
| UC-INT-C03 | A refused or failed authorization is explained | C Provider authorization/OAuth | as C02 | Y | Y | P | N | INTEGRATING | no test of the error redirects |
| UC-INT-C04 | Reauthorize an expired connection | C Provider authorization/OAuth | Owner and Admin | P | N | Y | N | IMPLEMENTING | no `reauthorization-required` status and no UI |
| UC-INT-C05 | Ask for more provider permission | C Provider authorization/OAuth | Owner and Admin | N | N | Y | N | DEFINED | domain facts only |
| UC-INT-D01 | Change the address of an outgoing webhook | D Connection configuration | Owner and Admin | N | N | N | N | DEFINED | no endpoint or UI exists |
| UC-INT-D02 | Typed, versioned provider configuration | D Connection configuration | system | P | N | N | N | IMPLEMENTING | only the webhook target address is a typed field; no versioned configuration object |
| UC-INT-E01 | A signing secret is generated and shown once | E Credential reference/storage contract | Owner and Admin | Y | Y | Y | Y | HARDENING | the physical store depends on Data Protection key persistence (restart safety, AIREQ061) not proven in a restart test |
| UC-INT-E02 | Regenerate a webhook secret | E Credential reference/storage contract | Owner and Admin | P | N | Y | N | IMPLEMENTING | domain only |
| UC-INT-E03 | Tokens are stored behind an opaque reference | E Credential reference/storage contract | system | Y | N | N | N | INTEGRATING | no integration test of the OAuth token path |
| UC-INT-E04 | Refresh tokens before they expire | E Credential reference/storage contract | system | P | N | N | N | IMPLEMENTING | no refresh job |
| UC-INT-E05 | Retire the secret after a disconnect | E Credential reference/storage contract | system | Y | N | N | N | INTEGRATING | no direct test of the cleanup consumer |
| UC-INT-F01 | Deliver a signed webhook for a work fact | F Outbound provider operation | system on Work and Collaboration facts | Y | N | N | N | INTEGRATING | no real e2e of a fact-driven delivery |
| UC-INT-F02 | Send a test delivery | F Outbound provider operation | Owner and Admin | Y | Y | Y | Y | HARDENING | the result of the test is not shown to the user (only "queued") |
| UC-INT-F03 | Send a Slack message (outbound operation) | F Outbound provider operation | system on behalf of an Automation rule whose creator may use the connection | N | N | N | N | DEFINED | unblocks UC-AUT-D09 |
| UC-INT-F04 | Dispatch an n8n webhook for Automation | F Outbound provider operation | system on behalf of an Automation Webhook step | Y | N | N | N | IMPLEMENTING | no real e2e; see UC-AUT-D08 |
| UC-INT-F05 | Read the delivery log of a webhook | F Outbound provider operation | Owner and Admin | N | N | Y | N | IMPLEMENTING | no read endpoint |
| UC-INT-F06 | Retry a failed delivery with backoff | F Outbound provider operation | system | P | N | N | N | IMPLEMENTING | the budget differs from the decision (DEF-12) |
| UC-INT-F07 | Reconcile an unknown provider outcome | F Outbound provider operation | system | N | N | N | N | DEFINED | no executable path outside n8n |
| UC-INT-G01 | Accept only bounded, verified calendar webhooks | G Inbound webhook verification | the provider (anonymous route, signature-authenticated, rate limited per IP) | Y | N | N | N | INTEGRATING | no provider simulator e2e; the rate limit has no negative test found |
| UC-INT-G02 | Reject replayed and duplicate deliveries | G Inbound webhook verification | system | Y | N | N | N | INTEGRATING | as G01 |
| UC-INT-G03 | The tenant comes from the binding, never the payload | G Inbound webhook verification | system | Y | N | N | N | INTEGRATING | as G01 |
| UC-INT-G04 | Malformed or conflicting payloads fail closed | G Inbound webhook verification | system | Y | N | N | N | INTEGRATING | as G01 |
| UC-INT-H01 | Bind a calendar to a connection | H Inbound event mapping | Owner and Admin (ManageIntegrations) | P | N | Y | N | IMPLEMENTING | no UI; the token travels in the request body and is not linked to the OAuth connection |
| UC-INT-H02 | Remove a calendar binding | H Inbound event mapping | Owner and Admin (ManageIntegrations on the calendar integration) | Y | N | N | N | IMPLEMENTING | no UI; not idempotent by key |
| UC-INT-H03 | Turn a verified calendar change into an internal fact | H Inbound event mapping | system | P | N | N | N | IMPLEMENTING | the consumer reconciles into Integrations state; the Work Management consumer of the fact is not found |
| UC-INT-H04 | Sync a calendar now | H Inbound event mapping | Owner and Admin (ManageIntegrations | N | N | N | N | IMPLEMENTING | handler is a stub; no endpoint |
| UC-INT-H05 | Backfill and cursor | H Inbound event mapping | system | P | N | N | N | IMPLEMENTING | domain only; no runner |
| UC-INT-H06 | Detect and resolve a sync conflict | H Inbound event mapping | Owner and Admin see conflicts | P | N | N | N | IMPLEMENTING | domain only |
| UC-INT-H07 | Keep both identities of a mapped event | H Inbound event mapping | system | P | N | N | N | IMPLEMENTING | no flow uses it end to end |
| UC-INT-H08 | Provider content is untrusted | H Inbound event mapping | system | N | N | N | N | DEFINED | no sanitizer exists at the provider boundary |
| UC-INT-I01 | See status, last success and last error | I Connection health | Owner and Admin | Y | Y | Y | Y | HARDENING | Expired and Revoked collapse into "error" in the DTO |
| UC-INT-I02 | Sync health is separate from connection status | I Connection health | Owner and Admin | N | N | N | N | IMPLEMENTING | no DTO field |
| UC-INT-I03 | Hold work while reauthorization is needed | I Connection health | Owner and Admin | N | N | Y | N | DEFINED | no held-operation state exists |
| UC-INT-I04 | Tell admins when a connection breaks (enterprise baseline) | I Connection health | Owner, Admin and the connector owner receive | N | N | N | N | DEFINED | (enterprise baseline) |
| UC-INT-J01 | Provider results map to one taxonomy | J Provider failure normalization | system | Y | N | N | N | INTEGRATING | the taxonomy is per adapter; no shared contract type; OAuth and calendar adapters are not covered |
| UC-INT-K01 | The Integrations page | K Integration management frontend | Owner and Admin | Y | Y | Y | Y | HARDENING | the permission-denied state has no real e2e; no axe; no 1 px diff |
| UC-INT-K02 | Confirm a disconnect | K Integration management frontend | Owner and Admin | Y | Y | Y | Y | HARDENING | no impact counts (B05) |
| UC-INT-K03 | Connection detail page | K Integration management frontend | Owner and Admin | N | P | Y | N | IMPLEMENTING | reduced view only |
| UC-INT-K04 | Live connection and sync progress | K Integration management frontend | Owner and Admin | N | N | N | N | DEFINED | no realtime producer or consumer exists |
| UC-INT-L01 | Back off on a provider rate limit | L Rate/retry/provider limits | system | P | N | N | N | IMPLEMENTING | retry-after is not honored |
| UC-INT-L02 | Queued work after a disconnect never runs | L Rate/retry/provider limits | system | P | N | N | N | IMPLEMENTING | skip is not recorded as a delivery state |
| UC-INT-M01 | Plan limits on connections and premium providers | M Entitlement integration | Owner and Admin | N | N | P | N | DEFINED | the code uses a constant, not Billing |
| UC-INT-N01 | Outgoing webhooks cannot reach internal networks | N Hardening/security | system | Y | N | N | Y | HARDENING | no test of the send-time DNS check in a hostile resolver case |
| UC-INT-N02 | Connect, pause and disconnect are audited | N Hardening/security | Owner and Admin read the audit log | N | N | N | N | DEFINED | (enterprise baseline) |

# Part 3. Use cases

# Automation (AUT)

## Capability A. Rule lifecycle

### UC-AUT-A01 Create a board rule
- Capability: AUT-001 Rule lifecycle
- Actors and permission: board Manager (ManageBoard on the board); workspace Owner and Admin are Managers everywhere. A rule with a Webhook action on the workspace path also needs ManageIntegrations.
- Preconditions: board exists and is visible to the actor; plan capacity for one more rule; the actor holds every action's permission on its targets now.
- Input: name (non-blank), trigger type and filter (for example group), optional all-of conditions (max 10), 1 to 5 ordered actions, `boardId`; `Idempotency-Key` header.
- Output: a rule in Draft with the creator bound as executor, configuration version 1; the new id with 201; one plan capacity slot consumed.
- Main flow: 1. Actor opens the board Automation tab and chooses New rule. 2. Fills trigger, conditions, actions. 3. Saves as draft or saves and enables (A06, E01). 4. Backend assembles and validates the configuration, checks executor authority, creates the rule and consumes capacity in one transaction.
- No-op / rejection / concurrency / retry: invalid configuration is rejected with a field-level error (AUT-001, AUT-002); plan limit reached is rejected; an actor without access to a target (for example a Private board item) is rejected, never bound; the same idempotency key returns the first result with no second rule or capacity slot.
- UI states: loading, validation error, permission denied, conflict, pending.
- Rules: automation.md §5 AUT-001, §7 AUT-002, §61.2 AUT-020; AIREQ009, AIREQ010, AIREQ017, AIREQ053.
- Acceptance: Given a board Manager, When they save a valid rule, Then it exists as Draft with them as executor and capacity is consumed once. Given a Member who is not a Manager, When they post the same body, Then the response is the same NotFound as for a stranger. Given the same idempotency key twice, Then one rule exists.
- Evidence: BE POST `/api/v1/workspaces/{workspaceId}/automations/` (Automation.Rules.Create), `CreateAutomationRuleCommandHandler` · FE FE-BS editor, FE-WA `create` · Design `Final-Automation` · Tests T-AA (`CreateAutomationRuleBillingGateTests`, `AutomationPermissionMarkerTests`, `AutomationRuleManagementAuthorityTests`), T-AI `CreateAutomationRulePipelineTests` · Real e2e E2E-AUT
- State: HARDENING — no cross-tenant negative test on create found; no axe or 1 px design proof; no owner check.

### UC-AUT-A02 Create a workspace-level rule (no board)
- Capability: AUT-001 Rule lifecycle
- Actors and permission: workspace Owner and Admin (ManageWorkspaceSettings); ManageIntegrations when an action is Webhook.
- Preconditions: as A01, without a board.
- Input: as A01 with `boardId` empty.
- Output: a Draft rule scoped to the workspace.
- Main flow: 1. Admin opens a workspace Automation page (K07). 2. Builds the rule. 3. Saves.
- No-op / rejection / concurrency / retry: as A01. Lead decision (2026-10-30): workspace-level rules are not offered in the UI until a workspace-level trigger source exists (all wired triggers are board facts); the API stays.
- UI states: none built.
- Rules: automation.md §8 Rule identity, §98 Governance; AIREQ016.
- Acceptance: Given an Admin, When they post without `boardId`, Then the rule is created at workspace scope and a Member gets NotFound.
- Evidence: BE same route as A01 (`BoardId is null` path) · FE none (the board claim is always sent) · Design none · Tests T-AA `AutomationBoardScopeTests` · Real e2e none
- State: IMPLEMENTING — backend only; no UI, no e2e.

### UC-AUT-A03 List the rules of a board or workspace
- Capability: AUT-001 Rule lifecycle
- Actors and permission: board Manager (ManageBoard) with `boardId`; Owner and Admin (ManageWorkspaceSettings) without. Members and Guests cannot read rule configuration (automation.md §61.1).
- Preconditions: workspace or board visible to the actor.
- Input: `workspaceId`, optional `boardId`.
- Output: rules not archived, ordered by name, with enabled flag, executor, `disabledReason`, version, actions, conditions.
- Main flow: 1. Open the Automation tab. 2. Backend returns rules. 3. UI shows status, runner and last stop reason.
- No-op / rejection / concurrency / retry: a caller without permission gets NotFound; a board claim for another board's rule returns nothing. The result is unbounded today (defect DEF-3).
- UI states: loading, empty, error with retry, permission denied, read-only.
- Rules: automation.md §61.1; AIREQ016.
- Acceptance: Given 3 rules on a board and 1 on another, When a Manager lists with that board, Then 3 return; a Member gets NotFound.
- Evidence: BE GET `/api/v1/workspaces/{workspaceId}/automations/` (Automation.Rules.List) · FE FE-BS, FE-WA `list` · Design `Final-Automation`, `Final-M-Automation` · Tests T-AA `AutomationRuleManagementAuthorityTests` · Real e2e E2E-AUT
- State: HARDENING — unbounded read (DEF-3); no pagination contract; no cross-tenant test.

### UC-AUT-A04 Edit a rule into a new version
- Capability: AUT-001 Rule lifecycle
- Actors and permission: as A01; the board path never edits a rule that has an integration action. The editor becomes the executor.
- Preconditions: rule exists, not archived; the editor holds every action's permission now.
- Input: full new definition (name, trigger, conditions, actions), `boardId`, expected version (required, not built).
- Output: configuration version +1; runs already queued or running finish on the version they started with; only later runs use the new one.
- Main flow: 1. Open the rule. 2. Change. 3. "Save as version N+1". 4. Backend revalidates and revises.
- No-op / rejection / concurrency / retry: invalid configuration rejected; a rule of another board is NotFound; a no-op edit (same definition) bumps no version and emits nothing (required); a stale expected version is rejected with Conflict and changes nothing (required; today last write wins, DEF-2); the same idempotency key returns the first result.
- UI states: validation error, permission denied, conflict, pending, read-only.
- Rules: automation.md §12, §13 AUT-004, §14; AIREQ013, AIREQ014; owner decision 44 (OD2).
- Acceptance: Given a running execution on version 1, When the Manager saves version 2, Then the execution ends on version 1 and the next run records version 2. Given a stale version, Then 409 and nothing changes.
- Evidence: BE PUT `/api/v1/workspaces/{workspaceId}/automations/{automationId}` (Automation.Rules.Update), `UpdateAutomationRuleCommandHandler`, `AutomationRule.Revise` · FE FE-BS `automation-editor.tsx` (version list in `automation-aside.tsx`), FE-WA `update` · Design `Final-Automation` · Tests T-AD `AutomationRuleLifecycleTests`, T-AA `AutomationExecutorIdentityTests`, T-AI `CreateAutomationRulePipelineTests` · Real e2e E2E-AUT
- State: HARDENING — no expected version (DEF-2); no no-op proof; no conflict UI.

### UC-AUT-A05 Delete a rule
- Capability: AUT-001 Rule lifecycle
- Actors and permission: as A01 (ManageBoard or ManageWorkspaceSettings).
- Preconditions: rule exists and is not already deleted.
- Input: `automationId`, `boardId` claim; idempotency key.
- Output: rule archived and removed from lists; no new executions; running ones finish; history readable 90 days; the plan slot is released once.
- Main flow: 1. Choose Delete. 2. Confirm. 3. Backend archives and releases capacity.
- No-op / rejection / concurrency / retry: a second delete is NotFound; a board claim mismatch is NotFound; capacity release uses a stable per-rule identity so a retry never releases twice.
- UI states: destructive confirmation, pending, error, permission denied.
- Rules: automation.md §100; AIREQ015, AIREQ109, AIREQ053; owner decision 44.
- Acceptance: Given a rule with history, When deleted, Then lists no longer show it, history is readable for 90 days, then NotFound, and capacity rises by one.
- Evidence: BE DELETE `/api/v1/workspaces/{workspaceId}/automations/{automationId}` (Automation.Rules.Delete) · FE FE-BS, FE-WA `remove` · Design `Final-Automation` · Tests T-AD `AutomationRuleDeletionLifecycleTests`, T-AI `CreateAutomationRulePipelineTests` · Real e2e E2E-AUT
- State: HARDENING — pending executions and scheduled jobs on delete are not proven; no purge job (J04).

### UC-AUT-A06 Save a rule as a draft
- Capability: AUT-001 Rule lifecycle
- Actors and permission: as A01.
- Preconditions: as A01.
- Input: as A01.
- Output: rule in Draft; it never runs.
- Main flow: 1. Save as draft in the editor. 2. Later enable (E01).
- No-op / rejection / concurrency / retry: a Draft never matches a trigger (AUT-002). The product decision says drafts do not count toward the limit; code consumes a slot at create (DEF-6, M04).
- UI states: validation error, pending, read-only.
- Rules: automation.md §6; owner decision 44 (OD3); AIREQ010.
- Acceptance: Given a Draft rule, When a matching fact arrives, Then no execution is created.
- Evidence: BE `AutomationRule.Create` (Status Draft) · FE FE-BS `automation-editor.tsx` (`submit("draft")`) · Design `Final-Automation` · Tests T-AD `AutomationRuleActivationInvariantTests` · Real e2e none
- State: INTEGRATING — capacity rule conflicts with the decision (DEF-6); no e2e for draft.

### UC-AUT-A07 Restore a deleted rule
- Capability: AUT-001 Rule lifecycle
- Actors and permission: board Manager or Owner and Admin, same as delete.
- Preconditions: rule deleted less than 90 days ago; capacity available.
- Input: rule id.
- Output: rule back as Disabled (never auto-enabled); capacity consumed again.
- Main flow: 1. Open deleted rules. 2. Restore. 3. Enable when ready (E01).
- No-op / rejection / concurrency / retry: restoring a live rule is a no-op; if capacity is gone the restore is rejected; the same idempotency key returns the first result. Lead decision (2026-10-30): restore is part of the product (AIREQ015 lists it) inside the 90-day window.
- UI states: empty, pending, error, permission denied.
- Rules: automation.md §100; AIREQ015.
- Acceptance: Given a rule deleted 10 days ago, When restored, Then it appears Disabled with its history and version.
- Evidence: BE `AutomationRule.Restore` (domain only, no endpoint) · FE none · Design none · Tests T-AD `AutomationRuleDeletionLifecycleTests` · Real e2e none
- State: IMPLEMENTING — domain method only; no command, endpoint or UI.

### UC-AUT-A08 Create a rule from a template
- Capability: AUT-001 Rule lifecycle
- Actors and permission: as A01.
- Preconditions: a published template valid under current trigger and action contracts.
- Input: template id, parameters.
- Output: an ordinary Draft rule; later template changes never change it.
- Main flow: 1. Open the template gallery. 2. Choose. 3. Fill targets. 4. Save as A01.
- No-op / rejection / concurrency / retry: a template that references an unknown trigger or action is rejected at instantiate.
- UI states: loading, empty, error.
- Rules: automation.md §78 to §80 AUT-026; AIREQ043, AIREQ044. Deferred by the frozen P5 decisions of the execution spec.
- Acceptance: Given a template, When instantiated, Then a rule with its own id exists and editing the template leaves it unchanged.
- Evidence: BE `AutomationTemplate` domain and table, no endpoint · FE none · Design none · Tests T-AD `AutomationTemplateTests` · Real e2e none
- State: IMPLEMENTING — domain and table only; deferred by decision.

## Capability B. Trigger model

### UC-AUT-B01 Fire a rule when an item is created
- Capability: AUT-002 Trigger model
- Actors and permission: set up by a board Manager (A01); fired by the system on the Work Management fact `board.item.created`.
- Preconditions: rule Active with trigger ItemCreated on the same board and workspace.
- Input: the fact (item id, board, workspace, causation).
- Output: one execution per rule and fact.
- Main flow: 1. An item is created. 2. The outbox delivers the fact to the Automation consumer. 3. The evaluator matches rules, checks conditions, creates the execution (G01).
- No-op / rejection / concurrency / retry: a redelivered fact makes no second execution (F01); a fact from another workspace never matches (F03).
- UI states: trigger picker option "Item created"; run appears in the log.
- Rules: automation.md §15 to §19 AUT-005, AUT-006; AIREQ022, AIREQ024, AIREQ047.
- Acceptance: Given an Active rule, When an item is created on its board, Then one execution is queued with the rule version.
- Evidence: BE `BoardItemCreatedAutomationConsumer`, `N8nAutomationRuleEvaluator` · FE FE-BS `automation-pickers.tsx` · Design `Final-Automation` · Tests T-AI `AutomationTriggerMatrixIntegrationTests`, `AutomationWorkEventRuntimeChainIntegrationTests` · Real e2e none
- State: INTEGRATING — the real e2e covers only the moved trigger.

### UC-AUT-B02 Fire a rule when an item moves to a group
- Capability: AUT-002 Trigger model
- Actors and permission: as B01.
- Preconditions: rule Active, trigger ItemMovedToGroup with an optional group filter on the rule's board.
- Input: the fact `board_item.moved`.
- Output: an execution when the target group matches the filter.
- Main flow: as B01 with `BoardItemMovedAutomationConsumer`.
- No-op / rejection / concurrency / retry: a move made by an automation is itself a fact that can trigger another rule; depth and rate are bounded (G03). A filter group on another board is rejected at save.
- UI states: trigger option "Moved to the group" with group picker.
- Rules: as B01; AUT-022.
- Acceptance: Given a rule with group Done, When an item moves to Done, Then it runs; a move to another group does not.
- Evidence: BE `BoardItemMovedAutomationConsumer` · FE FE-BS · Design `Final-Automation` · Tests T-AI `AutomationWorkActionChainIntegrationTests` · Real e2e E2E-AUT
- State: HARDENING — no axe or design diff; no owner check.

### UC-AUT-B03 Fire a rule when a person is assigned
- Capability: AUT-002 Trigger model
- Actors and permission: as B01.
- Preconditions: Active rule with trigger ItemAssigned.
- Input: the fact `board.item.member-assigned`.
- Output: one execution per fact.
- Main flow: as B01 with `BoardItemMemberAssignedAutomationConsumer`.
- No-op / rejection / concurrency / retry: a duplicate fact makes no second execution.
- UI states: trigger option; log.
- Rules: as B01.
- Acceptance: Given an Active rule, When a member is assigned, Then one execution is queued.
- Evidence: BE `BoardItemMemberAssignedAutomationConsumer` · FE FE-BS (picker; not checked against the API in a real run) · Design `Final-Automation` · Tests T-AI `AutomationTriggerMatrixIntegrationTests` · Real e2e none
- State: INTEGRATING — no real e2e.

### UC-AUT-B04 Triggers on update, field or status change, delete, form submit
- Capability: AUT-002 Trigger model
- Actors and permission: as A01.
- Preconditions: Work Management publishes the matching facts with a stable contract (AIREQ023).
- Input: trigger type ItemUpdated, FieldChanged (fieldId), ItemDeleted, FormSubmitted, plus "Status changed" from design.
- Output: execution as B01.
- Main flow: as B01 once a consumer exists.
- No-op / rejection / concurrency / retry: until a consumer exists these types must be refused at save (AIREQ024: released vocabulary matches runtime reachability). Today they save and never fire (DEF-5).
- UI states: trigger option hidden until reachable.
- Rules: automation.md §16 AUT-005; AIREQ024; owner decisions 56, 57 (the field value fact now carries the stored JSON).
- Acceptance: Given a FieldChanged rule, When the field changes, Then one execution is queued; before that, saving such a rule is rejected.
- Evidence: BE `AutomationTriggerValidator` accepts them; no consumer in `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/` · FE FE-BS (status trigger in the mock design) · Design `Final-Automation` · Tests T-AD `AutomationTriggerValidatorTests` · Real e2e none
- State: IMPLEMENTING — validators only; cannot run end to end.

### UC-AUT-B05 Run a rule on a schedule
- Capability: AUT-002 Trigger model
- Actors and permission: as A01.
- Preconditions: schedule valid: daily or weekly at a fixed time, time zone fixed per board.
- Input: schedule (time, days, IANA time zone), missed-run policy (run once or skip).
- Output: a scheduled occurrence creates one execution with a stable occurrence identity.
- Main flow: 1. Rule saved with a schedule. 2. The scheduler fires at the occurrence. 3. One execution per matching item set.
- No-op / rejection / concurrency / retry: daylight-saving gaps and overlaps follow the stored zone (AUT-008); a missed run follows the policy, default run once (owner decision 44, DEF-5); two scheduler instances never create two executions for one occurrence (AUT-007, AUT-009).
- UI states: schedule editor, next run time, missed-run choice.
- Rules: automation.md §22 to §27 AUT-007, AUT-008, AUT-009; AIREQ039 to AIREQ042. Deferred in the frozen P5 decisions.
- Acceptance: Given a daily 09:00 Hanoi rule, When 09:00 passes with the scheduler down and policy "run once", Then one execution runs after recovery.
- Evidence: BE `ScheduledJob`, `ScheduleDefinition` domain and table; validator accepts `cron` or `interval`; no scheduler runtime · FE FE-BS `automation-flow.tsx` schedule UI (mock handlers) · Design `Final-Automation` · Tests T-AD `ScheduledJobTests`, `ScheduleDefinitionTests` · Real e2e none
- State: IMPLEMENTING — no scheduler runtime; the validator accepts cron or interval while the UI sends daily or weekly.

### UC-AUT-B06 Run a rule by hand on an item
- Capability: AUT-002 Trigger model
- Actors and permission: any actor who can edit the item; the rule's executor must be able to act.
- Preconditions: Active rule with a manual trigger.
- Input: item id.
- Output: one execution attributed to the person who pressed the button as trigger actor; executor unchanged.
- Main flow: item page button; backend creates the execution.
- No-op / rejection / concurrency / retry: the same idempotency key gives one execution.
- UI states: button, pending, result.
- Rules: automation.md §21; owner decision 44 (OD8, deferred).
- Acceptance: Given a manual rule, When a member presses the button on an item, Then one execution is created.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — deferred by the owner until the item-page contract exists.

### UC-AUT-B07 An unknown or changed trigger type fails closed
- Capability: AUT-002 Trigger model
- Actors and permission: system.
- Preconditions: a stored rule names a trigger discriminator or configuration schema the code no longer knows.
- Input: the stored configuration.
- Output: the rule cannot be enabled and does not run; the list reports it as invalid.
- Main flow: the loader fails closed on the unknown discriminator.
- No-op / rejection / concurrency / retry: never coerced to another type.
- UI states: invalid rule badge (not built).
- Rules: automation.md §20; AIREQ019, AIREQ107.
- Acceptance: Given a stored rule with unknown trigger "X", When listed or evaluated, Then it is not executed and is reported invalid.
- Evidence: BE `AutomationConfiguration` (discriminator handling) · FE none · Design none · Tests T-AD `AutomationUnknownDiscriminatorTests`, `ConfigurationContractTests` · Real e2e none
- State: IMPLEMENTING — domain proof only; no UI for invalid rules.

### UC-AUT-B08 Start a rule from an inbound provider event
- Capability: AUT-002 Trigger model
- Actors and permission: system, after Integrations has verified and translated the event.
- Preconditions: Integrations publishes a stable internal fact (UC-INT-H03); rule trigger "email received" or "provider event".
- Input: the translated fact.
- Output: execution as B01.
- Main flow: provider webhook, Integrations verification, internal fact, Automation consumer.
- No-op / rejection / concurrency / retry: a raw provider webhook never triggers Automation directly (AUT-032).
- UI states: trigger option.
- Rules: automation.md §93, §94 AUT-032; AIREQ047; owner decision 44 (OD9, email-to-board deferred).
- Acceptance: Given a verified inbound fact, When published, Then a matching rule runs once.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — deferred until verified provider intake exists.

## Capability C. Condition model

### UC-AUT-C01 Add all-of equals conditions
- Capability: AUT-003 Condition model
- Actors and permission: as A01.
- Preconditions: condition targets (group, status option, field) belong to the rule's board.
- Input: up to 10 conditions `group`, `status` or `field` (equals); all must hold.
- Output: the rule runs only when every condition holds at the consistency point of evaluation.
- Main flow: 1. Add a condition in the editor. 2. At a fact, the evaluator reads item facts and tests each.
- No-op / rejection / concurrency / retry: a missing or null field value counts as not equal (condition false); more than 10 or an unknown kind is rejected at save; a target of another board is rejected.
- UI states: condition builder, validation error.
- Rules: automation.md §29 to §34 AUT-010, AUT-011; AIREQ020; owner decision 44 (OD4: no OR, no nesting).
- Acceptance: Given conditions status=Done and field=X, When an item has only status Done, Then the rule does not run.
- Evidence: BE `AutomationConditions`, `AutomationExecutorAuthority.EnsureRuleAsync` · FE FE-BS `automation-flow.tsx` · Design `Final-Automation` · Tests T-AA `AutomationBoardScopeTests`, T-AD `AutomationConditionDefinitionTests` · Real e2e none
- State: INTEGRATING — the real e2e has no condition; no condition-false end-to-end proof.

### UC-AUT-C02 A condition not met is shown as skipped, not failed
- Capability: AUT-003 Condition model
- Actors and permission: any reader of run history (J01 permission).
- Preconditions: a matching fact whose conditions are false.
- Input: the fact.
- Output: no action runs; the log shows "Skipped, conditions not met" and the failure counter is not touched.
- Main flow: the evaluator records the skip.
- No-op / rejection / concurrency / retry: a skip never counts toward the consecutive-failure limit (E05). Lead decision (2026-10-30): skips are recorded as executions with a new status Skipped and retained like other runs, so people can see why nothing happened.
- UI states: log row "Skipped".
- Rules: automation.md §33 AUT-011, §106; AIREQ021.
- Acceptance: Given a failing condition, When the fact arrives, Then the log shows Skipped and the rule is not disabled.
- Evidence: BE the evaluator drops the rule silently; `AutomationExecutionStatus` has no Skipped (DEF-16) · FE FE-BS `automation-log.tsx` (design shows Skipped) · Design `Final-M-Automation` · Tests T-AA `AutomationRuleEvaluatorGuardTests` · Real e2e none
- State: IMPLEMENTING — no Skipped status or record.

### UC-AUT-C03 Actor and time conditions, OR and nesting
- Capability: AUT-003 Condition model
- Actors and permission: as A01.
- Preconditions: none.
- Input: conditions ActorIsUser, ActorInTeam, TimeElapsed.
- Output: not released.
- Main flow: none.
- No-op / rejection / concurrency / retry: the API refuses these types while unreleased (AIREQ024).
- UI states: not shown.
- Rules: automation.md §31, §34; owner decision 44 (OD4).
- Acceptance: Given the type `ActorIsUser`, When saved, Then rejected as unknown.
- Evidence: BE enum `AutomationConditionType` only · FE none · Design none · Tests T-AD `AutomationConditionDefinitionTests` · Real e2e none
- State: DEFINED — deferred by decision OD4.

## Capability D. Action model

### UC-AUT-D01 Move the item to a group
- Capability: AUT-004 Action model
- Actors and permission: executor (bound creator) holding the MoveItem decision on the item.
- Preconditions: item exists in the workspace and is visible to the executor; the target group is on the item's board.
- Input: `targetGroupId`.
- Output: item moved as the executor; the fact `board_item.moved` v2 names the executor as actor and carries rule and execution ids; the item audit records the executor.
- Main flow: 1. Execution queued. 2. The dispatch consumer re-authorizes the executor. 3. The Work Management public action moves the item. 4. Step recorded.
- No-op / rejection / concurrency / retry: an item already in the group is a semantic no-op (step succeeds, no new fact); an executor who lost permission stops the rule with a reason; retry uses the stable operation id.
- UI states: action picker; log step.
- Rules: automation.md §36 AUT-012, §61.2 AUT-020, §77 AUT-025; AIREQ025, AIREQ048, AIREQ051.
- Acceptance: Given a rule moving to Done, When triggered, Then the item is in Done and its audit shows the executor.
- Evidence: BE `AutomationMoveItemDispatchConsumer`, `AutomationMoveItemUseCase`, `WorkItemActionAdapter` · FE FE-BS · Design `Final-Automation` · Tests T-AI `AutomationMoveItemExecutorCompositionIntegrationTests`, T-AA `AutomationExecutorIdentityTests` · Real e2e E2E-AUT
- State: HARDENING — no owner check; no 1 px diff.

### UC-AUT-D02 Change the item status
- Capability: AUT-004 Action model
- Actors and permission: executor with edit permission on the item.
- Preconditions: option `optionId` exists on the board.
- Input: `optionId`.
- Output: status changed through the Work Management public action; the fact names executor and rule.
- Main flow: as D01 with `ChangeStatusAsync`.
- No-op / rejection / concurrency / retry: the same status is a no-op; an archived option fails the step as terminal (see UC-AUT-D10).
- UI states: action picker "Status"; log.
- Rules: automation.md §36, §45; AIREQ025, AIREQ051.
- Acceptance: Given a rule setting status Doing, When run, Then the item has Doing.
- Evidence: BE `IWorkActionPort.ChangeStatusAsync` · FE FE-BS, FE-WA `toAction` · Design `Final-Automation` · Tests T-AI `AutomationWorkActionChainIntegrationTests` · Real e2e E2E-AUT
- State: HARDENING — as D01.

### UC-AUT-D03 Assign a person
- Capability: AUT-004 Action model
- Actors and permission: executor with permission to assign on the item.
- Preconditions: the person is an active workspace member.
- Input: `userId`.
- Output: person added as assignee.
- Main flow: as D01 with `AssignMemberAsync`.
- No-op / rejection / concurrency / retry: already assigned is a no-op; a person who left fails the step as terminal; an assignee fact from this action can trigger B03 within chain limits (G03).
- UI states: action picker "Person"; log.
- Rules: automation.md §45; AIREQ051.
- Acceptance: Given a rule assigning Hoa, When run, Then Hoa is assignee once even after a retry.
- Evidence: BE `IWorkActionPort.AssignMemberAsync` · FE FE-BS · Design `Final-Automation` · Tests T-AI `AutomationWorkActionChainIntegrationTests` · Real e2e E2E-AUT
- State: HARDENING — as D01.

### UC-AUT-D04 Set a field value
- Capability: AUT-004 Action model
- Actors and permission: executor with edit permission on the item.
- Preconditions: field exists and is not archived; the value is valid for its type.
- Input: `fieldId`, `value` (JSON).
- Output: field value written through Work Management.
- Main flow: as D01 with `SetFieldValueAsync`.
- No-op / rejection / concurrency / retry: an equal value is a no-op; a type mismatch fails as terminal.
- UI states: action picker "Field"; log.
- Rules: automation.md §37, §38 AUT-013; AIREQ025.
- Acceptance: Given a rule setting a date field to today, When run, Then the item shows the date.
- Evidence: BE `IWorkActionPort.SetFieldValueAsync`, `EnsureFieldValueAsync` · FE FE-BS (value sent as a string) · Design `Final-Automation` · Tests T-AI `AutomationWorkActionChainIntegrationTests` · Real e2e none
- State: INTEGRATING — no real e2e; the frontend sends the value as a string for every field type.

### UC-AUT-D05 Notify a member or send an email
- Capability: AUT-004 Action model
- Actors and permission: executor; the recipient must be an active member who can see the board.
- Preconditions: recipient valid.
- Input: `userId`, message (or subject and body).
- Output: a Collaboration notification or an email through `IAutomationMessagePort`.
- Main flow: the execution step calls the message port.
- No-op / rejection / concurrency / retry: a technical retry never sends a second notification for one logical outcome (automation.md §91).
- UI states: action picker "Notify"; log.
- Rules: automation.md §44; AIREQ051.
- Acceptance: Given a rule notifying Hoa, When run twice for one fact, Then Hoa gets one notification.
- Evidence: BE `AutomationMessageAdapter`, `IAutomationMessagePort` · FE FE-BS, FE-WA `toAction` · Design `Final-Automation` · Tests T-AA `AutomationMultiActionTests`, T-AI `AutomationWorkActionChainIntegrationTests` · Real e2e E2E-AUT (the third action of the spec is a notification)
- State: HARDENING — the email delivery provider is not proven in a real run.

### UC-AUT-D06 Run up to five actions in order
- Capability: AUT-004 Action model
- Actors and permission: as A01.
- Preconditions: each action valid.
- Input: an ordered list of 1 to 5 actions; no branching.
- Output: one execution with one step per action, run in order.
- Main flow: steps run sequentially, each recorded.
- No-op / rejection / concurrency / retry: Lead decision (2026-10-30): a failed step stops the execution; later steps do not run and show as Not run; earlier successful steps are kept (no rollback; compensation is a new action, automation.md §53). Manual rerun of the failed step is I03.
- UI states: action builder with Action 1..N; log with steps.
- Rules: automation.md §39, §40 AUT-014, §52, §53; AIREQ026, AIREQ031.
- Acceptance: Given three actions where step 2 fails, Then step 1 is Succeeded, step 2 Failed, step 3 Not run.
- Evidence: BE `AutomationExecutionStep`, `AutomationExecutionProgress` · FE FE-BS `automation-flow.tsx` · Design `Final-Automation` · Tests T-AA `AutomationMultiActionTests` · Real e2e E2E-AUT
- State: HARDENING — the "Not run" step state is not in the DTO (steps map to succeeded or failed only).

### UC-AUT-D07 Create an item
- Capability: AUT-004 Action model
- Actors and permission: executor with create permission on the target board.
- Preconditions: target board and fields valid.
- Input: title, group, field values.
- Output: a new item created through Work Management.
- Main flow: as D01.
- No-op / rejection / concurrency / retry: a retry must not create a second item: the operation id is the dedupe key.
- UI states: action picker.
- Rules: automation.md §45; AIREQ025 (action vocabulary matches an executor).
- Acceptance: Given a CreateItem rule, When retried, Then one item exists.
- Evidence: BE validator `CreateItem` only; no executor · FE none (`toAction` returns null) · Design none · Tests T-AD `AutomationActionValidatorTests` · Real e2e none
- State: IMPLEMENTING — validates and saves, never runs; must be refused until an executor exists.

### UC-AUT-D08 Call an n8n webhook
- Capability: AUT-004 Action model
- Actors and permission: ManageIntegrations on create and enable (workspace path); board Managers cannot create or edit it.
- Preconditions: a valid n8n webhook path; no reusable secret in the rule (AUT-029).
- Input: `webhookPath`.
- Output: a durable dispatch to n8n with a claim, outcome evidence and retry or unknown-outcome state.
- Main flow: execution queued; `N8nDispatchConsumer` claims, calls n8n outside the database transaction, settles.
- No-op / rejection / concurrency / retry: an unknown outcome is not re-fired (H05); a retryable failure re-queues with an attempt count; a duplicate delivery sees the claim.
- UI states: none built.
- Rules: automation.md §42, §43 AUT-015, §84.1; integrations.md §77.1 INT-027; AIREQ071 to AIREQ074.
- Acceptance: Given a Webhook rule, When an item is created, Then n8n receives one call with a stable operation identity.
- Evidence: BE `N8nDispatchConsumer`, `N8nDispatchUseCase`, `N8nClient` · FE none (DEF-14) · Design none · Tests T-AI `N8nDispatchRuntimeChainIntegrationTests`, `AutomationN8nDurabilityIntegrationTests`, `backend/tests/Notrelix.Integration.Tests/Extensibility/N8nAutomationTests.cs` · Real e2e none
- State: IMPLEMENTING — no UI to author or see it; no real e2e.

### UC-AUT-D09 Send a Slack message
- Capability: AUT-004 Action model
- Actors and permission: ManageIntegrations; the Slack connection owner.
- Preconditions: an active Slack connection (UC-INT-C02); channel chosen.
- Input: channel, message text.
- Output: a Slack message sent through an Integrations outbound operation (UC-INT-F03).
- Main flow: the execution step calls the Integrations operation contract; Automation holds no Slack client.
- No-op / rejection / concurrency / retry: a connection that needs reauthorization holds the step until reauthorized (automation.md §42; I03).
- UI states: action picker; held-for-reauthorization log state.
- Rules: automation.md §42, §43 AUT-015; AIREQ065; Lead decision (2026-10-30): not released until UC-INT-F03 exists.
- Acceptance: Given a Slack rule, When run, Then one message is posted and a retry posts none.
- Evidence: BE validator and `AutomationActionKinds` only · FE none · Design `Final-Integrations` (Teams example) · Tests T-AD `AutomationActionValidatorTests` · Real e2e none
- State: BLOCKED — waits on UC-INT-F03 (Slack outbound operation); today the rule saves and never runs (DEF-4).

### UC-AUT-D10 A deleted or archived target blocks enable or fails the step
- Capability: AUT-004 Action model
- Actors and permission: the actor enabling; the system at run time.
- Preconditions: a rule references a field, option, group, board or person that was archived or deleted.
- Input: rule configuration.
- Output: enable is blocked with the named reference; at run time the step fails as terminal with a visible reason; nothing is retargeted.
- Main flow: server check on enable and on each run (the save in A04 also checks).
- No-op / rejection / concurrency / retry: other steps that do not depend on the missing target keep their own state.
- UI states: blocked-enable message "field Ngày bàn giao archived" with Restore field and Choose another field.
- Rules: automation.md §101, §102 AUT-034, §104; AIREQ048.
- Acceptance: Given a rule step writing field F, When F is archived and the Manager enables, Then enable is rejected naming F.
- Evidence: BE `AutomationExecutorAuthority.EnsureCanExecuteAsync` (checks the first action only) · FE FE-BS (message in design only) · Design `Final-Automation` · Tests T-AA `AutomationBoardScopeAuthorityTests` · Real e2e none
- State: IMPLEMENTING — the enable check covers `rule.Configuration.Action` (first action) only; no structured broken-reference error.

## Capability E. Enable and disable lifecycle

### UC-AUT-E01 Enable a rule
- Capability: AUT-005 Enable/disable lifecycle
- Actors and permission: board Manager (ManageBoard); on the workspace path Owner and Admin with ManageIntegrations too. The board path refuses a rule with an integration action.
- Preconditions: rule is Draft or Disabled and valid; the executor holds the action permission now; a legacy rule with no executor cannot be enabled.
- Input: `isEnabled=true`, `boardId`, expected version (required, not built).
- Output: Active, disabled reason cleared, version +1.
- Main flow: PATCH enabled; the server re-validates (AUT-002) and checks the executor.
- No-op / rejection / concurrency / retry: already Active is a no-op with no version bump; invalid or unauthorized is rejected with the reason; a stale version is rejected (today not checked, DEF-1); a retry should carry an idempotency key (today none, DEF-1).
- UI states: switch, pending, validation error, conflict, permission denied.
- Rules: automation.md §9 to §12, §10 AUT-003, §61.2; AIREQ011.
- Acceptance: Given a valid Draft, When enabled, Then the next matching fact runs it. Given an executor who lost access, Then enable is rejected.
- Evidence: BE PATCH `/api/v1/workspaces/{workspaceId}/automations/{automationId}/enabled` (Automation.Rules.SetEnabled) · FE FE-BS, FE-WA `setEnabled` · Design `Final-Automation` · Tests T-AD `AutomationRuleActivationInvariantTests`, T-AA `AutomationPermissionMarkerTests` · Real e2e E2E-AUT
- State: HARDENING — no expected version or idempotency (DEF-1).

### UC-AUT-E02 Disable a rule
- Capability: AUT-005 Enable/disable lifecycle
- Actors and permission: ManageBoard or ManageWorkspaceSettings only (kill switch; ManageIntegrations is not needed).
- Preconditions: rule exists.
- Input: `isEnabled=false`.
- Output: Disabled; no new executions; running ones finish; history kept; the confirmation names the next run that will not happen.
- Main flow: PATCH enabled false.
- No-op / rejection / concurrency / retry: already Disabled is a no-op.
- UI states: switch, destructive confirmation ("Tắt quy tắc?"), pending.
- Rules: automation.md §10 AUT-003; AIREQ012.
- Acceptance: Given an Active rule, When disabled, Then a later fact makes no execution and old runs stay readable.
- Evidence: BE same route as E01 · FE FE-BS · Design `Final-M-Automation` · Tests T-AD `AutomationRuleLifecycleTests` · Real e2e E2E-AUT
- State: HARDENING — as E01.

### UC-AUT-E03 A rule stops when its runner loses access
- Capability: AUT-005 Enable/disable lifecycle
- Actors and permission: system reacting to Workspaces facts.
- Preconditions: Active rule whose executor is removed, suspended, demoted, or can no longer see or act on the target.
- Input: facts `workspace.member.removed`, `.suspended`, `.role-changed`; or a failed authority check at run.
- Output: rule Disabled with a recorded reason, shown in the list; it stays Disabled until someone enables it (the re-check clears the reason); re-activating the executor does not re-enable it.
- Main flow: consumers disable; the list shows `disabledReason`.
- No-op / rejection / concurrency / retry: a duplicate fact is a no-op.
- UI states: "Hồ Quang Minh không còn quyền", "Đổi người chạy rồi bật lại".
- Rules: automation.md §61.2 AUT-020, §62; AIREQ049, AIREQ050.
- Acceptance: Given an Active rule run by Minh, When Minh is removed, Then the rule is Disabled with a reason.
- Evidence: BE `WorkspaceMemberRemovedAutomationConsumer` and siblings, `AutomationExecutorLifecycle` · FE FE-BS (reason text) · Design `Final-M-Automation` · Tests T-AA `AutomationExecutorIdentityTests` · Real e2e none
- State: INTEGRATING — no real e2e.

### UC-AUT-E04 Change the person a rule runs as
- Capability: AUT-005 Enable/disable lifecycle
- Actors and permission: a board Manager picks a person who can edit the board; a workspace Admin may take over.
- Preconditions: the new runner holds the action permissions now.
- Input: runner user id, expected version.
- Output: executor changed; version +1; the change recorded.
- Main flow: today saving an edit makes the editor the executor (A04); there is no direct command.
- No-op / rejection / concurrency / retry: the same runner is a no-op.
- UI states: "Chạy với quyền của … Đổi người chạy".
- Rules: automation.md §61.2; owner decision 44 (OD6); AIREQ049.
- Acceptance: Given a Disabled rule, When a Manager sets runner Phúc who can edit, Then enable succeeds as Phúc.
- Evidence: BE `AutomationRule.Revise` (editor binds) · FE FE-BS (runner row) · Design `Final-Automation` · Tests T-AA `AutomationExecutorIdentityTests` · Real e2e none
- State: IMPLEMENTING — no explicit runner command; implicit takeover only.

### UC-AUT-E05 A rule stops after repeated failures
- Capability: AUT-005 Enable/disable lifecycle
- Actors and permission: system.
- Preconditions: the 5 latest finished executions since the rule was last enabled or edited all failed (`ConsecutiveFailureLimit`).
- Input: execution outcomes.
- Output: rule Disabled with reason "repeated failures"; skips do not count.
- Main flow: the evaluator checks before starting an execution.
- No-op / rejection / concurrency / retry: enabling again resets the window.
- UI states: reason in the list.
- Rules: automation.md §71; AIREQ116.
- Acceptance: Given 5 failed runs, When the next fact arrives, Then the rule is Disabled with the reason.
- Evidence: BE `AutomationGuardOptions`, `AutomationRule.DisableAutomatically`, `N8nAutomationRuleEvaluator` · FE FE-BS (reason text only) · Design none · Tests T-AA `AutomationRuleEvaluatorGuardTests` · Real e2e none
- State: INTEGRATING — no notice to the owner when it happens.

### UC-AUT-E06 Pause and resume rules with the board
- Capability: AUT-005 Enable/disable lifecycle
- Actors and permission: system on board archive and unarchive.
- Preconditions: board archived or restored.
- Input: board fact.
- Output: the board's Active rules are paused with reason "board archived" and resumed on unarchive; rules a person disabled stay disabled.
- Main flow: consumer on the board archived and unarchived facts.
- No-op / rejection / concurrency / retry: a duplicate fact is a no-op.
- UI states: reason text.
- Rules: automation.md §101; owner decision (board archive pauses rules and integration sync).
- Acceptance: Given an Active rule, When the board is archived and later restored, Then it did not run in between and is Active again.
- Evidence: BE none found · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — no consumer.

### UC-AUT-E07 Check a rule before enabling it
- Capability: AUT-005 Enable/disable lifecycle
- Actors and permission: as E01.
- Preconditions: rule saved.
- Input: rule id.
- Output: a list of checks: trigger valid, fields exist, recipients can see the board, runner still has permission; blocking and non-blocking items (for example a connection needing reauthorization).
- Main flow: "Kiểm tra cấu hình" calls a read-only check; Enable repeats it on the server.
- No-op / rejection / concurrency / retry: the check never changes data; the server check on enable is the authority (AUT-002).
- UI states: loading, blocking, warning, all clear.
- Rules: automation.md §7 AUT-002; AIREQ011, AIREQ095 (frontend validation is never canonical).
- Acceptance: Given a rule with an archived field, When checked, Then the field is listed as blocking and enable stays refused.
- Evidence: BE none (no dry-run endpoint) · FE FE-BS (mock handlers) · Design `Final-Automation` · Tests none · Real e2e none
- State: IMPLEMENTING — mock-only UI path; no backend check endpoint.

## Capability F. Source-event subscription and matching

### UC-AUT-F01 One fact starts each matching rule once
- Capability: AUT-006 Source-event subscription/matching
- Actors and permission: system.
- Preconditions: Active rules of the fact's workspace.
- Input: a completed Work Management fact delivered by the outbox.
- Output: one execution per matching rule; identity from rule and source event id.
- Main flow: the consumer loads Active rules, filters by trigger type and filter, then conditions, creates executions.
- No-op / rejection / concurrency / retry: a redelivered or concurrent duplicate fact creates no second execution (AUT-009, AUT-024); the source transaction never runs automation inline (AUT-006).
- UI states: none.
- Rules: automation.md §18 AUT-006, §28 AUT-009, §75 AUT-024; AIREQ027, AIREQ028, AIREQ047.
- Acceptance: Given a fact delivered twice, Then exactly one execution exists for the rule.
- Evidence: BE `N8nAutomationRuleEvaluator`, `AutomationConsumerDefinitions` · FE none · Design none · Tests T-AI `AutomationWorkEventRuntimeChainIntegrationTests`, `AutomationTriggerMatrixIntegrationTests` · Real e2e E2E-AUT (trigger to action)
- State: HARDENING — a durable uniqueness constraint (AIREQ028) is not confirmed in the migrations.

### UC-AUT-F02 Filter by group on the trigger
- Capability: AUT-006 Source-event subscription/matching
- Actors and permission: as A01.
- Preconditions: the group is on the rule's board.
- Input: trigger configuration `groupId`.
- Output: only facts for that group match.
- Main flow: `AutomationTriggerFilter` compares.
- No-op / rejection / concurrency / retry: a filter group of another board is rejected at save.
- UI states: group picker.
- Rules: automation.md §19; AIREQ018.
- Acceptance: Given filter Done, When an item moves to Doing, Then no execution.
- Evidence: BE `AutomationTriggerFilter`, `IWorkActionPort.EnsureBoardGroupAsync` · FE FE-BS · Design `Final-Automation` · Tests T-AA `AutomationBoardScopeTests` · Real e2e E2E-AUT
- State: HARDENING — as B02.

### UC-AUT-F03 A fact from another workspace never matches
- Capability: AUT-006 Source-event subscription/matching
- Actors and permission: system.
- Preconditions: rules in two workspaces.
- Input: a fact of workspace W1.
- Output: only W1 rules are loaded and may run (tenant context restored on the consumer).
- Main flow: tenant consume filter and RLS.
- No-op / rejection / concurrency / retry: a fact with a missing tenant is rejected.
- UI states: none.
- Rules: automation.md §62; RULE.md tenant invariants; AIREQ104.
- Acceptance: Given the same group id value in two workspaces, When a W1 fact arrives, Then W2 rules do not run.
- Evidence: BE `TenantContextConsumeFilter`, `AutomationRuleConfiguration` (RLS) · FE none · Design none · Tests T-AA `AutomationBoardScopeAuthorityTests` · Real e2e none
- State: INTEGRATING — no dedicated cross-tenant integration test for the Automation consumers found.

## Capability G. Execution creation

### UC-AUT-G01 Each execution freezes the rule version
- Capability: AUT-007 Execution creation
- Actors and permission: system.
- Preconditions: a matching fact and an Active rule.
- Input: rule configuration at that moment.
- Output: an execution with the rule version and a configuration snapshot; it finishes on that snapshot.
- Main flow: the evaluator creates the execution in the same transaction as the intent.
- No-op / rejection / concurrency / retry: later edits never change a running execution.
- UI states: run row "phiên bản N".
- Rules: automation.md §13 AUT-004, §48 AUT-016; AIREQ014, AIREQ029.
- Acceptance: Given a run on version 1, When version 2 is saved mid-run, Then the run records version 1.
- Evidence: BE `AutomationExecution.RuleVersion`, `AutomationRuleSnapshot` · FE FE-BS `automation-log.tsx` · Design `Final-Automation` · Tests T-AD `AutomationExecutionVersionTests`, T-AI `CreateAutomationRulePipelineTests` · Real e2e E2E-AUT
- State: HARDENING — as A04.

### UC-AUT-G02 The runner is re-checked on every execution
- Capability: AUT-007 Execution creation
- Actors and permission: system as executor.
- Preconditions: Active rule.
- Input: executor id, action, target.
- Output: the action runs as the executor only if they still hold the permission and active membership; otherwise the rule is Disabled with a reason and nothing is changed.
- Main flow: authority check immediately before each action; the trigger actor is never the principal.
- No-op / rejection / concurrency / retry: no cached allow is reused.
- UI states: reason in the list.
- Rules: automation.md §60, §61, §62 AUT-020; AIREQ048 to AIREQ050.
- Acceptance: Given an executor demoted to Viewer, When the next fact arrives, Then nothing moves and the rule is Disabled.
- Evidence: BE `AutomationExecutorAuthority.EnsureCanExecuteAsync` · FE none · Design `Final-Automation` (runner card) · Tests T-AA `AutomationExecutorIdentityTests`, T-AI `AutomationMoveItemExecutorCompositionIntegrationTests` · Real e2e none
- State: INTEGRATING — no real e2e.

### UC-AUT-G03 Chains and runaway rules are bounded
- Capability: AUT-007 Execution creation
- Actors and permission: system.
- Preconditions: a fact caused by an execution's action.
- Input: causation chain, execution rate.
- Output: a chain deeper than 3 hops does not start; a rule starting more than 60 executions in 60 seconds stops starting new ones.
- Main flow: the evaluator reads ancestry and the window.
- No-op / rejection / concurrency / retry: a self-trigger by the rule's own action is stopped by depth.
- UI states: the log notes the stop (not built).
- Rules: automation.md §66 to §71 AUT-022; AIREQ037, AIREQ116.
- Acceptance: Given two rules that move an item back and forth, Then the chain stops at depth 3.
- Evidence: BE `AutomationGuardOptions`, `N8nAutomationRuleEvaluator` · FE none · Design none · Tests T-AA `AutomationRuleEvaluatorGuardTests` · Real e2e none
- State: INTEGRATING — a stopped trigger is silent to users.

### UC-AUT-G04 Concurrent claims never duplicate work
- Capability: AUT-007 Execution creation
- Actors and permission: system.
- Preconditions: two workers receive the same execution intent.
- Input: execution id.
- Output: one worker claims and acts; the other sees the claim.
- Main flow: a durable claim before the provider or target effect.
- No-op / rejection / concurrency / retry: a stale claim is recovered under a governed procedure, not by blind re-fire (AIREQ072, AIREQ073).
- UI states: none.
- Rules: automation.md §74, §75 AUT-024; AIREQ028, AIREQ072.
- Acceptance: Given two parallel deliveries, Then the effect happens once.
- Evidence: BE `N8nDispatchUseCase` claim logic · FE none · Design none · Tests T-AI `AutomationN8nDurabilityIntegrationTests` · Real e2e none
- State: IMPLEMENTING — claim proven for n8n only; MoveItem and work actions rely on operation ids.

### UC-AUT-G05 An automated change names the rule and the runner
- Capability: AUT-007 Execution creation
- Actors and permission: any viewer of the changed item.
- Preconditions: an action changed an item.
- Input: execution.
- Output: the item fact names the executor as actor and carries `automationRuleId` and `automationExecutionId`; the item history reads "automation <rule> on behalf of <runner>".
- Main flow: Work Management attributes on the public action.
- No-op / rejection / concurrency / retry: none.
- UI states: history line.
- Rules: automation.md §61.2 Attribution, §68 Causation; AIREQ054.
- Acceptance: Given a rule move, Then the item history shows the rule and the runner.
- Evidence: BE `WorkItemActionAdapter` · FE item history (Collaboration) not checked for the rule label · Design `Final-Automation` · Tests T-AI `AutomationMoveItemExecutorCompositionIntegrationTests` · Real e2e none
- State: INTEGRATING — the rule label in the item history UI is not proven.

### UC-AUT-G06 Test-run a rule
- Capability: AUT-007 Execution creation
- Actors and permission: as A01.
- Preconditions: rule saved.
- Input: rule id and a sample item.
- Output: a dry run showing which steps would run; no external effect.
- Main flow: none built.
- No-op / rejection / concurrency / retry: a dry run never writes data.
- UI states: result panel.
- Rules: the frozen P5 decisions of the execution spec (no test-preview endpoint in P5).
- Acceptance: Given a sample item, When tested, Then the panel lists the steps and nothing changes.
- Evidence: BE none · FE `AutomationRuleRepository.test` in `frontend/packages/product/automation/state/src/data/repositories.ts` (no adapter) · Design none · Tests none · Real e2e none
- State: DEFINED — deferred by the P5 decisions.

## Capability H. Execution state machine

### UC-AUT-H01 An execution moves Queued, Running, then ends
- Capability: AUT-008 Execution state machine
- Actors and permission: system.
- Preconditions: execution created.
- Input: step outcomes.
- Output: status Queued, Running, Succeeded, Failed or Cancelled; invalid transitions are rejected; success only after durable effect evidence.
- Main flow: the consumer starts, runs steps, settles in a second transaction.
- No-op / rejection / concurrency / retry: a finished execution never changes state.
- UI states: queued, running, succeeded, failed, cancelled badges.
- Rules: automation.md §49 to §51 AUT-017; AIREQ030, AIREQ032.
- Acceptance: Given an execution that ended Succeeded, When a late duplicate arrives, Then state is unchanged.
- Evidence: BE `AutomationExecution`, `AutomationExecutionStatus` · FE FE-BS `automation-log.tsx` · Design `Final-Automation` · Tests T-AD `AutomationExecutionTests` · Real e2e E2E-AUT
- State: HARDENING — as D01.

### UC-AUT-H02 See each step of a run
- Capability: AUT-008 Execution state machine
- Actors and permission: as J01.
- Preconditions: execution with steps.
- Input: execution.
- Output: steps with index, type, status and safe error.
- Main flow: steps stored as progress and read by the history query.
- No-op / rejection / concurrency / retry: error text is redacted (AUT-035).
- UI states: step list, error line.
- Rules: automation.md §41, §52; AIREQ031, AIREQ113.
- Acceptance: Given a failed step 2, Then the log shows step 1 Succeeded and step 2 with its reason.
- Evidence: BE `AutomationExecutionProgress`, `AutomationExecutionDto.Steps` · FE FE-BS `automation-run-sheet.tsx`, FE-WA `runs` · Design `Final-M-Automation` · Tests T-AA `AutomationMultiActionTests` · Real e2e E2E-AUT
- State: HARDENING — step status is stored in the `LastResponse` JSON, not in `AutomationExecutionStep` rows.

### UC-AUT-H03 Partial failure keeps what already happened
- Capability: AUT-008 Execution state machine
- Actors and permission: as J01.
- Preconditions: a multi-action execution with a failing step.
- Input: outcomes.
- Output: execution Failed; earlier effects stay; no automatic rollback.
- Main flow: see D06.
- No-op / rejection / concurrency / retry: the notice of step 1 is not sent again on a rerun (I03).
- UI states: "Bước 1 thành công, Bước 2 lỗi".
- Rules: automation.md §52, §53; AIREQ031, AIREQ033.
- Acceptance: as D06.
- Evidence: BE `AutomationMultiActionTests` subject · FE FE-BS · Design `Final-M-Automation` · Tests T-AA `AutomationMultiActionTests` · Real e2e none
- State: INTEGRATING — no real e2e of a failing step.

### UC-AUT-H04 Cancel a queued execution
- Capability: AUT-008 Execution state machine
- Actors and permission: board Manager or Owner and Admin.
- Preconditions: execution Queued (not Running).
- Input: execution id, expected version.
- Output: status Cancelled; no step runs.
- Main flow: a cancel command (not built).
- No-op / rejection / concurrency / retry: cancelling a finished execution is rejected; a stale version is rejected.
- UI states: cancel action, confirmation.
- Rules: automation.md §49; AIREQ030.
- Acceptance: Given a Queued execution, When cancelled, Then it ends Cancelled and no action runs.
- Evidence: BE `AutomationExecution` Cancelled state and event, no endpoint · FE `AutomationExecutionRepository.cancel?` unimplemented · Design none · Tests T-AD `AutomationExecutionTests` · Real e2e none
- State: IMPLEMENTING — domain only.

### UC-AUT-H05 An unknown outcome is held, never re-fired
- Capability: AUT-008 Execution state machine
- Actors and permission: system; an operator through governed recovery.
- Preconditions: a dispatch was interrupted or the provider did not answer clearly.
- Input: the attempt.
- Output: execution Failed with "outcome unknown, reconciliation required"; the effect is not repeated blindly.
- Main flow: the n8n use case settles unknown outcomes.
- No-op / rejection / concurrency / retry: retry only after reconciliation (AUT-018).
- UI states: "provider outcome unknown" (automation.md §106) not built.
- Rules: automation.md §56 AUT-018; AIREQ036, AIREQ070, AIREQ117.
- Acceptance: Given a timeout after sending, Then the execution shows unknown outcome and n8n is not called again.
- Evidence: BE `N8nDispatchUseCase` · FE none · Design none · Tests T-AI `AutomationN8nDurabilityIntegrationTests` · Real e2e none
- State: IMPLEMENTING — no UI state; no governed reconciliation action.

## Capability I. Business retry and failure policy

### UC-AUT-I01 Retryable failures retry within one budget
- Capability: AUT-009 Business retry/failure policy
- Actors and permission: system.
- Preconditions: a transient failure (network, 5xx, rate limit).
- Input: outcome category.
- Output: execution re-queued with an attempt count; transport redelivery and business retry share one budget.
- Main flow: the consumer records retryable evidence and releases the claim.
- No-op / rejection / concurrency / retry: the same operation identity is reused; no second notification for one logical outcome.
- UI states: "đang thử lại, lần N" (not built).
- Rules: automation.md §54, §55 AUT-018; AIREQ034, AIREQ035.
- Acceptance: Given a 503 then 200, Then one effect and attempt count 2.
- Evidence: BE `AutomationExecution.RecordRetryableDispatchFailure`, `AutomationConsumerDefinitions` (`UseMessageRetry`) · FE none (`attemptCount` is in the DTO) · Design none · Tests T-AD `AutomationExecutionRetryTests`, T-AI `AutomationN8nDurabilityIntegrationTests` · Real e2e none
- State: INTEGRATING — no UI for the retrying state.

### UC-AUT-I02 A terminal failure shows a safe, useful reason
- Capability: AUT-009 Business retry/failure policy
- Actors and permission: as J01.
- Preconditions: a terminal failure (invalid target, revoked permission, incompatible field, target deleted).
- Input: failure.
- Output: execution Failed with a stable reason class and a message free of secrets and raw provider text.
- Main flow: classify, redact, store.
- No-op / rejection / concurrency / retry: not retried.
- UI states: failed row with reason and next step.
- Rules: automation.md §106, §107 AUT-035; AIREQ033, AIREQ113.
- Acceptance: Given a deleted target group, Then the log says the group no longer exists.
- Evidence: BE `AutomationExecution.Fail`, `AutomationExecutionDto.Error` · FE FE-BS `automation-log.tsx` · Design `Final-M-Automation` · Tests T-AD `AutomationExecutionTests` · Real e2e none
- State: INTEGRATING — failure classes are free text, not a stable enum.

### UC-AUT-I03 Rerun a failed step
- Capability: AUT-009 Business retry/failure policy
- Actors and permission: board Manager.
- Preconditions: execution Failed with a retryable or fixed cause; steps before the failure succeeded.
- Input: execution id, step index, expected version.
- Output: only the failed step runs again on the same rule version snapshot; at most 3 manual reruns per step.
- Main flow: "Chạy lại bước lỗi" after fixing the cause (for example reauthorize a connection).
- No-op / rejection / concurrency / retry: steps that already succeeded are never sent again; a stale version is rejected; the same idempotency key returns the first result.
- UI states: button disabled until fixed, pending, result.
- Rules: automation.md §54, §55 AUT-018; owner decision 44 (OD7); AIREQ035.
- Acceptance: Given step 1 Succeeded and step 2 Failed, When rerun, Then only step 2 executes.
- Evidence: BE none · FE FE-BS `automation-run-sheet.tsx` (mock handlers), `AutomationExecutionRepository.retry?` · Design `Final-M-Automation` · Tests none · Real e2e none
- State: IMPLEMENTING — mock-only UI path.

### UC-AUT-I04 A gone target or revoked permission is terminal
- Capability: AUT-009 Business retry/failure policy
- Actors and permission: system.
- Preconditions: the action target is deleted, archived or inaccessible.
- Input: failure.
- Output: terminal Failed; for a lost runner permission also E03.
- Main flow: authority and target checks fail the step.
- No-op / rejection / concurrency / retry: never retargeted.
- UI states: reason.
- Rules: automation.md §54, §101 AUT-034; AIREQ033.
- Acceptance: Given the target group deleted, Then no retry and the reason names it.
- Evidence: BE `AutomationMoveItemUseCase`, `WorkItemActionAdapter` · FE none · Design none · Tests T-AA `AutomationBoardScopeAuthorityTests` · Real e2e none
- State: IMPLEMENTING — the terminal versus retryable class is not a stable contract.

## Capability J. Execution history and query

### UC-AUT-J01 Read the recent runs of a rule
- Capability: AUT-010 Execution history/query
- Actors and permission: ManageBoard (with `boardId`) or ManageWorkspaceSettings. Members and Guests do not read history (automation.md §61.1).
- Preconditions: rule exists, or was deleted less than 90 days ago.
- Input: `automationId`, `page`, `pageSize` (clamped 1 to 100), `boardId`.
- Output: executions newest first with status, attempts, payload, error, version, steps.
- Main flow: open the run log of a rule.
- No-op / rejection / concurrency / retry: a board claim mismatch is NotFound; a deleted rule past 90 days is NotFound.
- UI states: loading, empty, error with retry, permission denied.
- Rules: automation.md §57, §58 AUT-019; AIREQ038, AIREQ109.
- Acceptance: Given 25 runs, When page 1 of size 20 is read, Then the 20 newest return.
- Evidence: BE GET `/api/v1/automations/{automationId}/executions` (Automation.Executions.List), `GetAutomationExecutionsQueryHandler` · FE FE-BS `automation-log.tsx`, FE-WA `runs` · Design `Final-Automation` · Tests T-AA `AutomationRuleManagementAuthorityTests` · Real e2e E2E-AUT
- State: HARDENING — offset paging, no total or cursor (DEF-3); the payload may hold item data and its redaction is not proven.

### UC-AUT-J02 Open one run
- Capability: AUT-010 Execution history/query
- Actors and permission: as J01.
- Preconditions: execution in the list.
- Input: execution id.
- Output: trigger fact, who triggered it, runner, rule version, steps, outcome.
- Main flow: the run sheet is built from the list row; there is no detail endpoint.
- No-op / rejection / concurrency / retry: none.
- UI states: sheet, loading, error.
- Rules: automation.md §57; AIREQ038.
- Acceptance: Given a run, When opened, Then the steps and version are shown.
- Evidence: BE none for detail (list only) · FE FE-BS `automation-run-sheet.tsx` · Design `Final-M-Automation` · Tests FE-BS component tests · Real e2e none
- State: INTEGRATING — triggerer and item reference come from the payload only.

### UC-AUT-J03 See all runs of a workspace and filter them
- Capability: AUT-010 Execution history/query
- Actors and permission: Owner and Admin (ManageWorkspaceSettings); a board Manager for one board.
- Preconditions: none.
- Input: status, rule, date range, cursor, limit.
- Output: runs across rules, newest first, with counts per status.
- Main flow: a new feed; the board feed first (owner decision 44).
- No-op / rejection / concurrency / retry: cursor paging; unauthorized is NotFound. (enterprise baseline)
- UI states: loading, empty, error, filter chips.
- Rules: automation.md §57, §92; Lead decision (2026-10-30): a board run feed first, the workspace feed after.
- Acceptance: Given runs of three rules, When filtered Failed, Then only failed runs return.
- Evidence: BE none · FE `AutomationExecutionRepository.listHistory` unimplemented · Design none · Tests none · Real e2e none
- State: DEFINED — (enterprise baseline).

### UC-AUT-J04 Keep and purge history
- Capability: AUT-010 Execution history/query
- Actors and permission: system.
- Preconditions: executions exist.
- Input: retention policy.
- Output: history of a deleted rule stays readable 90 days; history of live rules is kept 400 days, then purged; purge never touches a running execution.
- Main flow: a scheduled purge.
- No-op / rejection / concurrency / retry: purge is idempotent.
- UI states: the log notes the retention.
- Rules: automation.md §59; Lead decision (2026-10-30): 400 days for live rules (enterprise baseline).
- Acceptance: Given a run older than 400 days, When purge runs, Then it is gone.
- Evidence: BE the 90-day read cutoff only (`DeletedRuleRetention`), no purge job · FE none · Design none · Tests none · Real e2e none
- State: IMPLEMENTING — read cutoff only; no purge.

### UC-AUT-J05 Rule changes appear in the workspace audit log
- Capability: AUT-010 Execution history/query
- Actors and permission: Owner and Admin read the audit log.
- Preconditions: a rule is created, edited, enabled, disabled, deleted or auto-disabled.
- Input: the change.
- Output: an audit entry with actor, rule, and a before and after summary; run history stays separate.
- Main flow: Governance records from Automation facts.
- No-op / rejection / concurrency / retry: duplicate facts create one entry.
- UI states: audit list.
- Rules: automation.md §64, §65 AUT-021. (enterprise baseline)
- Acceptance: Given a rule enabled by Hoa, Then the audit log shows Hoa enabled it.
- Evidence: BE domain facts exist (`AutomationRuleEnabledDomainEvent`); no audit consumer found · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — (enterprise baseline).

## Capability K. Automation authoring frontend

### UC-AUT-K01 See the rules of a board
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: board Manager; others see a permission-denied state.
- Preconditions: board settings open.
- Input: board id.
- Output: rule cards with trigger, actions, runner, enabled switch, last run.
- Main flow: as A03.
- No-op / rejection / concurrency / retry: an account or workspace switch clears the list; a late response of the old scope is dropped.
- UI states: loading, empty, error, permission denied, read-only.
- Rules: DESIGN.md automation interfaces (section 23); AIREQ099; AIREQ096.
- Acceptance: Given a board with two rules, Then two cards show with the correct switch state.
- Evidence: BE as A03 · FE FE-BS `board-settings-container.tsx` · Design `Final-Automation` · Tests `workspace-rules.interaction.component.test.tsx` under FE-BS `__tests__/` · Real e2e E2E-AUT
- State: HARDENING — scope-transition behaviour not proven.

### UC-AUT-K02 Compose a rule: trigger, conditions, actions
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: board Manager.
- Preconditions: board loaded with groups, statuses, fields and members.
- Input: pickers for trigger, conditions, up to five actions, name.
- Output: a request as A01 or A04; the server stays the authority on validity.
- Main flow: build, save draft, save and enable, or save as a new version.
- No-op / rejection / concurrency / retry: unsupported trigger or action types are not offered (DEF-5, DEF-14); server errors show on the field.
- UI states: validation error, pending, conflict, permission denied, keyboard and screen reader flow.
- Rules: automation.md §37, §38; AIREQ016, AIREQ095; DESIGN.md section 23.
- Acceptance: Given a valid form, When saved, Then the rule appears; a server rejection shows beside the field.
- Evidence: BE as A01, A04 · FE FE-BS `automation-editor.tsx`, `automation-flow.tsx`, `automation-pickers.tsx` · Design `Final-Automation` · Tests FE-BS `__tests__/` · Real e2e E2E-AUT
- State: HARDENING — no conflict state; no axe proof; 1 px diff not measured.

### UC-AUT-K03 Read the run log of a rule
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: as J01.
- Preconditions: rule selected.
- Input: rule id.
- Output: runs with status, version, time, steps; a note says the run log is not the audit log.
- Main flow: as J01.
- No-op / rejection / concurrency / retry: error with a retry button.
- UI states: loading, empty, error, permission denied.
- Rules: automation.md §58 AUT-019.
- Acceptance: Given runs, Then the log lists them newest first.
- Evidence: BE as J01 · FE FE-BS `automation-log.tsx`, `automation-log-parts.tsx` · Design `Final-Automation` · Tests FE-BS `__tests__/` · Real e2e E2E-AUT (the list is read after the run)
- State: INTEGRATING — the real e2e reads the list but does not assert the log screen.

### UC-AUT-K04 See why a rule cannot be enabled
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: board Manager.
- Preconditions: enable rejected.
- Input: server error.
- Output: a blocking message naming the cause and the fix action.
- Main flow: the error is mapped from the API problem code.
- No-op / rejection / concurrency / retry: none.
- UI states: blocking notice with actions.
- Rules: automation.md §7 AUT-002, §106.
- Acceptance: Given an archived field, When enable fails, Then the notice names it.
- Evidence: BE rejection codes in `AutomationRuleCodes.cs` · FE FE-BS (message copy) · Design `Final-Automation` · Tests FE-BS `__tests__/` · Real e2e none
- State: IMPLEMENTING — structured reasons are not in the API (see UC-AUT-D10).

### UC-AUT-K05 A non-manager sees a denied state
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: Member, Guest, Viewer.
- Preconditions: board visible.
- Input: none.
- Output: no rule data; the tab explains that only managers open it.
- Main flow: the list call returns NotFound; the UI shows permission denied, not an empty list.
- No-op / rejection / concurrency / retry: none.
- UI states: permission denied.
- Rules: automation.md §61.1; AIREQ096.
- Acceptance: Given a Member, When opening the tab, Then a denied notice shows and no rule names leak.
- Evidence: BE `AutomationPermissionMarkerTests` subject · FE FE-BS · Design `Final-Settings` ("Chỉ quản lý board mở được trang này") · Tests T-AA `AutomationPermissionMarkerTests` · Real e2e none
- State: INTEGRATING — no real e2e of the denied path.

### UC-AUT-K06 Phone: read rules, switch them, read the run log
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: board Manager.
- Preconditions: mobile client with a bearer session.
- Input: board id.
- Output: rules, enable and disable, run log; editing stays on web.
- Main flow: as K01 and E01 on the phone.
- No-op / rejection / concurrency / retry: offline shows the read cache only (owner decision 47).
- UI states: loading, empty, error, offline read-only.
- Rules: owner decisions 44 (OD10), 47; AIREQ096.
- Acceptance: Given a phone, Then a Manager can disable a rule and read its log.
- Evidence: BE as A03, E02, J01 · FE `frontend/packages/product/automation/mobile/src/components/mobile-automation-screen.tsx` (placeholder) · Design `Final-M-Automation` · Tests `automation-mobile-contract.mobile.test.ts` · Real e2e none
- State: IMPLEMENTING — the screen is a placeholder.

### UC-AUT-K07 Workspace automation page
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: Owner and Admin.
- Preconditions: workspace open.
- Input: none.
- Output: all rules across boards, filter by board and status.
- Main flow: list without `boardId`.
- No-op / rejection / concurrency / retry: as A03.
- UI states: loading, empty, error, denied.
- Rules: automation.md §8; AIREQ016. (enterprise baseline)
- Acceptance: Given three boards with rules, Then the page lists all.
- Evidence: BE `GetWorkspaceAutomationsQuery` without a board · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — (enterprise baseline).

### UC-AUT-K08 Edit a schedule
- Capability: AUT-011 Automation authoring frontend
- Actors and permission: board Manager.
- Preconditions: B05 exists.
- Input: time, days, zone, missed-run choice.
- Output: schedule saved and the next run shown.
- Main flow: the schedule section in the editor.
- No-op / rejection / concurrency / retry: see B05.
- UI states: validation error, next-run preview.
- Rules: automation.md §24, §25 AUT-008, §26; AIREQ040.
- Acceptance: Given daily 09:00 Hà Nội, Then the editor shows the next 09:00.
- Evidence: BE none · FE FE-BS `automation-flow.tsx` (mock) · Design `Final-Automation` · Tests FE-BS `__tests__/` · Real e2e none
- State: IMPLEMENTING — blocked on the B05 runtime.

## Capability L. Realtime execution status

### UC-AUT-L01 Watch a run progress live
- Capability: AUT-012 Realtime execution status
- Actors and permission: as J01.
- Preconditions: log open.
- Input: events `automation.execution.started`, `step-updated`, `completed`, `failed` (frames carry no payload; the event only invalidates the query).
- Output: the log refetches and shows the new state.
- Main flow: the realtime adapter invalidates the execution query keys.
- No-op / rejection / concurrency / retry: duplicate and out-of-order frames only cause a refetch; a stale view never triggers a new execution.
- UI states: live badge.
- Rules: automation.md §89, §90 AUT-031; AIREQ100, AIREQ101.
- Acceptance: Given the log open, When a run finishes, Then its row updates without reload.
- Evidence: BE no producer maps domain events to realtime (DEF-15) · FE `frontend/packages/product/automation/state/src/realtime/execution-adapter.ts` · Design none · Tests `automation-state.unit.test.ts` · Real e2e none
- State: IMPLEMENTING — no backend producer.

### UC-AUT-L02 Recover after a realtime gap
- Capability: AUT-012 Realtime execution status
- Actors and permission: as J01.
- Preconditions: connection dropped and resumed.
- Input: reconnect.
- Output: the log refetches from the durable query.
- Main flow: reconnect triggers invalidation.
- No-op / rejection / concurrency / retry: a late frame of the previous workspace never overwrites the new scope.
- UI states: reconnecting banner.
- Rules: automation.md §90; AIREQ101.
- Acceptance: Given a missed event, When reconnected, Then the log shows the true state.
- Evidence: BE durable query J01 · FE `automationQueryKeys` in `frontend/packages/product/automation/state/src/query/keys.ts` · Design none · Tests `automation-query-keys.unit.test.ts` · Real e2e none
- State: IMPLEMENTING — the keys exist in the island package; the real UI uses Work Management keys.

## Capability M. Entitlement integration

### UC-AUT-M01 Creating a rule respects the plan limit
- Capability: AUT-013 Entitlement integration
- Actors and permission: as A01.
- Preconditions: a plan capacity fact from Billing (`AutomationRule` capability).
- Input: create.
- Output: allowed, or rejected with "limit reached"; the capacity consume and the rule create share one transaction.
- Main flow: ask Billing for the capability, never for the plan name.
- No-op / rejection / concurrency / retry: concurrent creates lose the last slot cleanly (rollback); a retry reuses the rule id as operation id.
- UI states: limit-reached notice with an upgrade path.
- Rules: automation.md §72, §73 AUT-023; AIREQ052, AIREQ053.
- Acceptance: Given 0 slots left, When creating, Then rejected and no rule exists.
- Evidence: BE `CreateAutomationRuleCommandHandler`, `IBillingCapabilityFacts` · FE generic error toast only · Design none · Tests T-AA `CreateAutomationRuleBillingGateTests` · Real e2e none
- State: INTEGRATING — no limit-reached UI.

### UC-AUT-M02 Deleting a rule frees its slot
- Capability: AUT-013 Entitlement integration
- Actors and permission: as A05.
- Preconditions: rule deleted.
- Input: delete.
- Output: one slot released with a stable release identity.
- Main flow: see A05.
- No-op / rejection / concurrency / retry: a retry never releases twice.
- UI states: none.
- Rules: AIREQ053.
- Acceptance: Given a retried delete, Then capacity rises by one.
- Evidence: BE `DeleteAutomationRuleCommandHandler.ReleaseOperationId` · FE none · Design none · Tests T-AA `CreateAutomationRuleBillingGateTests` · Real e2e none
- State: INTEGRATING — the retry-after-delete path is not proven.

### UC-AUT-M03 What happens to rules on a plan downgrade
- Capability: AUT-013 Entitlement integration
- Actors and permission: system on Billing facts.
- Preconditions: the account drops below its rule limit.
- Input: Billing fact.
- Output: Lead decision (2026-10-30): existing rules keep running and stay editable; new rules are blocked until under the limit; premium triggers or actions are paused with a visible reason; history is kept.
- Main flow: a consumer on the capability-changed fact.
- No-op / rejection / concurrency / retry: repeated facts are no-ops.
- UI states: limit notice, paused reason.
- Rules: automation.md §72, §73 AUT-023.
- Acceptance: Given 12 rules and a limit of 10, Then all 12 run and creating the 13th is rejected.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — the rule text is in Part 5.

### UC-AUT-M04 Only enabled rules count toward the limit
- Capability: AUT-013 Entitlement integration
- Actors and permission: as A01.
- Preconditions: owner decision 44 (OD3).
- Input: create, enable, disable.
- Output: Draft and Disabled rules use no slot; Enable consumes one and may be rejected at the limit; Disable releases.
- Main flow: move the consume from create to enable.
- No-op / rejection / concurrency / retry: enable is idempotent on the rule id.
- UI states: limit notice on the enable switch.
- Rules: owner decision 44 (OD3); AIREQ053.
- Acceptance: Given a limit of 3 and 3 enabled rules, When creating a Draft, Then it saves; When enabling it, Then rejected.
- Evidence: BE the consume is in create (DEF-6) · FE none · Design none · Tests T-AA `CreateAutomationRuleBillingGateTests` · Real e2e none
- State: IMPLEMENTING — code contradicts the decision.

## Capability N. Hardening and observability

### UC-AUT-N01 Operators see backlog and failure metrics
- Capability: AUT-014 Hardening/observability
- Actors and permission: operators.
- Preconditions: none.
- Input: telemetry.
- Output: queue depth, execution age, failure rate by class, rules auto-disabled; no payload or secret content.
- Main flow: metrics from the consumers.
- No-op / rejection / concurrency / retry: none.
- UI states: operations dashboards.
- Rules: automation.md §92; AIREQ114, AIREQ113.
- Acceptance: Given a growing backlog, Then an alert fires.
- Evidence: BE none confirmed · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — no metrics, alerts or dashboards exist.

### UC-AUT-N02 Follow one fact to its execution by correlation id
- Capability: AUT-014 Hardening/observability
- Actors and permission: operators.
- Preconditions: none.
- Input: correlation id.
- Output: the source event, execution and actions share a correlation id; message, operation and correlation ids stay distinct.
- Main flow: the envelope carries the correlation id.
- No-op / rejection / concurrency / retry: none.
- UI states: none.
- Rules: AIREQ111, AIREQ112.
- Acceptance: Given an execution, Then the logs for the source event and the action share the id.
- Evidence: BE `EnvelopeBuilder`, `AutomationTriggerContext.CausationId` · FE none · Design none · Tests T-AI `AutomationWorkEventRuntimeChainIntegrationTests` · Real e2e none
- State: IMPLEMENTING — not asserted end to end.

### UC-AUT-N03 A stuck provider claim is recovered under control
- Capability: AUT-014 Hardening/observability
- Actors and permission: operators with a governed, audited procedure.
- Preconditions: a queued provider claim stays stale.
- Input: execution id.
- Output: the operator decides reconcile or fail; no automatic re-fire.
- Main flow: a runbook.
- No-op / rejection / concurrency / retry: the freshness of a claim never grants re-fire.
- UI states: none.
- Rules: AIREQ072, AIREQ073, AIREQ117.
- Acceptance: Given a stale claim, Then the system does not call the provider again by itself.
- Evidence: BE `N8nDispatchUseCase` · FE none · Design none · Tests T-AI `AutomationN8nDurabilityIntegrationTests` · Real e2e none
- State: IMPLEMENTING — no operator action exists.

### UC-AUT-N04 No reusable secret in a rule
- Capability: AUT-014 Hardening/observability
- Actors and permission: as A01.
- Preconditions: none.
- Input: action configuration.
- Output: provider secrets live in Integrations behind a reference; rule configuration holds only the connection reference or webhook path; history and logs redact.
- Main flow: validators reject secret-like fields.
- No-op / rejection / concurrency / retry: none.
- UI states: validation error.
- Rules: automation.md §85, §86 AUT-029; AIREQ113.
- Acceptance: Given a config with `apiKey`, Then rejected.
- Evidence: BE `AutomationActionValidator`, `N8nWebhookPath` · FE none · Design none · Tests T-AD `AutomationActionValidatorTests` · Real e2e none
- State: INTEGRATING — secret-like field rejection is not confirmed by a test.

### UC-AUT-N05 Tenant isolation of rules and executions
- Capability: AUT-014 Hardening/observability
- Actors and permission: system.
- Preconditions: two workspaces and two accounts.
- Input: reads and writes.
- Output: a user of W1 never sees, changes or triggers W2 rules or executions; the answer is the same NotFound.
- Main flow: pipeline permission plus RLS on `automation_rules` and `automation_executions`.
- No-op / rejection / concurrency / retry: none.
- UI states: not found.
- Rules: AIREQ104, AIREQ110; RULE.md tenant invariants.
- Acceptance: Given a W2 rule id, When a W1 Manager calls update, Then NotFound.
- Evidence: BE `AutomationRuleConfiguration`, `AutomationExecutionConfiguration` · FE none · Design none · Tests T-AA `AutomationBoardScopeAuthorityTests` · Real e2e none
- State: INTEGRATING — no Automation-specific cross-tenant integration test with Postgres RLS found.

### UC-AUT-N06 Pause all automation of a workspace
- Capability: AUT-014 Hardening/observability
- Actors and permission: Owner and Admin.
- Preconditions: none.
- Input: pause or resume.
- Output: no rule of the workspace starts executions while paused; running ones finish; rules keep their own state.
- Main flow: a workspace switch read by the evaluator.
- No-op / rejection / concurrency / retry: pausing twice is a no-op; the switch carries an expected version.
- UI states: banner while paused.
- Rules: automation.md §71. (enterprise baseline)
- Acceptance: Given the switch on, When facts arrive, Then no execution starts.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — (enterprise baseline).

# Integrations (INT)

## Capability A. Connector catalog

### UC-INT-A01 See what can be connected
- Capability: INT-001 Connector catalog
- Actors and permission: workspace Owner and Admin (ManageIntegrations). Members see no catalog, but keep using connected integrations on their boards (design note).
- Preconditions: workspace visible to the actor.
- Input: `workspaceId`.
- Output: the catalog (Google Calendar, Slack, outgoing webhook), whether each provider is configured on the server (client id and secret present), and the workspace's connections.
- Main flow: 1. Open Integrations. 2. Backend returns catalog and connections. 3. A provider without configuration shows "Not configured" and no Connect button.
- No-op / rejection / concurrency / retry: a caller without ManageIntegrations gets NotFound; revoked or deleted connections are not listed.
- UI states: loading, empty, error, permission denied.
- Rules: integrations.md §6, §110 INT-036; AIREQ056, AIREQ098.
- Acceptance: Given no client id for Slack, Then Slack shows "Not configured" and cannot be connected.
- Evidence: BE GET `/api/v1/workspaces/{workspaceId}/integrations/` (Integrations.Connections.List), `IntegrationCatalog` · FE FE-INT · Design `Final-Integrations` · Tests T-II `CalendarConnectionFlowIntegrationTests`, MOCK-INT · Real e2e E2E-INT
- State: HARDENING — the list is unbounded (small set); no axe or 1 px proof.

### UC-INT-A02 Show planned providers as "coming soon"
- Capability: INT-001 Connector catalog
- Actors and permission: Owner and Admin.
- Preconditions: Billing capability facts for gated providers (BIL-002).
- Input: none.
- Output: cards for Microsoft OneDrive and SharePoint, Teams and Power BI with no Connect button; gated by plan from Billing capabilities, never from the plan name.
- Main flow: catalog entries marked `comingSoon`.
- No-op / rejection / concurrency / retry: no adapter, so nothing can be connected.
- UI states: coming-soon card, plan-gated card.
- Rules: owner decision 45 (3 providers plus "coming" cards); integrations.md §88.
- Acceptance: Given the catalog, Then planned providers show as unavailable and cannot start authorization.
- Evidence: BE none · FE none · Design `Final-Integrations` · Tests none · Real e2e none
- State: DEFINED — no catalog flag, card or Billing gate exists.

### UC-INT-A03 Marketplace and plugins (enterprise baseline)
- Capability: INT-001 Connector catalog
- Actors and permission: Owner and Admin install; Members use.
- Preconditions: none; the product docs have no marketplace or plugin concept.
- Input: a listed connector from a reviewed directory.
- Output: Lead decision (2026-10-30): version one ships first-party connectors only. A directory of third-party apps is out of scope until a security review process exists. No third-party code runs inside Notrelix; any extension is an outbound webhook or an API client with a scoped token. When a directory is added, an admin allow-list decides what members may install, each app shows requested scopes, and an app can be removed at any time with its grants revoked.
- Main flow: none built.
- No-op / rejection / concurrency / retry: installing an unlisted app is refused.
- UI states: directory, scope review, installed list.
- Rules: integrations.md §107 threat model, §108 INT-035, §110 INT-036; baseline: [Slack authentication and scopes](https://api.slack.com/authentication). (enterprise baseline)
- Acceptance: Given an allow-list without app X, When a member tries to install X, Then it is refused.
- Evidence: BE none · FE none · Design `Ecosystem` (layer "Tích hợp bên ngoài") · Tests none · Real e2e none
- State: DEFINED — (enterprise baseline); needs a product decision to schedule.

## Capability B. Connection lifecycle

### UC-INT-B01 Connect an outgoing webhook
- Capability: INT-002 Connection lifecycle
- Actors and permission: Owner and Admin (ManageIntegrations on the workspace).
- Preconditions: fewer than the per-workspace webhook maximum; the address passes the SSRF policy (https, public address; in Development http and private networks are allowed).
- Input: `url`.
- Output: an Active connection with a generated signing secret returned once; 201.
- Main flow: 1. Choose Webhook. 2. Enter the address. 3. Backend validates, stores the secret behind a reference, creates the connection. 4. A one-time dialog shows the secret.
- No-op / rejection / concurrency / retry: a refused address returns a validation error on `url`; over the limit is rejected; there is no idempotency key, so a retry after a lost response creates a second connection and loses the first secret (DEF-10).
- UI states: validation error, one-time secret dialog, pending, permission denied.
- Rules: integrations.md §7, §15, §16 INT-005, §20; AIREQ055, AIREQ060.
- Acceptance: Given a public https address, Then a connection and a secret are returned once; Given a private address in production, Then refused.
- Evidence: BE POST `/api/v1/workspaces/{workspaceId}/integrations/webhooks` (Integrations.Webhooks.Connect), `ConnectWebhookCommandHandler`, `WebhookAddressPolicy` · FE FE-INT, `IntegrationSecretDialog` · Design `Final-Integrations` · Tests T-ID `WebhookConnectionStateTests`, MOCK-INT · Real e2e E2E-INT
- State: HARDENING — no idempotency key (DEF-10); no cross-tenant negative test.

### UC-INT-B02 Pause a connection
- Capability: INT-002 Connection lifecycle
- Actors and permission: Owner and Admin.
- Preconditions: connection Active.
- Input: `connectionId`.
- Output: status Paused; no deliveries are sent or fanned out.
- Main flow: POST pause.
- No-op / rejection / concurrency / retry: pausing a Paused connection is a no-op; a Revoked or Expired one is refused; wrong workspace is NotFound.
- UI states: switch, pending, error.
- Rules: integrations.md §8 INT-002, §102; AIREQ055.
- Acceptance: Given an Active webhook, When paused, Then a new Work fact creates no delivery.
- Evidence: BE POST `.../integrations/{connectionId}/pause` (Integrations.Connections.Pause) · FE FE-INT · Design `Final-Integrations` · Tests T-ID `IntegrationConnectionTests`, T-II `WebhookFanOutRuntimeChainIntegrationTests`, MOCK-INT · Real e2e E2E-INT
- State: HARDENING — no expected version; no idempotency.

### UC-INT-B03 Resume a connection
- Capability: INT-002 Connection lifecycle
- Actors and permission: Owner and Admin.
- Preconditions: connection Paused.
- Input: `connectionId`.
- Output: status Active; deliveries resume for new facts only (held work is not replayed unless defined in I03).
- Main flow: POST resume.
- No-op / rejection / concurrency / retry: resuming an Active connection is a no-op.
- UI states: switch, pending, error.
- Rules: as B02.
- Acceptance: Given a Paused webhook, When resumed, Then a later fact creates one delivery.
- Evidence: BE POST `.../integrations/{connectionId}/resume` (Integrations.Connections.Resume) · FE FE-INT · Design `Final-Integrations` · Tests T-ID `IntegrationConnectionTests`, MOCK-INT · Real e2e E2E-INT
- State: HARDENING — as B02.

### UC-INT-B04 Disconnect a connection
- Capability: INT-002 Connection lifecycle
- Actors and permission: Owner and Admin.
- Preconditions: connection exists.
- Input: `connectionId`.
- Output: status Revoked; active calendar bindings deactivated; stored secret retired after commit; business resources created through the connection are kept as plain links; nothing else is deleted.
- Main flow: 1. Choose Disconnect. 2. Confirm the impact. 3. Backend revokes and deactivates. 4. The consumer retires the secret.
- No-op / rejection / concurrency / retry: disconnecting a Revoked connection is a no-op; queued work does not run with a retired secret (L02); the provider grant is not revoked at the provider today (DEF-8).
- UI states: destructive confirmation, pending, error.
- Rules: integrations.md §49 INT-017, §97 INT-033, §101, §102, §104 INT-034; AIREQ063, AIREQ092; owner decision 45.
- Acceptance: Given an Active webhook, When disconnected, Then it no longer receives deliveries and is absent from the list.
- Evidence: BE DELETE `.../integrations/{connectionId}` (Integrations.Connections.Disconnect), `IntegrationConnectionRevokedConsumer` · FE FE-INT, `IntegrationDisconnectDialog` · Design `Final-Integrations` · Tests T-ID `IntegrationConnectionDeletionLifecycleTests`, MOCK-INT · Real e2e E2E-INT
- State: HARDENING — no provider-side revoke (DEF-8); no impact counts in the dialog (B05).

### UC-INT-B05 See the impact before disconnecting
- Capability: INT-002 Connection lifecycle
- Actors and permission: Owner and Admin.
- Preconditions: connection exists.
- Input: `connectionId`.
- Output: counts of links created, rules that use it, held operations, boards affected.
- Main flow: the disconnect dialog reads a preview.
- No-op / rejection / concurrency / retry: read only.
- UI states: loading, counts, error.
- Rules: integrations.md §97, §104; owner decision 45 (disconnect keeps created links).
- Acceptance: Given a connection used by 2 rules, Then the dialog says 2 rules will stop.
- Evidence: BE none · FE none · Design `Final-Integrations` · Tests none · Real e2e none
- State: DEFINED — planned as slice 4 of the integrations plan.

### UC-INT-B06 Connect again after a disconnect
- Capability: INT-002 Connection lifecycle
- Actors and permission: Owner and Admin.
- Preconditions: a Revoked connection of the same provider.
- Input: authorization as C01.
- Output: a new logical connection (a Revoked one is never revived); a different provider account asks for explicit confirmation.
- Main flow: as C01, C02.
- No-op / rejection / concurrency / retry: the existing-connection lookup ignores Revoked ones.
- UI states: confirm dialog when the provider account changed.
- Rules: integrations.md §99, §100; AIREQ058.
- Acceptance: Given a Revoked Google connection, When authorizing again, Then a new connection is created.
- Evidence: BE `CompleteIntegrationAuthorizationCommandHandler` (filters Revoked) · FE FE-INT · Design `Final-Integrations` · Tests none · Real e2e none
- State: IMPLEMENTING — provider account change confirmation is not built.

### UC-INT-B07 Read one connection in detail
- Capability: INT-002 Connection lifecycle
- Actors and permission: Owner and Admin.
- Preconditions: connection exists.
- Input: `connectionId`.
- Output: health, recent activity, granted scopes, what uses it.
- Main flow: a detail endpoint (not built); the web composes a reduced view from the list.
- No-op / rejection / concurrency / retry: wrong workspace is NotFound.
- UI states: loading, error, permission denied.
- Rules: integrations.md §7, §42; AIREQ098.
- Acceptance: Given a connection, Then the detail shows last success, last error and scopes.
- Evidence: BE none · FE FE-INT `integrations-view.ts` (reduced view) · Design `Final-Integrations` · Tests none · Real e2e none
- State: IMPLEMENTING — no detail endpoint.

## Capability C. Provider authorization and OAuth

### UC-INT-C01 Start provider authorization
- Capability: INT-003 Provider authorization/OAuth
- Actors and permission: Owner and Admin (ManageIntegrations).
- Preconditions: the provider is configured (client id and secret) and is OAuth.
- Input: `provider` key.
- Output: the provider's authorization URL with a protected state (10 minute lifetime); least-privilege scopes only.
- Main flow: 1. Choose Connect. 2. Backend returns the URL. 3. The browser goes to the provider.
- No-op / rejection / concurrency / retry: an unknown or unconfigured provider is NotFound; no PKCE today (DEF-9); the callback base address comes from configuration.
- UI states: pending redirect, error.
- Rules: integrations.md §13, §14 INT-004, §108 INT-035; AIREQ057, AIREQ059.
- Acceptance: Given a configured Slack, Then an authorization address with state is returned; a Member gets NotFound.
- Evidence: BE GET `.../integrations/{provider}/authorize` (Integrations.Connections.Authorize), `IntegrationOAuthStateProtector` · FE FE-INT · Design `Final-Integrations` · Tests T-II `CalendarConnectionFlowIntegrationTests` · Real e2e E2E-INT (only "Not configured" is proven)
- State: INTEGRATING — no PKCE (DEF-9); no real provider round trip.

### UC-INT-C02 Complete authorization on the callback
- Capability: INT-003 Provider authorization/OAuth
- Actors and permission: the signed-in member returning from the provider; the command needs ManageIntegrations on the workspace named in the state.
- Preconditions: valid unexpired state.
- Input: `code`, `state`, optional `error`.
- Output: tokens exchanged and stored behind a reference; an Active connection (or an Expired or Error one reauthorized); redirect to the Integrations page with `connected=<provider>`.
- Main flow: 1. Provider redirects to the callback. 2. The state is validated. 3. The code is exchanged. 4. The connection and secret version are written.
- No-op / rejection / concurrency / retry: an invalid state redirects home; a refused or empty code redirects with `error=refused`; failure redirects with `error=failed`; a replayed code fails at the provider.
- UI states: success toast, error toast.
- Rules: integrations.md §9, §10, §14 INT-004, §15 INT-005; AIREQ058, AIREQ060.
- Acceptance: Given a valid code, Then one Active connection exists and no token is in any response.
- Evidence: BE GET `/api/v1/integrations/{provider}/callback` (Integrations.Connections.Callback), `CompleteIntegrationAuthorizationCommandHandler` · FE FE-INT (reads `connected` and `error`) · Design `Final-Integrations` · Tests T-II `CalendarConnectionFlowIntegrationTests` · Real e2e none
- State: INTEGRATING — no real e2e; no provider simulator.

### UC-INT-C03 A refused or failed authorization is explained
- Capability: INT-003 Provider authorization/OAuth
- Actors and permission: as C02.
- Preconditions: the member denied consent or the exchange failed.
- Input: callback with `error` or a failed exchange.
- Output: the member returns to Integrations with a clear message; no connection is created.
- Main flow: redirect with `error=refused` or `error=failed`.
- No-op / rejection / concurrency / retry: the member may try again.
- UI states: error toast with retry.
- Rules: integrations.md §14 INT-004.
- Acceptance: Given a denied consent, Then no connection exists and the message says it was refused.
- Evidence: BE callback endpoint · FE FE-INT (toast) · Design `Final-Integrations` · Tests none · Real e2e none
- State: INTEGRATING — no test of the error redirects.

### UC-INT-C04 Reauthorize an expired connection
- Capability: INT-003 Provider authorization/OAuth
- Actors and permission: Owner and Admin; the connector owner is told.
- Preconditions: connection Expired or Error.
- Input: authorization as C01.
- Output: the same logical connection returns to Active with a new secret version; configuration, history and mappings stay; held operations resume (I03).
- Main flow: banner "Uỷ quyền lại với Microsoft"; same provider account expected.
- No-op / rejection / concurrency / retry: a different provider account asks for confirmation (B06).
- UI states: reauthorize banner, held-work list.
- Rules: integrations.md §18, §19 INT-006, §99; AIREQ062; owner decision 45 (`reauthorization-required` status).
- Acceptance: Given an Expired connection, When reauthorized, Then it is Active again with the same id.
- Evidence: BE `CompleteIntegrationAuthorizationCommandHandler` (Expired and Error path) · FE none (status shows "error") · Design `Final-Integrations` · Tests T-ID `ReconnectNoOpTests` · Real e2e none
- State: IMPLEMENTING — no `reauthorization-required` status and no UI.

### UC-INT-C05 Ask for more provider permission
- Capability: INT-003 Provider authorization/OAuth
- Actors and permission: Owner and Admin.
- Preconditions: a new feature needs a scope the connection lacks.
- Input: requested scope.
- Output: the member sees what is added and consents again; nothing fails silently.
- Main flow: scope upgrade starts C01 with the extra scope.
- No-op / rejection / concurrency / retry: until consent, the feature is disabled with a message.
- UI states: consent prompt, "Không làm được" list.
- Rules: integrations.md §108 INT-035, §109; AIREQ059.
- Acceptance: Given a feature needing a new scope, Then the member is asked to approve it before it works.
- Evidence: BE domain `IntegrationScopeAddedDomainEvent` only · FE none · Design `Final-Integrations` · Tests T-ID `IntegrationScopeCanonicalizationTests` · Real e2e none
- State: DEFINED — domain facts only.

## Capability D. Connection configuration

### UC-INT-D01 Change the address of an outgoing webhook
- Capability: INT-004 Connection configuration
- Actors and permission: Owner and Admin.
- Preconditions: connection Active or Paused.
- Input: new `url`, expected version.
- Output: the address changes after the same SSRF check; the secret is unchanged.
- Main flow: edit on the connection detail.
- No-op / rejection / concurrency / retry: the same address is a no-op; a stale version is rejected.
- UI states: validation error, pending.
- Rules: integrations.md §112, §113 INT-037; AIREQ056.
- Acceptance: Given a new public address, Then later deliveries go to it.
- Evidence: BE none (disconnect and reconnect is the only way) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — no endpoint or UI exists.

### UC-INT-D02 Typed, versioned provider configuration
- Capability: INT-004 Connection configuration
- Actors and permission: system.
- Preconditions: none.
- Input: provider configuration.
- Output: each provider has a discriminator, schema version and validation; secrets are references; no schema-less JSON.
- Main flow: configuration classes per provider.
- No-op / rejection / concurrency / retry: an unknown discriminator or version fails closed.
- UI states: none.
- Rules: integrations.md §112, §113 INT-037; AIREQ056, AIREQ107.
- Acceptance: Given a stored configuration with an unknown version, Then it is not used.
- Evidence: BE `IntegrationConnection`, `IntegrationsOptions` · FE none · Design none · Tests T-ID `IntegrationConnectionContractTests` · Real e2e none
- State: IMPLEMENTING — only the webhook target address is a typed field; no versioned configuration object.

## Capability E. Credential reference and storage

### UC-INT-E01 A signing secret is generated and shown once
- Capability: INT-005 Credential reference/storage contract
- Actors and permission: Owner and Admin.
- Preconditions: B01.
- Input: none.
- Output: a random secret; only a reference is stored; the secret is never returned again.
- Main flow: see B01.
- No-op / rejection / concurrency / retry: lost response means regenerate (E02), not read.
- UI states: one-time dialog with copy.
- Rules: integrations.md §15, §16 INT-005; AIREQ060, AIREQ061, AIREQ113.
- Acceptance: Given a created webhook, When the list is read, Then no secret field appears.
- Evidence: BE `WebhookSignature.GenerateSecret`, `DataProtectionIntegrationSecretStore`, `IIntegrationSecretStore` · FE `IntegrationSecretDialog` · Design `Final-Integrations` · Tests T-ID `IntegrationSecretStateTests`, `WebhookSecretHashTests`, MOCK-INT · Real e2e E2E-INT
- State: HARDENING — the physical store depends on Data Protection key persistence (restart safety, AIREQ061) not proven in a restart test.

### UC-INT-E02 Regenerate a webhook secret
- Capability: INT-005 Credential reference/storage contract
- Actors and permission: Owner and Admin.
- Preconditions: connection Active or Paused.
- Input: `connectionId`, expected version.
- Output: a new secret version shown once; the old one is invalid at once.
- Main flow: "Regenerate secret" on the detail.
- No-op / rejection / concurrency / retry: a retry with the same idempotency key returns the same new secret reference, never two.
- UI states: confirm dialog, one-time dialog.
- Rules: integrations.md §17; owner decision 45 (regeneration allowed); AIREQ062.
- Acceptance: Given a regenerated secret, Then deliveries signed with the old secret are no longer produced.
- Evidence: BE `IntegrationConnection.RotateSecret`, `IntegrationSecretRotatedDomainEvent` (no command, no endpoint) · FE none · Design `Final-Integrations` · Tests T-ID `SecretRotationBoundaryTests` · Real e2e none
- State: IMPLEMENTING — domain only.

### UC-INT-E03 Tokens are stored behind an opaque reference
- Capability: INT-005 Credential reference/storage contract
- Actors and permission: system.
- Preconditions: OAuth completed.
- Input: access and refresh tokens.
- Output: tokens are encrypted in the secret store; the connection holds a version and a reference; no response, log or event carries a token.
- Main flow: `IntegrationSecretVersion` plus `IntegrationSecretBlob`.
- No-op / rejection / concurrency / retry: a failed commit leaves no orphan the app reads.
- UI states: none.
- Rules: integrations.md §15, §16 INT-005; AIREQ060 to AIREQ062, AIREQ105.
- Acceptance: Given a connected Google account, Then no API response contains a token.
- Evidence: BE `IntegrationSecretBlobConfiguration`, `IntegrationSecretVersionConfiguration`, `SecretEncryptor` · FE none · Design none · Tests T-ID `IntegrationSecretStateTests` · Real e2e none
- State: INTEGRATING — no integration test of the OAuth token path.

### UC-INT-E04 Refresh tokens before they expire
- Capability: INT-005 Credential reference/storage contract
- Actors and permission: system.
- Preconditions: a connection with a refresh token.
- Input: expiry time.
- Output: a new access token stored as a new secret version; if refresh fails, the connection becomes Expired and C04 is offered.
- Main flow: a refresh job (not found).
- No-op / rejection / concurrency / retry: concurrent refreshes produce one new version.
- UI states: reauthorization banner on failure.
- Rules: integrations.md §18 token expiry, §19 INT-006.
- Acceptance: Given a token 5 minutes from expiry, Then a new version exists and the connection stays Active.
- Evidence: BE `IntegrationOAuthClient` (token exchange and refresh call), no scheduled refresh · FE none · Design none · Tests none · Real e2e none
- State: IMPLEMENTING — no refresh job.

### UC-INT-E05 Retire the secret after a disconnect
- Capability: INT-005 Credential reference/storage contract
- Actors and permission: system.
- Preconditions: a connection was Revoked.
- Input: the revocation fact.
- Output: the stored secret is deleted after commit with broker retry and dead letter; the connection state does not depend on the cleanup.
- Main flow: `IntegrationConnectionRevokedConsumer`.
- No-op / rejection / concurrency / retry: a duplicate fact is a no-op.
- UI states: none.
- Rules: integrations.md §97 INT-033; AIREQ063.
- Acceptance: Given a disconnected connection, Then its secret blob is gone after the consumer runs.
- Evidence: BE `IntegrationConnectionRevokedConsumer` · FE none · Design none · Tests T-II `CalendarWebhookDataSessionIntegrationTests` · Real e2e none
- State: INTEGRATING — no direct test of the cleanup consumer.

## Capability F. Outbound provider operation

### UC-INT-F01 Deliver a signed webhook for a work fact
- Capability: INT-006 Outbound provider operation
- Actors and permission: system on Work and Collaboration facts.
- Preconditions: an Active outgoing webhook connection in the fact's workspace.
- Input: a completed fact and its identity.
- Output: one signed `POST` per Active connection with delivery id, event type, timestamp and HMAC signature headers; delivery id derived from the source event id and the connection id.
- Main flow: 1. A fact arrives. 2. `WebhookEventFanOut` stages one intent per connection. 3. The dispatch consumer sends (redirects off, address re-checked at connect time).
- No-op / rejection / concurrency / retry: a redelivered fact asks for the same deliveries again and the receiver sees a repeat; Paused, Revoked or deleted connections are skipped.
- UI states: none (delivery log is F05).
- Rules: integrations.md §52, §53 INT-019, §93 INT-031; AIREQ065, AIREQ067, AIREQ074.
- Acceptance: Given two Active webhooks, When one item is created, Then each receives one delivery with its own delivery id.
- Evidence: BE `WebhookEventFanOut`, `IntegrationWebhookDispatchRequestedConsumer`, `OutboundWebhookSender` · FE none · Design none · Tests T-II `WebhookFanOutRuntimeChainIntegrationTests` · Real e2e none
- State: INTEGRATING — no real e2e of a fact-driven delivery.

### UC-INT-F02 Send a test delivery
- Capability: INT-006 Outbound provider operation
- Actors and permission: Owner and Admin.
- Preconditions: Active webhook connection.
- Input: `connectionId`.
- Output: a queued test delivery with a delivery id; the receiver gets a signed request.
- Main flow: POST test; the intent goes through the outbox.
- No-op / rejection / concurrency / retry: each press makes a new delivery id; a Paused connection is refused.
- UI states: "test queued" toast.
- Rules: integrations.md §52, §53.
- Acceptance: Given a receiver, When Test is pressed, Then the receiver sees a request whose signature verifies.
- Evidence: BE POST `.../integrations/{connectionId}/test` (Integrations.Connections.Test) · FE FE-INT · Design `Final-Integrations` · Tests T-II `WebhookFanOutRuntimeChainIntegrationTests` · Real e2e E2E-INT
- State: HARDENING — the result of the test is not shown to the user (only "queued").

### UC-INT-F03 Send a Slack message (outbound operation)
- Capability: INT-006 Outbound provider operation
- Actors and permission: system on behalf of an Automation rule whose creator may use the connection.
- Preconditions: an Active Slack connection; channel chosen.
- Input: channel, text, operation id from Automation.
- Output: Lead decision (2026-10-30): minimum scope `chat:write` only; the adapter posts one message with an idempotency key derived from the operation id; Slack errors map to the result taxonomy (J01).
- Main flow: Automation calls the Integrations operation contract; the adapter calls Slack outside the database transaction.
- No-op / rejection / concurrency / retry: unknown outcome is reconciled, not blindly retried (F07).
- UI states: held-for-reauthorization step.
- Rules: integrations.md §52, §53 INT-019, §54, §55, §56 INT-020, §108 INT-035; AIREQ065 to AIREQ070; baseline: [Slack authentication](https://api.slack.com/authentication).
- Acceptance: Given an Automation Slack step, When run, Then one message is posted and a retry posts none.
- Evidence: BE none (Slack OAuth connection only) · FE none · Design `Final-Integrations` (Teams example) · Tests none · Real e2e none
- State: DEFINED — unblocks UC-AUT-D09.

### UC-INT-F04 Dispatch an n8n webhook for Automation
- Capability: INT-006 Outbound provider operation
- Actors and permission: system on behalf of an Automation Webhook step.
- Preconditions: n8n configured; webhook path valid.
- Input: execution id, path, payload.
- Output: a signed call to n8n with a stable operation identity and a classified result.
- Main flow: `N8nClient` posts; `N8nWebhookOutcome` is Success, RetryableFailure, TerminalFailure or UnknownOutcome.
- No-op / rejection / concurrency / retry: Automation owns the retry budget; Integrations keeps the call idempotent (INT-027).
- UI states: none.
- Rules: integrations.md §77.1, §78 INT-027; AIREQ071.
- Acceptance: Given a retry, Then n8n sees the same operation identity.
- Evidence: BE `N8nClient`, `N8nWebhookActions` · FE none · Design none · Tests `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs`, `backend/tests/Notrelix.Integration.Tests/Extensibility/N8nSignatureServiceTests.cs` · Real e2e none
- State: IMPLEMENTING — no real e2e; see UC-AUT-D08.

### UC-INT-F05 Read the delivery log of a webhook
- Capability: INT-006 Outbound provider operation
- Actors and permission: Owner and Admin.
- Preconditions: deliveries exist (`WebhookDelivery` rows).
- Input: `connectionId`, cursor.
- Output: deliveries with id, event type, status (Pending, Sent, Failed, Retrying), attempts, last status code, time; no payload secrets.
- Main flow: a detail tab (planned slice 3).
- No-op / rejection / concurrency / retry: cursor paging.
- UI states: loading, empty, error.
- Rules: integrations.md §27 Webhook Delivery, §28 INT-010; AIREQ083, AIREQ114.
- Acceptance: Given a failed delivery, Then the log shows its attempts and last status.
- Evidence: BE `WebhookDelivery` entity and table only · FE none · Design `Final-Integrations` · Tests T-ID `WebhookDeliveryLifecycleTests` · Real e2e none
- State: IMPLEMENTING — no read endpoint.

### UC-INT-F06 Retry a failed delivery with backoff
- Capability: INT-006 Outbound provider operation
- Actors and permission: system.
- Preconditions: a delivery failed with a retryable result (5xx, 408, 429, timeout, refusal).
- Input: attempt number.
- Output: owner decision 45: up to 8 attempts per event with backoff, then Failed and visible. Today 3 redeliveries at 1, 5 and 30 seconds.
- Main flow: broker retry with intervals.
- No-op / rejection / concurrency / retry: the delivery id header is the receiver's dedupe key; a hot loop is forbidden.
- UI states: attempts in the log (F05).
- Rules: integrations.md §57, §58, §59 INT-021; AIREQ068, AIREQ069; owner decision 45.
- Acceptance: Given a receiver that fails 7 times then succeeds, Then it is delivered once on attempt 8.
- Evidence: BE `IntegrationWebhookDispatchRequestedConsumerDefinition` (1 s, 5 s, 30 s) · FE none · Design none · Tests T-II `WebhookFanOutRuntimeChainIntegrationTests` · Real e2e none
- State: IMPLEMENTING — the budget differs from the decision (DEF-12).

### UC-INT-F07 Reconcile an unknown provider outcome
- Capability: INT-006 Outbound provider operation
- Actors and permission: system; operators under governed recovery.
- Preconditions: a send timed out after the request may have been processed.
- Input: operation identity.
- Output: the operation is marked unknown and reconciled by a provider lookup or the idempotency key; it is never re-sent blindly.
- Main flow: reconciliation job.
- No-op / rejection / concurrency / retry: if the provider offers no lookup and no idempotency, the step fails as unknown and a person decides.
- UI states: "provider outcome unknown" notice.
- Rules: integrations.md §55, §56 INT-020; AIREQ070.
- Acceptance: Given a timeout after send, Then no second send happens before reconciliation.
- Evidence: BE `N8nWebhookOutcome.UnknownOutcome` for n8n only · FE none · Design none · Tests `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs` · Real e2e none
- State: DEFINED — no executable path outside n8n.

## Capability G. Inbound webhook verification

### UC-INT-G01 Accept only bounded, verified calendar webhooks
- Capability: INT-007 Inbound webhook verification
- Actors and permission: the provider (anonymous route, signature-authenticated, rate limited per IP).
- Preconditions: an active binding for the webhook path.
- Input: `POST /api/v1/integrations/calendar/webhooks/{provider}/{webhookPath}` with `X-Calendar-Signature`, `X-Calendar-Timestamp`, JSON body.
- Output: 200 when verified and claimed; 401 otherwise; stable problem codes at the boundary.
- Main flow: 1. Content type must be `application/json` (else 415). 2. Body read as raw bytes within the size and time limit (413, 408). 3. Empty (400), non-UTF-8 or non-JSON (400) rejected. 4. Signature verified over the exact bytes. 5. The receipt is claimed with the intent in one outbox transaction.
- No-op / rejection / concurrency / retry: an unknown, deactivated or deleted path is rejected before any payload trust; a route provider that differs from the binding fails closed.
- UI states: none.
- Rules: integrations.md §20, §21 INT-007, §22, §23, §24 INT-008; AIREQ075 to AIREQ079, AIREQ084.
- Acceptance: Given a body with a wrong signature, Then 401 and a rejected receipt is recorded; given a 2 MB body, Then 413.
- Evidence: BE POST `/api/v1/integrations/calendar/webhooks/{provider}/{webhookPath}` (Integrations.Calendar.HandleWebhook), `HandleCalendarWebhookCommandHandler`, `CalendarWebhookVerifier` · FE none · Design none · Tests T-II `CalendarWebhookIntakeIntegrationTests`, `CalendarWebhookDataSessionIntegrationTests` · Real e2e none
- State: INTEGRATING — no provider simulator e2e; the rate limit has no negative test found.

### UC-INT-G02 Reject replayed and duplicate deliveries
- Capability: INT-007 Inbound webhook verification
- Actors and permission: system.
- Preconditions: a verified delivery.
- Input: provider event id scoped by connection and provider.
- Output: the first claim creates a Captured receipt and one processing intent; a duplicate is an idempotent no-op that enqueues nothing; a stale timestamp is rejected.
- Main flow: `CalendarWebhookIntake.AcceptAsync`.
- No-op / rejection / concurrency / retry: dedupe key is (connection, provider, external event id), not the event type.
- UI states: none.
- Rules: integrations.md §25, §26 INT-009, §27, §28 INT-010; AIREQ078, AIREQ079.
- Acceptance: Given the same event posted twice, Then one processing intent exists.
- Evidence: BE `CalendarWebhookIntake`, `InboundWebhookReceipt` · FE none · Design none · Tests T-II `CalendarWebhookIntakeIntegrationTests`, `backend/tests/Notrelix.Infrastructure.Tests/Integrations/InboundWebhookReceiptLifecycleTests.cs` · Real e2e none
- State: INTEGRATING — as G01.

### UC-INT-G03 The tenant comes from the binding, never the payload
- Capability: INT-007 Inbound webhook verification
- Actors and permission: system.
- Preconditions: a signature-verified delivery.
- Input: the webhook path.
- Output: the owning account and workspace are adopted from the binding only after verification; the receipt and the processing run under that tenant on the RLS session.
- Main flow: `CalendarWebhookBindingResolver`.
- No-op / rejection / concurrency / retry: a payload that names another workspace is ignored.
- UI states: none.
- Rules: integrations.md §23, §24 INT-008; AIREQ077, AIREQ104.
- Acceptance: Given a payload naming workspace W2 on a W1 path, Then only W1 data is touched.
- Evidence: BE `CalendarWebhookBindingResolver`, `TenantContextConsumeFilter` · FE none · Design none · Tests T-II `CalendarWebhookDataSessionIntegrationTests` · Real e2e none
- State: INTEGRATING — as G01.

### UC-INT-G04 Malformed or conflicting payloads fail closed
- Capability: INT-007 Inbound webhook verification
- Actors and permission: system.
- Preconditions: a verified delivery with an unsupported or conflicting payload.
- Input: payload.
- Output: the receipt is marked failed or poison with a safe reason; no business state changes; nothing loops forever.
- Main flow: the processing consumer validates the schema.
- No-op / rejection / concurrency / retry: a receipt becomes Processed only after semantic reconciliation.
- UI states: none.
- Rules: integrations.md §29, §30, §75 INT-026; AIREQ081, AIREQ082.
- Acceptance: Given a payload missing the event id, Then rejected at the boundary; given an unsupported type, Then marked and skipped.
- Evidence: BE `CalendarWebhookProcessingRequestedConsumer`, `CalendarWebhookProcessingUseCase` · FE none · Design none · Tests T-II `CalendarWebhookProcessingRuntimeTests` · Real e2e none
- State: INTEGRATING — as G01.

## Capability H. Inbound event mapping, with calendar sync

### UC-INT-H01 Bind a calendar to a connection
- Capability: INT-008 Inbound event mapping
- Actors and permission: Owner and Admin (ManageIntegrations).
- Preconditions: an Active Google or Microsoft connection.
- Input: `provider`, `providerAccountId`, `syncDirection` (Push, Pull, Both), `workspaceId`; today also a raw `accessToken` (DEF-11).
- Output: a `CalendarIntegration` binding with a unique webhook path; 201 with the binding id.
- Main flow: POST connect calendar; the secret goes behind a reference.
- No-op / rejection / concurrency / retry: a second active binding for the same account is refused; no idempotency key (DEF-11).
- UI states: provider account picker, direction choice.
- Rules: integrations.md §65, §66 INT-023, §68 sync direction, §10; AIREQ056, AIREQ089.
- Acceptance: Given an Active Google connection, When bound with direction Pull, Then a binding and a webhook path exist.
- Evidence: BE POST `/api/v1/workspaces/{workspaceId}/calendar/connections` (Integrations.Calendar.Connect), `ConnectCalendarCommandHandler` · FE none · Design `Final-Calendar` · Tests T-ID `CalendarIntegrationTests`, T-II `CalendarConnectionFlowIntegrationTests` · Real e2e none
- State: IMPLEMENTING — no UI; the token travels in the request body and is not linked to the OAuth connection.

### UC-INT-H02 Remove a calendar binding
- Capability: INT-008 Inbound event mapping
- Actors and permission: Owner and Admin (ManageIntegrations on the calendar integration).
- Preconditions: binding exists.
- Input: `integrationId`.
- Output: the binding is deactivated; later webhooks to its path are rejected; mappings are kept for history.
- Main flow: DELETE.
- No-op / rejection / concurrency / retry: an unknown id is NotFound.
- UI states: confirmation.
- Rules: integrations.md §48, §101, §103; AIREQ092.
- Acceptance: Given a binding, When removed, Then its webhook path answers 401 and no resource is deleted.
- Evidence: BE DELETE `/api/v1/calendar-integrations/{integrationId}/` (Integrations.Calendar.Disconnect), `DisconnectCalendarCommandHandler` · FE none · Design none · Tests T-ID `CalendarIntegrationTests` · Real e2e none
- State: IMPLEMENTING — no UI; not idempotent by key.

### UC-INT-H03 Turn a verified calendar change into an internal fact
- Capability: INT-008 Inbound event mapping
- Actors and permission: system.
- Preconditions: a Captured receipt.
- Input: receipt id.
- Output: the provider event is mapped to a stable product-neutral fact; Integrations never writes Work Management persistence directly (INT-028); unsupported events are marked and skipped.
- Main flow: the consumer decrypts the receipt in tenant scope, reconciles, marks Processed.
- No-op / rejection / concurrency / retry: duplicates are no-ops.
- UI states: none.
- Rules: integrations.md §79, §80 INT-028, §93 INT-031; AIREQ080, AIREQ081, AIREQ089.
- Acceptance: Given a changed provider event, Then one internal fact is published once.
- Evidence: BE `CalendarWebhookProcessingUseCase`, `CalendarEvent`, `CalendarSyncFingerprint` · FE none · Design none · Tests T-II `CalendarWebhookProcessingRuntimeTests` · Real e2e none
- State: IMPLEMENTING — the consumer reconciles into Integrations state; the Work Management consumer of the fact is not found.

### UC-INT-H04 Sync a calendar now
- Capability: INT-008 Inbound event mapping
- Actors and permission: Owner and Admin (ManageIntegrations; today the marker says ManageWorkspaceSettings, DEF-7).
- Preconditions: an active binding.
- Input: `integrationId`.
- Output: a sync job runs; status shows syncing then idle or failed.
- Main flow: POST sync (not built); the handler throws `NotImplementedException`.
- No-op / rejection / concurrency / retry: a second press while syncing is a no-op; the same idempotency key returns the same job.
- UI states: sync button, syncing, failed, rate-limited.
- Rules: integrations.md §31 to §43, §41; AIREQ090.
- Acceptance: Given a binding, When Sync now is pressed, Then a job starts and ends with a status.
- Evidence: BE `TriggerCalendarSyncCommandHandler` throws; `CalendarSyncJob` unused · FE none · Design `Final-Calendar` · Tests none · Real e2e none
- State: IMPLEMENTING — handler is a stub; no endpoint.

### UC-INT-H05 Backfill and cursor
- Capability: INT-008 Inbound event mapping
- Actors and permission: system.
- Preconditions: a new binding or a resync request.
- Input: provider changes from a cursor.
- Output: the cursor advances only after durable processing; backfill and live changes converge without duplicates; the cursor is progress, not truth.
- Main flow: `IntegrationSyncCursor` moves per batch.
- No-op / rejection / concurrency / retry: an interrupted batch repeats from the last cursor.
- UI states: sync health (I02).
- Rules: integrations.md §36 to §41 INT-013, INT-014; AIREQ086, AIREQ091.
- Acceptance: Given a crash mid-batch, Then no event is lost or duplicated.
- Evidence: BE `IntegrationSyncCursor`, `SyncStatus` domain · FE none · Design none · Tests T-ID `IntegrationSyncCursorTests`, `SyncTransitionMatrixTests` · Real e2e none
- State: IMPLEMENTING — domain only; no runner.

### UC-INT-H06 Detect and resolve a sync conflict
- Capability: INT-008 Inbound event mapping
- Actors and permission: Owner and Admin see conflicts; the system applies the policy.
- Preconditions: both sides changed one event.
- Input: provider revision and fingerprint.
- Output: Lead decision (2026-10-30): Notrelix is the source of truth for dates and titles of items it owns; the provider copy is overwritten on the next push; a conflict is recorded and shown, never silently dropped. Last-write-wins is not used.
- Main flow: fingerprint compare; `CalendarConflictDetectedDomainEvent`.
- No-op / rejection / concurrency / retry: identical content is not a conflict.
- UI states: conflict banner on the item.
- Rules: integrations.md §60 to §64 INT-022, §70; AIREQ087.
- Acceptance: Given both sides changed, Then a conflict is recorded and the Notrelix value stays.
- Evidence: BE `CalendarSyncRules`, `CalendarConflictDetectedDomainEvent` · FE none · Design none · Tests T-ID `CalendarSyncRulesTests` · Real e2e none
- State: IMPLEMENTING — domain only.

### UC-INT-H07 Keep both identities of a mapped event
- Capability: INT-008 Inbound event mapping
- Actors and permission: system.
- Preconditions: an item is linked to a provider event.
- Input: link.
- Output: a unique link per connection holds the Notrelix item id and the provider event id; a provider deletion does not delete the item (INT-018); disconnect keeps links as history.
- Main flow: `CalendarEventLink`.
- No-op / rejection / concurrency / retry: the same event cannot be linked twice on one connection.
- UI states: "linked" chip.
- Rules: integrations.md §44 to §48 INT-016, §50, §51 INT-018, §67; AIREQ088, AIREQ092.
- Acceptance: Given a provider deletion, Then the item remains and the link is marked.
- Evidence: BE `CalendarEventLinkConfiguration`, `CalendarEvent` · FE none · Design none · Tests T-ID `CalendarEventTests` · Real e2e none
- State: IMPLEMENTING — no flow uses it end to end.

### UC-INT-H08 Provider content is untrusted
- Capability: INT-008 Inbound event mapping
- Actors and permission: system.
- Preconditions: provider titles, descriptions, HTML.
- Input: provider payload.
- Output: Lead decision (2026-10-30): text is length-limited and stored as plain text; HTML is stripped; attendee emails never create workspace membership (INT-025).
- Main flow: sanitize at the anti-corruption layer.
- No-op / rejection / concurrency / retry: invalid content is dropped with a reason, the event still maps.
- UI states: none.
- Rules: integrations.md §72, §73 INT-025, §74, §75 INT-026, §76; AIREQ082.
- Acceptance: Given a title with a script tag, Then it is stored as text.
- Evidence: BE none confirmed · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — no sanitizer exists at the provider boundary.

## Capability I. Connection health

### UC-INT-I01 See status, last success and last error
- Capability: INT-009 Connection health
- Actors and permission: Owner and Admin.
- Preconditions: connection exists.
- Input: none.
- Output: status active, paused or error with last success time, last error time and a short error text; target address for webhooks.
- Main flow: shown on each card.
- No-op / rejection / concurrency / retry: error text is redacted.
- UI states: ok, off, warn tones.
- Rules: integrations.md §19 INT-006, §42, §43 INT-015; AIREQ064.
- Acceptance: Given a failing receiver, Then the card shows the last error.
- Evidence: BE `IntegrationConnectionMapper` · FE FE-INT `integrations-view.ts` · Design `Final-Integrations` · Tests MOCK-INT · Real e2e E2E-INT
- State: HARDENING — Expired and Revoked collapse into "error" in the DTO.

### UC-INT-I02 Sync health is separate from connection status
- Capability: INT-009 Connection health
- Actors and permission: Owner and Admin.
- Preconditions: a calendar binding.
- Input: none.
- Output: sync health: idle, syncing, degraded, rate-limited, failed, reauthorization-required, resync-required, shown apart from the connection status.
- Main flow: read from the cursor status.
- No-op / rejection / concurrency / retry: none.
- UI states: health chip.
- Rules: integrations.md §42, §43 INT-015.
- Acceptance: Given a valid token and a failing sync, Then status is Active and health is failed.
- Evidence: BE `SyncStatus` (Idle, InProgress, Completed, Failed, Partial) domain · FE none · Design `Final-Integrations` · Tests T-ID `SyncTransitionMatrixTests` · Real e2e none
- State: IMPLEMENTING — no DTO field.

### UC-INT-I03 Hold work while reauthorization is needed
- Capability: INT-009 Connection health
- Actors and permission: Owner and Admin.
- Preconditions: connection Expired.
- Input: outbound operations.
- Output: new operations wait (not lost, not run with the dead secret); after reauthorization the owner can rerun them (see UC-AUT-I03).
- Main flow: held list on the card.
- No-op / rejection / concurrency / retry: held work is never executed with a revoked secret.
- UI states: "1 thao tác đang chờ" list.
- Rules: integrations.md §102; owner decision 45 (held messages retained).
- Acceptance: Given an Expired Teams connection, Then a new step waits and is listed.
- Evidence: BE none · FE none · Design `Final-Integrations` · Tests none · Real e2e none
- State: DEFINED — no held-operation state exists.

### UC-INT-I04 Tell admins when a connection breaks (enterprise baseline)
- Capability: INT-009 Connection health
- Actors and permission: Owner, Admin and the connector owner receive.
- Preconditions: a connection becomes Expired or Error.
- Input: the status fact.
- Output: one notification per outage (not per failed retry) with a link to reauthorize.
- Main flow: Collaboration notification from the Integrations fact.
- No-op / rejection / concurrency / retry: duplicates are collapsed.
- UI states: inbox item.
- Rules: integrations.md §90; AIREQ114. (enterprise baseline)
- Acceptance: Given a token that expired, Then the connector owner gets one inbox item.
- Evidence: BE domain `IntegrationConnectionExpiredDomainEvent` only · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — (enterprise baseline).

## Capability J. Provider failure normalization

### UC-INT-J01 Provider results map to one taxonomy
- Capability: INT-010 Provider failure normalization
- Actors and permission: system.
- Preconditions: a provider call finished.
- Input: HTTP status, timeout, provider error code.
- Output: Success, Retryable, Terminal, Rate limited, Authorization failed or Unknown outcome; no raw provider text leaves Integrations.
- Main flow: the adapter translates (anti-corruption layer).
- No-op / rejection / concurrency / retry: 5xx, 408, 429, timeout and refusal are retryable; other 4xx are terminal.
- UI states: reason in connection health.
- Rules: integrations.md §5 INT-001, §54, §55, §111 INT-036; AIREQ066, AIREQ113.
- Acceptance: Given a 404 from a receiver, Then Terminal; given a 503, Then Retryable.
- Evidence: BE `OutboundWebhookSender` result enum, `N8nClient` · FE none · Design none · Tests T-AA `N8nDispatchUseCaseTests`, `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs` · Real e2e none
- State: INTEGRATING — the taxonomy is per adapter; no shared contract type; OAuth and calendar adapters are not covered.

## Capability K. Integration management frontend

### UC-INT-K01 The Integrations page
- Capability: INT-011 Integration management frontend
- Actors and permission: Owner and Admin; others see permission denied.
- Preconditions: workspace route.
- Input: workspace id.
- Output: catalog list, connection cards with status, actions; the `connected` and `error` query results as toasts.
- Main flow: as A01, B01 to B04, F02.
- No-op / rejection / concurrency / retry: a workspace switch clears state; a forbidden list shows permission denied.
- UI states: loading, empty, error, permission denied, pending.
- Rules: AIREQ098, AIREQ099; DESIGN.md (Integrations); team spec section 38.
- Acceptance: Given an Admin, Then the page lists and manages connections; given a Member, Then permission denied.
- Evidence: BE routes of INT-A to INT-F · FE FE-INT `integrations.tsx`, `use-integrations.ts`, `integrations-api.ts` · Design `Final-Integrations` · Tests MOCK-INT · Real e2e E2E-INT
- State: HARDENING — the permission-denied state has no real e2e; no axe; no 1 px diff.

### UC-INT-K02 Confirm a disconnect
- Capability: INT-011 Integration management frontend
- Actors and permission: Owner and Admin.
- Preconditions: a connection card.
- Input: confirm.
- Output: the dialog states what stops and what stays; the connection goes.
- Main flow: dialog then DELETE.
- No-op / rejection / concurrency / retry: cancel changes nothing.
- UI states: destructive confirmation, pending, error.
- Rules: integrations.md §97 INT-033; DESIGN.md destructive actions.
- Acceptance: Given the dialog, When confirmed, Then the card disappears.
- Evidence: BE as B04 · FE `IntegrationDisconnectDialog` in FE-INT · Design `Final-Integrations` · Tests MOCK-INT · Real e2e E2E-INT
- State: HARDENING — no impact counts (B05).

### UC-INT-K03 Connection detail page
- Capability: INT-011 Integration management frontend
- Actors and permission: Owner and Admin.
- Preconditions: a connection.
- Input: connection id.
- Output: tabs for status, activity, permissions granted, "cannot do" list, used by.
- Main flow: as B07.
- No-op / rejection / concurrency / retry: none.
- UI states: loading, error, permission denied.
- Rules: AIREQ098.
- Acceptance: Given a connection, Then the detail matches the artboard.
- Evidence: BE none · FE FE-INT `integrations-view.ts` · Design `Final-Integrations` · Tests none · Real e2e none
- State: IMPLEMENTING — reduced view only.

### UC-INT-K04 Live connection and sync progress
- Capability: INT-011 Integration management frontend
- Actors and permission: Owner and Admin.
- Preconditions: page open.
- Input: connection and sync events.
- Output: the card updates; after a gap the page refetches durable state.
- Main flow: realtime invalidation of the connection queries.
- No-op / rejection / concurrency / retry: a stale event never overwrites the new workspace.
- UI states: live badge.
- Rules: integrations.md §95, §96 INT-032; AIREQ101.
- Acceptance: Given a status change, Then the card updates without reload.
- Evidence: BE none · FE none (`frontend/packages/features/integrations/**` has no realtime) · Design none · Tests none · Real e2e none
- State: DEFINED — no realtime producer or consumer exists.

## Capability L. Rate, retry and provider limits

### UC-INT-L01 Back off on a provider rate limit
- Capability: INT-012 Rate/retry/provider limits
- Actors and permission: system.
- Preconditions: a 429 or retry-after.
- Input: response.
- Output: the next attempt waits for retry-after or the backoff; rate limit never marks the connection failed; backoff is visible in health.
- Main flow: the sender returns Retryable; the broker retry applies fixed intervals.
- No-op / rejection / concurrency / retry: no hot loop; per-connection isolation so one bad receiver cannot stall others.
- UI states: "rate-limited" chip.
- Rules: integrations.md §58, §59 INT-021; AIREQ068, AIREQ115.
- Acceptance: Given retry-after 30 s, Then no attempt is made before 30 s.
- Evidence: BE `OutboundWebhookSender` (429 as retryable), consumer definition (`ConcurrentMessageLimit` 4) · FE none · Design none · Tests none · Real e2e none
- State: IMPLEMENTING — retry-after is not honored.

### UC-INT-L02 Queued work after a disconnect never runs
- Capability: INT-012 Rate/retry/provider limits
- Actors and permission: system.
- Preconditions: deliveries or operations queued when a connection is revoked or paused.
- Input: queued intent.
- Output: the dispatch consumer skips a Paused, Revoked or deleted connection; no call uses a retired secret.
- Main flow: the consumer rechecks the connection at send.
- No-op / rejection / concurrency / retry: skipped intents are recorded as cancelled.
- UI states: none.
- Rules: integrations.md §102; AIREQ063.
- Acceptance: Given a queued delivery and a disconnect, Then no request is sent.
- Evidence: BE `IntegrationWebhookDispatchRequestedConsumer` · FE none · Design none · Tests T-II `WebhookFanOutRuntimeChainIntegrationTests` · Real e2e none
- State: IMPLEMENTING — skip is not recorded as a delivery state.

## Capability M. Entitlement integration

### UC-INT-M01 Plan limits on connections and premium providers
- Capability: INT-013 Entitlement integration
- Actors and permission: Owner and Admin.
- Preconditions: Billing capability facts for connections and providers.
- Input: connect.
- Output: allowed or rejected with a limit message; a downgrade pauses premium sync but keeps the connection, mappings and history; Billing never owns a connection.
- Main flow: ask Billing for the capability before connect.
- No-op / rejection / concurrency / retry: concurrent connects use one capacity operation id.
- UI states: plan-gated card, limit notice.
- Rules: integrations.md §88, §89 INT-030; AIREQ052, AIREQ053.
- Acceptance: Given a plan allowing 1 webhook, When a second is added, Then rejected with the limit message.
- Evidence: BE only a fixed `MaxWebhooksPerWorkspace` in `ConnectWebhook.cs` · FE none · Design `Final-Integrations` (plan-gated cards) · Tests none · Real e2e none
- State: DEFINED — the code uses a constant, not Billing.

## Capability N. Hardening and security

### UC-INT-N01 Outgoing webhooks cannot reach internal networks
- Capability: INT-014 Hardening/security
- Actors and permission: system.
- Preconditions: a webhook address.
- Input: address.
- Output: https only and public addresses in production; link-local (cloud metadata) never; the resolved address is checked at connect time so a name that later points inward is refused; redirects are not followed.
- Main flow: shape check at connect, then `ConnectCallback` check at send.
- No-op / rejection / concurrency / retry: the Development override allows http and private networks.
- UI states: address error on the field.
- Rules: integrations.md §107 threat model, §75; AIREQ066.
- Acceptance: Given `https://169.254.169.254/`, Then refused at connect and at send.
- Evidence: BE `WebhookAddressPolicy`, `WebhookTargetPolicy`, `OutboundWebhookSender.ConnectAsync` · FE FE-INT (field error) · Design none · Tests T-II `WebhookFanOutRuntimeChainIntegrationTests`, `backend/tests/Notrelix.Application.Tests/Features/Integrations/WebhookDeliveryRulesTests.cs` · Real e2e E2E-INT (a refused address is asserted)
- State: HARDENING — no test of the send-time DNS check in a hostile resolver case.

### UC-INT-N02 Connect, pause and disconnect are audited
- Capability: INT-014 Hardening/security
- Actors and permission: Owner and Admin read the audit log.
- Preconditions: a connection change.
- Input: the change.
- Output: an audit entry with actor, provider, action and result; no secret or token.
- Main flow: Governance records from Integrations facts.
- No-op / rejection / concurrency / retry: duplicates make one entry.
- UI states: audit list.
- Rules: integrations.md §83 INT-029, §84; AIREQ113. (enterprise baseline)
- Acceptance: Given a disconnect by Hoa, Then the audit log shows it.
- Evidence: BE domain facts exist (`IntegrationConnectionRevokedDomainEvent`); no audit consumer found · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — (enterprise baseline).

## Endpoints with no use case

None. Every route of `backend/src/Notrelix.API/Endpoints/Automation/**` and `backend/src/Notrelix.API/Endpoints/Integrations/**` appears in an Evidence line: Automation.Rules.Create (UC-AUT-A01), .List (A03), .Update (A04), .Delete (A05), .SetEnabled (E01), Automation.Executions.List (J01); Integrations.Connections.List (UC-INT-A01), .Authorize (C01), .Callback (C02), .Pause (B02), .Resume (B03), .Test (F02), .Disconnect (B04), Integrations.Webhooks.Connect (B01), Integrations.Calendar.Connect (H01), .Disconnect (H02), .HandleWebhook (G01).

# Part 4. Product decisions applied

Owner decisions (from the owner's decision list, used here by number; the text is restated, the source file is a private planning file):

- Product decision (owner, 2026-10-27), item 44, OD1: rules keep workspace URLs with a `boardId`; a board run feed is added later.
- Product decision (owner, 2026-10-27), item 44, OD2: editing a rule creates a new version; runs already started finish on the old one.
- Product decision (owner, 2026-10-27), item 44, OD3: drafts do not count toward the rule limit; only enabled rules count.
- Product decision (owner, 2026-10-27), item 44, OD4: conditions are "all of, equals" only; no OR and no nesting in version one.
- Product decision (owner, 2026-10-27), item 44, OD5: the first schedule release is daily and weekly at a fixed time with the time zone fixed per board; a missed run defaults to "run once".
- Product decision (owner, 2026-10-27), item 44, OD6: a board Manager picks the person a rule runs as (a person who can edit the board); a workspace Admin may take over.
- Product decision (owner, 2026-10-27), item 44, OD7: a rerun runs the failed step only, on the same version snapshot, at most 3 manual reruns per step.
- Product decision (owner, 2026-10-27), item 44, OD8: the manual trigger button on the item page is deferred.
- Product decision (owner, 2026-10-27), item 44, OD9: the email-to-board trigger is deferred until verified provider intake exists.
- Product decision (owner, 2026-10-27), item 44, OD10: the phone offers read, enable and disable, and the run log; editing stays on web.
- Product decision (owner, 2026-10-27), item 45: three providers (Google Calendar, Slack, outgoing webhook) plus "coming soon" cards; workspace scope; a `reauthorization-required` status; up to 8 retries per event; secret regeneration; disconnect keeps created links.
- Product decision (owner, 2026-10-27), item 47: mobile uses a bearer token with rotating refresh; offline is read cache only.
- Product decision (owner, 2026-10-27), items 56 and 57: Work Management publishes completed item facts through the outbox; the field-value-changed fact carries the stored JSON text.
- Product decision (owner, 2026-10-27), board archive: archiving a board pauses its automation rules and integration sync; unarchive resumes them.

Lead decisions made in this catalog:

- Lead decision (2026-10-30): workspace-level rules (no board) stay in the API but are not offered in the UI until a workspace-level trigger source exists (UC-AUT-A02).
- Lead decision (2026-10-30): deleted rules can be restored inside the 90-day history window and come back Disabled (UC-AUT-A07).
- Lead decision (2026-10-30): a condition that is not met is recorded as an execution with status Skipped and never counts as a failure (UC-AUT-C02).
- Lead decision (2026-10-30): a failed step stops the execution; later steps show Not run; earlier effects are kept; no automatic rollback (UC-AUT-D06).
- Lead decision (2026-10-30): the `SlackMessage` action is not released until the Slack outbound operation (UC-INT-F03) exists, and the API refuses unreleased action and trigger types (UC-AUT-D09, B04).
- Lead decision (2026-10-30): a board run feed first, a workspace run feed after (UC-AUT-J03).
- Lead decision (2026-10-30): run history of live rules is kept 400 days, then purged (UC-AUT-J04).
- Lead decision (2026-10-30): on a plan downgrade existing rules keep running and stay editable, new rules are blocked until under the limit, premium triggers or actions are paused with a visible reason, history is kept (UC-AUT-M03).
- Lead decision (2026-10-30): Automation has no service principal in version one; every rule runs as a named person (its runner). A service principal needs its own decision (automation.md §63).
- Lead decision (2026-10-30): version one has no third-party marketplace or plugin runtime; extensions are outbound webhooks or scoped API clients, and a directory needs an admin allow-list and a review process first (UC-INT-A03).
- Lead decision (2026-10-30): the Slack outbound adapter asks for the minimum scope `chat:write` only (UC-INT-F03).
- Lead decision (2026-10-30): in a sync conflict Notrelix is the source of truth for the dates and titles of items it owns; conflicts are recorded and shown; last-write-wins is not used (UC-INT-H06).
- Lead decision (2026-10-30): provider text is length-limited, stored as plain text, HTML is stripped, and a provider user never becomes a workspace member by being named in a payload (UC-INT-H08).

# Part 5. Canonical updates required

For `docs/product/automation.md`:

- Section 61.1 / 61.2: add that enable, disable and edit carry an expected version and that enable and disable carry an idempotency key.
- Section 12 (edit while enabled): state that every edit creates the next configuration version and a stale expected version is a conflict.
- Section 33 and 106: add the execution status Skipped: "A matching fact whose conditions are false is recorded as a Skipped execution, never as Failed, never counted toward automatic disable."
- Section 52: replace the policy list with: "A multi-action Execution stops at the first failed step. Later steps show Not run. Earlier effects stay. A manager may rerun the failed step only, on the same version snapshot, at most three times."
- Section 59: add "History of an enabled or disabled Rule is kept 400 days; history of a deleted Rule stays readable 90 days."
- Section 72 and 73: add the downgrade rule: "On a downgrade existing Rules keep running and stay editable; creating a Rule or enabling one over the limit is refused; premium triggers or actions pause with a visible reason; history is kept."
- Section 63: add "Version one has no service principal; every Rule runs as one named person."
- Section 73 / 79: record that only enabled Rules count toward the Rule limit (owner decision, drafts excluded).
- Section 84 / 109: add "The API refuses a trigger or action type that has no runtime consumer or executor."

For `docs/product/integrations.md`:

- Section 99: add "A Revoked Connection is never revived; connecting the same provider again creates a new Connection. A different provider account needs explicit confirmation."
- Section 110: add the version-one provider list (Google Calendar, Slack, outgoing webhook), the Slack scope `chat:write` for outbound messages, and "no third-party marketplace or plugin runtime in version one; extensions are outbound webhooks or scoped API clients."
- Section 60 to 64: add "In a sync conflict Notrelix is the source of truth for dates and titles of items it owns; conflicts are recorded and visible."
- Section 76: add "Provider text is stored as plain text, length-limited; HTML is stripped."
- Section 57: add the webhook retry budget "up to 8 attempts per event with backoff, then Failed and visible."
- Section 88 and 89: add that the per-workspace webhook maximum comes from Billing capability facts, not a constant.

# Part 6. Not covered

| Product-doc section | Reason |
|---|---|
| automation.md §1 to §4 (mission, owns, does not own, language) | Definitions; no flow of their own. |
| automation.md §53 Compensation | Folded into UC-AUT-D06 and UC-AUT-H03: compensation is a new action, not a rollback; no separate use case. |
| automation.md §63 Service principal | Lead decision: none in version one; recorded in Part 4. |
| automation.md §46 Documents action, §95 to §97 relations to Work, Documents, Collaboration | No Documents action is released (AIREQ025). Work and Collaboration actions are covered in D01 to D05. |
| automation.md §81, §82 Agents (AUT-027), §83, §84 arbitrary code (AUT-028) | Not product. Agents need explicit admission; arbitrary code is never offered. The domain `Agents` folder is evidence only (AIREQ045, AIREQ046). |
| automation.md §92 Analytics | Analytics owns derived metrics; see the Analytics catalog. |
| automation.md §99 Governance policy changes by rules (AUT-033) | A negative rule: no action may change permissions; enforced by the action allow-list (D01 to D09). |
| automation.md §105 Schedule deletion | Covered with rule delete in UC-AUT-A05 once schedules exist (UC-AUT-B05). |
| automation.md §108 to §122 (alignment, ambiguity, change impact, checklists, evidence, stop conditions) | Guidance for implementers, not user flows. |
| integrations.md §1 to §4, §118 to §119, §131 to §132 | Definitions and alignment notes. |
| integrations.md §5, §6 provider adapters, §114 to §117 migration, adapter replacement, service extraction | Architecture topics; covered by AIREQ119 to AIREQ125 and the architecture gates, no user flow. |
| integrations.md §11, §12 account versus workspace scope | Lead and owner decision 45: workspace scope only in version one; account-level connections are not offered. |
| integrations.md §13, §14, §83, §84 Governance relation | Covered by the permission line of each use case (ManageIntegrations). |
| integrations.md §86, §87 Identity and Accounts relations | Provider identity never becomes login or membership (UC-INT-H08, UC-INT-B06); no separate flow. |
| integrations.md §79 to §82 Work, Documents, Collaboration relations, file-provider adapters | File-provider adapters (OneDrive, SharePoint, Google Drive) are shown as coming-soon cards only (UC-INT-A02); building one needs a product decision. |
| integrations.md §91, §92 Analytics and Search | Owned by those contexts. |
| integrations.md §104, §105, §106 provider-held copies, data residency and privacy | Policy topics with no flow yet; deletion of provider-held copies is the provider's responsibility (INT-034); data residency needs an owner decision before a use case. |
| integrations.md §120 to §130 change impact, checklists, testing, stop conditions | Guidance for implementers. |
| Integrations on the phone | Owner decision 47 and the design give the phone read-only Automation only; no Integrations screen is designed. |
| Board-scoped connections | Owner decision 45: workspace scope only. |
