/**
 * @notrelix/observability — Telemetry and monitoring
 *
 * Provides telemetry tracking and observability utilities.
 */

export { initObservability, getObservabilityConfig, type ObservabilityConfig } from './init';
export { trackEvent, reportError, type EventProperties } from './tracing';
export { trackEvent as trackTelemetryEvent, type TelemetryEvent } from './telemetry/telemetry';
