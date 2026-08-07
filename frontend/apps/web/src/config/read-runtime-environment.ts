import type { RuntimeEnvironmentInput, RuntimeMode } from '@notrelix/kernel';

function normalizeMode(rawMode: string | undefined): RuntimeMode {
  if (rawMode === 'production' || rawMode === 'test') {
    return rawMode;
  }
  return 'development';
}

export function readWebRuntimeEnvironment(
  env: ImportMetaEnv,
): RuntimeEnvironmentInput {
  return {
    mode: normalizeMode(env.MODE),
    apiUrl: env.VITE_API_URL,
    realtimeUrl: env.VITE_WS_URL,
    appUrl: env.VITE_APP_URL,
    releaseSha: env.VITE_RELEASE_SHA,
    mockApi: env.VITE_MOCK_API === 'true',
  };
}
