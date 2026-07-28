import React, { createContext, useContext, type ReactNode } from 'react';
import { createNotrelixClient, type NotrelixClient, type NotrelixClientConfig, type SessionExpiredEvent } from '@notrelix/contracts';
import { parseEnv, type ResolvedRuntimeEnvironment, type RuntimeEnvironmentInput } from '@notrelix/kernel';
import { RealtimeClient } from '@notrelix/realtime';
import { createSessionEventBus, type SessionEventBus } from './session-event-bus';

export type { SessionEventBus, SessionExpiredEvent } from './session-event-bus';
export { useFeatureRuntimeDependencies, type FeatureRuntimeDependencies } from './use-feature-runtime-dependencies';

export interface ClockPort {
  now(): Date;
  isoNow(): string;
}

export interface TelemetryPort {
  track(event: string, properties?: Record<string, unknown>): void;
  reportError(error: unknown, context?: Record<string, unknown>): void;
  flush?(): Promise<void> | void;
}

export interface FeatureFlagsPort {
  isEnabled(flag: string): boolean;
  getFlags(): Record<string, boolean>;
}

export interface AppRuntimeFactories {
  readonly createApiClient?: (config: NotrelixClientConfig) => NotrelixClient;
  readonly createRealtimeClient?: (url: string) => RealtimeClient;
  readonly clock?: ClockPort;
  readonly telemetry?: TelemetryPort;
  readonly featureFlags?: FeatureFlagsPort;
}

export interface AppRuntime {
  readonly api: NotrelixClient;
  readonly realtime: RealtimeClient;
  readonly sessionEvents: SessionEventBus;
  readonly clock: ClockPort;
  readonly telemetry: TelemetryPort;
  readonly featureFlags: FeatureFlagsPort;
  readonly env: ResolvedRuntimeEnvironment;
  dispose(): void;
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

  const telemetry: TelemetryPort = factories.telemetry ?? {
    track: (event, properties) => {
      if (!resolvedEnv.isProduction) {
        console.debug(`[Telemetry] ${event}`, properties);
      }
    },
    reportError: (error, context) => {
      if (!resolvedEnv.isProduction) {
        console.error('[Telemetry Error]', error, context);
      }
    },
  };

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
    : new RealtimeClient(resolvedEnv.realtimeUrl);

  const featureFlags: FeatureFlagsPort = factories.featureFlags ?? {
    isEnabled: () => true,
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
    dispose(): void {
      if (disposed) return;
      disposed = true;

      sessionEvents.clear();
      realtimeClient.disconnect();
      telemetry.flush?.();
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
