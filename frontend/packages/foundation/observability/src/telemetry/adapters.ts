import type {
  TelemetryErrorPayload,
  TelemetryPayload,
  TelemetryPort,
} from "./ports";
import { redactTelemetryProperties } from "./redaction";

export class ConsoleTelemetryAdapter implements TelemetryPort {
  public constructor(private readonly context: Record<string, unknown> = {}) {}

  public track(name: string, properties?: Record<string, unknown>): void {
    console.debug("[Telemetry]", {
      name,
      properties: redactTelemetryProperties(properties),
      context: redactTelemetryProperties(this.context) ?? {},
      timestamp: new Date().toISOString(),
    } satisfies TelemetryPayload);
  }

  public reportError(error: unknown, context?: Record<string, unknown>): void {
    console.error("[Telemetry Error]", {
      error,
      context: redactTelemetryProperties({ ...this.context, ...context }) ?? {},
      timestamp: new Date().toISOString(),
    } satisfies TelemetryErrorPayload);
  }

  public withContext(context: Record<string, unknown>): TelemetryPort {
    return new ConsoleTelemetryAdapter({ ...this.context, ...context });
  }

  public async flush(): Promise<void> {}
}

export class RecordingTelemetryAdapter implements TelemetryPort {
  public readonly events: TelemetryPayload[] = [];
  public readonly errors: TelemetryErrorPayload[] = [];

  public constructor(private readonly context: Record<string, unknown> = {}) {}

  public track(name: string, properties?: Record<string, unknown>): void {
    this.events.push({
      name,
      properties: redactTelemetryProperties(properties),
      context: redactTelemetryProperties(this.context) ?? {},
      timestamp: new Date().toISOString(),
    });
  }

  public reportError(error: unknown, context?: Record<string, unknown>): void {
    this.errors.push({
      error,
      context: redactTelemetryProperties({ ...this.context, ...context }) ?? {},
      timestamp: new Date().toISOString(),
    });
  }

  public withContext(context: Record<string, unknown>): TelemetryPort {
    return new RecordingTelemetryAdapter({ ...this.context, ...context });
  }

  public async flush(): Promise<void> {}
}

export class ProductionTelemetryAdapter implements TelemetryPort {
  public constructor(
    private readonly send: (
      payload: TelemetryPayload | TelemetryErrorPayload,
    ) => Promise<void> | void,
    private readonly context: Record<string, unknown> = {},
  ) {}

  public track(name: string, properties?: Record<string, unknown>): void {
    void this.send({
      name,
      properties: redactTelemetryProperties(properties),
      context: redactTelemetryProperties(this.context) ?? {},
      timestamp: new Date().toISOString(),
    });
  }

  public reportError(error: unknown, context?: Record<string, unknown>): void {
    void this.send({
      error,
      context: redactTelemetryProperties({ ...this.context, ...context }) ?? {},
      timestamp: new Date().toISOString(),
    });
  }

  public withContext(context: Record<string, unknown>): TelemetryPort {
    return new ProductionTelemetryAdapter(this.send, {
      ...this.context,
      ...context,
    });
  }

  public async flush(): Promise<void> {}
}
