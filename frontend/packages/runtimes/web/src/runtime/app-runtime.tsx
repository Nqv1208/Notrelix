import React, { createContext, useContext, type ReactNode } from 'react';
import { createNotrelixClient, type NotrelixClient, type NotrelixClientConfig, type SessionExpiredEvent } from '@notrelix/contracts';
import { parseEnv, type ResolvedRuntimeEnvironment, type RuntimeEnvironmentInput } from '@notrelix/kernel';
import { ConsoleTelemetryAdapter, type TelemetryPort } from '@notrelix/observability';
import { RealtimeClient, type RealtimeTransport } from '@notrelix/realtime';
import type { ClockPort } from '@notrelix/platform';
import { createSessionEventBus, type SessionEventBus } from './session-event-bus';
import { createBrowserWebSocketFactory } from '../realtime/browser-websocket-factory';

export { createSessionEventBus, type SessionEventBus, type SessionExpiredEvent } from './session-event-bus';
export { useFeatureRuntimeDependencies, type FeatureRuntimeDependencies } from './use-feature-runtime-dependencies';

export type { ClockPort } from '@notrelix/platform';

export interface FeatureFlagsPort {
  isEnabled(flag: string): boolean;
  getFlags(): Record<string, boolean>;
}

export interface AppRuntimeFactories {
  readonly createApiClient?: (config: NotrelixClientConfig) => NotrelixClient;
  readonly createRealtimeClient?: (url: string) => RealtimeTransport;
  readonly clock?: ClockPort;
  readonly telemetry?: TelemetryPort;
  readonly featureFlags?: FeatureFlagsPort;
}

export interface AppRuntime {
  readonly api: NotrelixClient;
  readonly realtime: RealtimeTransport;
  readonly sessionEvents: SessionEventBus;
  readonly clock: ClockPort;
  readonly telemetry: TelemetryPort;
  readonly featureFlags: FeatureFlagsPort;
  readonly env: ResolvedRuntimeEnvironment;
  dispose(): Promise<void>;
}

export function createAppRuntime(
  input: RuntimeEnvironmentInput,
  factories: AppRuntimeFactories = {}
): AppRuntime {
  const resolvedEnv = parseEnv(input);

  const clock: ClockPort = factories.clock ?? {
    now: () => new Date(),
    isoNow: () => new Date().toISOString(),
  };

  const telemetry: TelemetryPort = factories.telemetry ?? new ConsoleTelemetryAdapter({
    releaseSha: resolvedEnv.releaseSha,
    environment: resolvedEnv.mode,
  });

  const sessionEvents = createSessionEventBus((err, ctx) => telemetry.reportError(err, ctx));

  const client = factories.createApiClient
    ? factories.createApiClient({
        baseUrl: resolvedEnv.apiUrl,
        clock,
        onSessionExpired: (event: SessionExpiredEvent) => {
          sessionEvents.publish(event);
        },
      })
    : createNotrelixClient({
        baseUrl: resolvedEnv.apiUrl,
        clock,
        onSessionExpired: (event: SessionExpiredEvent) => {
          sessionEvents.publish(event);
        },
      });

  const realtimeClient = factories.createRealtimeClient
    ? factories.createRealtimeClient(resolvedEnv.realtimeUrl)
    : new RealtimeClient(resolvedEnv.realtimeUrl, {
        socketFactory: createBrowserWebSocketFactory(),
        telemetry,
      });

  const featureFlags: FeatureFlagsPort = factories.featureFlags ?? {
    isEnabled: () => false,
    getFlags: () => ({}),
  };

  let disposed = false;

  const runtime: AppRuntime = {
    api: client,
    realtime: realtimeClient,
    sessionEvents,
    clock,
    telemetry,
    featureFlags,
    env: resolvedEnv,
    async dispose(): Promise<void> {
      if (disposed) return;
      disposed = true;

      sessionEvents.clear();
      realtimeClient.dispose();
      await telemetry.flush();
    },
  };

  return Object.freeze(runtime);
}

const AppRuntimeContext = createContext<AppRuntime | null>(null);

export function AppRuntimeProvider({
  runtime,
  children,
}: {
  runtime: AppRuntime;
  children: ReactNode;
}) {
  return (
    <AppRuntimeContext.Provider value={runtime}>
      {children}
    </AppRuntimeContext.Provider>
  );
}

export function useAppRuntime(): AppRuntime {
  const context = useContext(AppRuntimeContext);
  if (!context) {
    throw new Error('useAppRuntime must be used within an AppRuntimeProvider');
  }
  return context;
}
