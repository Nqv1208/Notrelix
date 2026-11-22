import type { RuntimeEnvironmentInput, RuntimeMode } from "@notrelix/kernel";

function normalizeMode(rawMode: string | undefined): RuntimeMode {
  if (rawMode === "production" || rawMode === "test") {
    return rawMode;
  }
  return "development";
}

/**
 * Release images are promoted unchanged across environments, so no URL is
 * baked in: unless a VITE_* value is set explicitly (e.g. dev against a remote
 * API), everything resolves to the origin the page was loaded from. nginx serves
 * the API at /api/v1 and the realtime gateway at /api/v1/realtime on that origin
 * (the realtime client appends the path to an origin-only ws(s) URL).
 */
export function readWebRuntimeEnvironment(
  env: ImportMetaEnv,
  origin: string | undefined = typeof window === "undefined"
    ? undefined
    : window.location.origin,
): RuntimeEnvironmentInput {
  return {
    mode: normalizeMode(env.MODE),
    apiUrl: env.VITE_API_URL || (origin && `${origin}/api/v1`) || undefined,
    realtimeUrl:
      env.VITE_WS_URL || (origin && origin.replace(/^http/, "ws")) || undefined,
    appUrl: env.VITE_APP_URL || origin || undefined,
    releaseSha: env.VITE_RELEASE_SHA,
    mockApi: env.VITE_MOCK_API === "true",
  };
}
