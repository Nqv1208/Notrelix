import React, { createContext, useContext, type ReactNode } from 'react';
import { createNotrelixClient, type NotrelixClient } from '@notrelix/contracts';
import { parseEnv, type ResolvedEnv } from '@notrelix/kernel';

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
  api: NotrelixClient;
  clock: ClockPort;
  telemetry: TelemetryPort;
  featureFlags: FeatureFlagsPort;
  env: ResolvedEnv;
}

export function createAppRuntime(rawEnv: Partial<Record<string, string | undefined>> = {}): AppRuntime {
  const resolvedEnv = parseEnv(rawEnv);
  const client = createNotrelixClient({ baseUrl: resolvedEnv.apiUrl });

  const defaultClock: ClockPort = {
    now: () => new Date(),
    isoNow: () => new Date().toISOString(),
  };

  const defaultTelemetry: TelemetryPort = {
    track: (event, properties) => {
      if (resolvedEnv.nodeEnv === 'development') {
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
    clock: defaultClock,
    telemetry: defaultTelemetry,
    featureFlags: defaultFlags,
    env: resolvedEnv,
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
