import React, { createContext, useContext, type ReactNode } from 'react';
import { createNotrelixClient, type NotrelixClient } from '@notrelix/contracts';
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
}

export interface FeatureFlagsPort {
  isEnabled(flag: string): boolean;
  getFlags(): Record<string, boolean>;
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
  input: RuntimeEnvironmentInput | Partial<Record<string, string | undefined>> = {}
): AppRuntime {
  const resolvedEnv = parseEnv(input as Record<string, unknown>);
  const sessionEvents = createSessionEventBus();

  const client = createNotrelixClient({
    baseUrl: resolvedEnv.apiUrl,
    onSessionExpired: (error) => {
      sessionEvents.publish({
        type: 'session-expired',
        error,
        occurredAt: new Date().toISOString(),
      });
    },
  });

  const realtimeClient = new RealtimeClient(resolvedEnv.realtimeUrl);

  const defaultClock: ClockPort = {
    now: () => new Date(),
    isoNow: () => new Date().toISOString(),
  };

  const defaultTelemetry: TelemetryPort = {
    track: (event, properties) => {
      if (!resolvedEnv.isProduction) {
        console.debug(`[Telemetry] ${event}`, properties);
      }
    },
    reportError: (error, context) => {
      console.error('[Telemetry Error]', error, context);
    },
  };

  const defaultFlags: FeatureFlagsPort = {
    isEnabled: () => true,
    getFlags: () => ({}),
  };

  return {
    api: client,
    realtime: realtimeClient,
    sessionEvents,
    clock: defaultClock,
    telemetry: defaultTelemetry,
    featureFlags: defaultFlags,
    env: resolvedEnv,
    dispose(): void {
      sessionEvents.clear();
      realtimeClient.disconnect();
    },
  };
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
