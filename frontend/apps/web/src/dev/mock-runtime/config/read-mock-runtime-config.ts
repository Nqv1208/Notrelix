import {
  mockPersonas,
  mockScenarios,
  type MockPersona,
  type MockRuntimeConfig,
  type MockScenario,
} from "./mock-runtime-config";

type MockRuntimeEnvironment = Pick<
  ImportMetaEnv,
  "VITE_MOCK_PERSONA" | "VITE_MOCK_SCENARIO" | "VITE_MOCK_LATENCY_MS"
>;

function readAllowedValue<T extends string>(
  name: string,
  value: string | undefined,
  allowed: readonly T[],
  fallback: T,
): T {
  if (!value) return fallback;
  if (allowed.includes(value as T)) return value as T;
  throw new Error(`[Mock Runtime] Invalid ${name}: ${value}`);
}

export function readMockRuntimeConfig(
  env: MockRuntimeEnvironment,
): MockRuntimeConfig {
  const latencyMs = env.VITE_MOCK_LATENCY_MS
    ? Number(env.VITE_MOCK_LATENCY_MS)
    : 0;
  if (!Number.isFinite(latencyMs) || latencyMs < 0) {
    throw new Error(
      `[Mock Runtime] Invalid VITE_MOCK_LATENCY_MS: ${env.VITE_MOCK_LATENCY_MS}`,
    );
  }

  return {
    persona: readAllowedValue<MockPersona>(
      "VITE_MOCK_PERSONA",
      env.VITE_MOCK_PERSONA,
      mockPersonas,
      "owner",
    ),
    scenario: readAllowedValue<MockScenario>(
      "VITE_MOCK_SCENARIO",
      env.VITE_MOCK_SCENARIO,
      mockScenarios,
      "default",
    ),
    latencyMs,
  };
}
