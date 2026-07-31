export interface TelemetryPort {
  track(name: string, properties?: Record<string, unknown>): void;
  reportError(error: unknown, context?: Record<string, unknown>): void;
  withContext(context: Record<string, unknown>): TelemetryPort;
  flush(): Promise<void>;
}

export interface TelemetryPayload {
  name: string;
  properties?: Record<string, unknown>;
  context: Record<string, unknown>;
  timestamp: string;
}

export interface TelemetryErrorPayload {
  error: unknown;
  context: Record<string, unknown>;
  timestamp: string;
}
