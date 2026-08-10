/**
 * @notrelix/observability — Telemetry and monitoring
 *
 * Provides telemetry tracking and observability utilities.
 */

export {
  initObservability,
  getObservabilityConfig,
  type ObservabilityConfig,
} from "./init";
export { trackEvent, reportError, type EventProperties } from "./tracing";
export {
  trackEvent as trackTelemetryEvent,
  type TelemetryEvent,
} from "./telemetry/telemetry";
export type {
  TelemetryPort,
  TelemetryPayload,
  TelemetryErrorPayload,
} from "./telemetry/ports";
export {
  ConsoleTelemetryAdapter,
  RecordingTelemetryAdapter,
  ProductionTelemetryAdapter,
} from "./telemetry/adapters";
export { redactTelemetryProperties } from "./telemetry/redaction";
