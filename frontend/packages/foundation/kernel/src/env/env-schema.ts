import { z } from "zod";

export type RuntimeMode = "development" | "test" | "production";

export interface RuntimeEnvironmentInput {
  readonly mode?: RuntimeMode;
  readonly apiUrl?: string;
  readonly realtimeUrl?: string;
  readonly appUrl?: string;
  readonly releaseSha?: string;
  readonly mockApi?: boolean;
}

export interface ResolvedRuntimeEnvironment {
  readonly mode: RuntimeMode;
  readonly isProduction: boolean;
  readonly apiUrl: string;
  readonly realtimeUrl: string;
  readonly wsUrl: string; // Alias for realtimeUrl backward compatibility
  readonly appUrl: string;
  readonly releaseSha: string;
  readonly mockApi: boolean;
  readonly nodeEnv: RuntimeMode; // Alias for mode backward compatibility
}

export const envSchema = z.object({
  mode: z.enum(["development", "production", "test"]).default("development"),
  apiUrl: z.string().url().optional(),
  realtimeUrl: z.string().url().optional(),
  appUrl: z.string().url().optional(),
  releaseSha: z.string().optional(),
  mockApi: z.boolean().default(false),

  // Legacy Vite / Next environment mapping fallbacks
  VITE_API_URL: z.string().url().optional(),
  VITE_WS_URL: z.string().url().optional(),
  VITE_APP_URL: z.string().url().optional(),
  VITE_RELEASE_SHA: z.string().optional(),
  VITE_MOCK_API: z.string().optional(),
  NODE_ENV: z.enum(["development", "production", "test"]).optional(),
});

export function parseEnv(rawInput: Partial<Record<string, unknown>> = {}): ResolvedRuntimeEnvironment {
  const mode: RuntimeMode =
    (rawInput.mode as RuntimeMode) ||
    (rawInput.NODE_ENV as RuntimeMode) ||
    "development";

  const apiUrl =
    (rawInput.apiUrl as string) ||
    (rawInput.VITE_API_URL as string) ||
    (rawInput.NEXT_PUBLIC_API_URL as string);

  const realtimeUrl =
    (rawInput.realtimeUrl as string) ||
    (rawInput.VITE_WS_URL as string) ||
    (rawInput.NEXT_PUBLIC_WS_URL as string);

  const appUrl =
    (rawInput.appUrl as string) ||
    (rawInput.VITE_APP_URL as string);

  const releaseSha =
    (rawInput.releaseSha as string) ||
    (rawInput.VITE_RELEASE_SHA as string) ||
    "dev-local";

  const mockApi =
    typeof rawInput.mockApi === "boolean"
      ? rawInput.mockApi
      : rawInput.VITE_MOCK_API === "true";

  const isProduction = mode === "production";

  if (isProduction) {
    const missing: string[] = [];
    if (!apiUrl) missing.push("apiUrl");
    if (!realtimeUrl) missing.push("realtimeUrl");
    if (!appUrl) missing.push("appUrl");
    if (missing.length > 0) {
      throw new Error(
        `[Kernel Env] Missing required environment variables in production: ${missing.join(", ")}`
      );
    }
    if (mockApi) {
      throw new Error("[Kernel Env] mockApi cannot be true in production mode");
    }
  }

  const resolvedApiUrl = apiUrl ?? "http://localhost:8000/api/v1";
  const resolvedRealtimeUrl = realtimeUrl ?? "ws://localhost:8000/realtime";
  const resolvedAppUrl = appUrl ?? "http://localhost:3000";

  return {
    mode,
    isProduction,
    apiUrl: resolvedApiUrl,
    realtimeUrl: resolvedRealtimeUrl,
    wsUrl: resolvedRealtimeUrl,
    appUrl: resolvedAppUrl,
    releaseSha,
    mockApi,
    nodeEnv: mode,
  };
}

export type Env = ResolvedRuntimeEnvironment;
export const envSchemaDefinition = envSchema;
