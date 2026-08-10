import {
  parseEnv,
  type ResolvedRuntimeEnvironment,
  type RuntimeEnvironmentInput,
} from "@notrelix/kernel";
import {
  ConsoleTelemetryAdapter,
  type TelemetryPort,
} from "@notrelix/observability";
import { RealtimeClient, type RealtimeTransport } from "@notrelix/realtime";
import type { ClockPort } from "@notrelix/platform";
import { createNativeWebSocketFactory } from "../realtime/native-websocket-factory";

export interface MobileRuntimeFactories {
  clock?: ClockPort;
  telemetry?: TelemetryPort;
  createRealtimeClient?: (url: string) => RealtimeTransport;
}

export interface MobileRuntime {
  readonly realtime: RealtimeTransport;
  readonly clock: ClockPort;
  readonly telemetry: TelemetryPort;
  readonly env: ResolvedRuntimeEnvironment;
  dispose(): Promise<void>;
}

export function createMobileRuntime(
  input: RuntimeEnvironmentInput,
  factories: MobileRuntimeFactories = {},
): MobileRuntime {
  const resolvedEnv = parseEnv(input);

  const clock: ClockPort = factories.clock ?? {
    now: () => new Date(),
    isoNow: () => new Date().toISOString(),
  };

  const telemetry: TelemetryPort =
    factories.telemetry ??
    new ConsoleTelemetryAdapter({
      releaseSha: resolvedEnv.releaseSha,
      environment: resolvedEnv.mode,
    });

  const realtimeClient = factories.createRealtimeClient
    ? factories.createRealtimeClient(resolvedEnv.realtimeUrl)
    : new RealtimeClient(resolvedEnv.realtimeUrl, {
        socketFactory: createNativeWebSocketFactory(),
        telemetry,
      });

  let disposed = false;

  const runtime: MobileRuntime = {
    realtime: realtimeClient,
    clock,
    telemetry,
    env: resolvedEnv,
    async dispose(): Promise<void> {
      if (disposed) return;
      disposed = true;

      realtimeClient.dispose();
      await telemetry.flush();
    },
  };

  return Object.freeze(runtime);
}
