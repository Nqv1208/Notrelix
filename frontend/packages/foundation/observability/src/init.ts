export interface ObservabilityConfig {
  readonly enabled?: boolean;
  readonly environment?: string;
  readonly appVersion?: string;
  readonly sampleRate?: number;
  readonly isDevelopment?: boolean;
}

let activeConfig: ObservabilityConfig = {
  enabled: true,
  environment: 'development',
  appVersion: '0.1.0',
  sampleRate: 1.0,
  isDevelopment: true,
};

export function initObservability(config: ObservabilityConfig = {}): ObservabilityConfig {
  activeConfig = {
    ...activeConfig,
    ...config,
  };
  return activeConfig;
}

export function getObservabilityConfig(): ObservabilityConfig {
  return activeConfig;
}
