# FE-FZ-12 Backend Issue: Automation State Contract Blockers

## Scope

Frontend added the Automation state/testing boundary required by FE-FZ-12:

- `@notrelix/automation-state`
- `@notrelix/automation-testing`
- repository interfaces for rules, executions, and templates
- query keys/hooks, command helpers, and execution realtime adapter

The remaining work cannot be truthfully completed in frontend without backend/API contract confirmation.

## Blockers

### 1. REST API contract missing from generated frontend contracts

Frontend needs official endpoints for:

- list rules by workspace
- get rule detail
- create rule
- update rule
- enable rule
- disable rule
- delete rule
- test rule
- execution history with cursor pagination
- execution detail
- list templates

The frontend repository interfaces are ready, but no generated OpenAPI contract currently proves the URL shapes, request payloads, response payloads, pagination shape, or error model.

### 2. Retry/cancel execution support is unconfirmed

FE-FZ-12 includes:

- retry execution if backend supports it
- cancel execution if backend supports it

Frontend left these as optional repository capabilities:

- `AutomationExecutionRepository.retry?`
- `AutomationExecutionRepository.cancel?`

Backend must confirm whether these operations exist, which execution statuses allow them, and the expected conflict/error behavior.

### 3. Realtime event schema is not generated

Frontend currently has a module adapter for target event names:

- `automation.execution.started`
- `automation.execution.step-updated`
- `automation.execution.completed`
- `automation.execution.failed`

Generated realtime contracts currently do not include these event types. Backend/contracts must add official payload schemas with at least:

- `executionId`
- `ruleId`
- execution status
- sequence or aggregate version
- optional step update payload
- optional error payload

### 4. Sequence/version semantics need backend confirmation

Frontend adapter ignores stale events when `sequence` or `version` is older than cached execution detail. Backend must confirm whether Automation execution events use:

- global subscription sequence
- aggregate version
- both
- neither

Frontend needs one authoritative ordering field to avoid applying stale execution updates.

## Frontend Temporary Position

Until these contracts exist:

- Automation web must remain render/compose only.
- No web component should subscribe to socket directly.
- No fake REST URL should be introduced.
- No retry/cancel UI should be exposed as production behavior.
