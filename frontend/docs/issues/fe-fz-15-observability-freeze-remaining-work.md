# FE-FZ-15 Issue: Observability Freeze Remaining Work

## Scope

Frontend centralized the telemetry contract in `@notrelix/observability` and updated runtime-web to import that contract.

Implemented:

- `TelemetryPort`
- `ConsoleTelemetryAdapter`
- `RecordingTelemetryAdapter`
- `ProductionTelemetryAdapter` shell
- `withContext()`
- required `flush()`
- telemetry property redaction for sensitive keys
- runtime default telemetry context with release SHA and environment mode

## Remaining Work

### 1. Production telemetry transport contract is missing

`ProductionTelemetryAdapter` accepts a generic sender, but the product does not yet define:

- vendor/provider
- HTTP endpoint
- batching behavior
- retry/drop policy
- authentication model
- sampling policy

### 2. Capture coverage is incomplete

FE-FZ-15 requires capture for:

- unhandled promise rejections
- API duration/error kind
- route navigation timing
- realtime connect/reconnect/recovery
- Web Vitals

Only existing runtime/error-boundary/reporting paths are wired today.

### 3. Context lifecycle needs full app wiring

The contract supports `withContext()`, but route/workspace/session/correlation context updates are not fully propagated through the app lifecycle.

### 4. PII redaction policy needs product/security confirmation

Frontend added conservative key-based redaction for email/token/secret/body/content/payload. Product/security should confirm the final whitelist and vendor payload shape.

## Acceptance Criteria

- Production telemetry sender is configured with a real transport contract.
- Runtime captures unhandled rejections and Web Vitals.
- API client emits duration/error-kind telemetry without body/raw content.
- Router/workspace/session context is updated through `withContext()`.
- Realtime lifecycle events use the centralized telemetry port.
- Redaction policy is reviewed and covered by tests.
