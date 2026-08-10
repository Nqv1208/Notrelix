// Centralized telemetry tracker contract.

export type TelemetryEvent = {
  name: string;
  properties: Record<string, unknown> | undefined;
  timestamp: number;
};

let debugTelemetry = false;

export function configureTelemetry(options: { debug?: boolean }) {
  debugTelemetry = Boolean(options.debug);
}

export function trackEvent(name: string, properties?: Record<string, unknown>) {
  const event: TelemetryEvent = {
    name,
    properties,
    timestamp: Date.now(),
  };

  if (debugTelemetry) {
    console.log("[Telemetry Event]:", event);
  }

  // Production mapping would go here to send event payloads to telemetry providers.
}
