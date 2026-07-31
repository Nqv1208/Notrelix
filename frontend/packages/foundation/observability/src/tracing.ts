import { getObservabilityConfig } from './init';

export interface EventProperties {
  [key: string]: string | number | boolean | null | undefined;
}

export function trackEvent(eventName: string, properties?: EventProperties): void {
  const config = getObservabilityConfig();
  if (!config.enabled) return;

  if (config.isDevelopment) {
    console.log(`[Telemetry] ${eventName}`, properties);
  }
}

export function reportError(error: unknown, context?: Record<string, unknown>): void {
  const config = getObservabilityConfig();
  if (!config.enabled) return;

  const message = error instanceof Error ? error.message : String(error);
  if (config.isDevelopment) {
    console.error(`[ErrorReport] ${message}`, context);
  }
}
