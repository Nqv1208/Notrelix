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
  readonly isDevelopment: boolean;
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

function isValidUrl(val: string | undefined): boolean {
  if (!val || typeof val !== "string") return false;
  try {
    new URL(val);
    return true;
  } catch {
    return false;
  }
}

export function parseEnv(
  rawInput: RuntimeEnvironmentInput | Record<string, unknown> = {},
): ResolvedRuntimeEnvironment {
  const input = rawInput as Record<string, unknown>;

  const mode: RuntimeMode =
    input.mode === "production" ||
    input.mode === "test" ||
    input.mode === "development"
      ? (input.mode as RuntimeMode)
      : input.NODE_ENV === "production" ||
          input.NODE_ENV === "test" ||
          input.NODE_ENV === "development"
        ? (input.NODE_ENV as RuntimeMode)
        : "development";

  const rawApiUrl =
    (input.apiUrl as string) ||
    (input.VITE_API_URL as string) ||
    (input.NEXT_PUBLIC_API_URL as string);

  const rawRealtimeUrl =
    (input.realtimeUrl as string) ||
    (input.VITE_WS_URL as string) ||
    (input.NEXT_PUBLIC_WS_URL as string);

  const rawAppUrl = (input.appUrl as string) || (input.VITE_APP_URL as string);

  const releaseSha =
    (input.releaseSha as string) ||
    (input.VITE_RELEASE_SHA as string) ||
    "dev-local";

  const mockApi =
    typeof input.mockApi === "boolean"
      ? input.mockApi
      : input.VITE_MOCK_API === "true";

  const isProduction = mode === "production";
  const isDevelopment = mode === "development";

  if (isProduction) {
    const missing: string[] = [];
    if (!isValidUrl(rawApiUrl)) missing.push("apiUrl");
    if (!isValidUrl(rawRealtimeUrl)) missing.push("realtimeUrl");
    if (!isValidUrl(rawAppUrl)) missing.push("appUrl");
    if (missing.length > 0) {
      throw new Error(
        `[Kernel Env] Missing or invalid required environment variables in production: ${missing.join(", ")}`,
      );
    }
    if (mockApi) {
      throw new Error("[Kernel Env] mockApi cannot be true in production mode");
    }
  }

  const resolvedApiUrl = isValidUrl(rawApiUrl)
    ? rawApiUrl!
    : "http://localhost:5000";
  const resolvedRealtimeUrl = isValidUrl(rawRealtimeUrl)
    ? rawRealtimeUrl!
    : "ws://localhost:5000/realtime";
  const resolvedAppUrl = isValidUrl(rawAppUrl)
    ? rawAppUrl!
    : "http://localhost:3000";

  return {
    mode,
    isProduction,
    isDevelopment,
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
