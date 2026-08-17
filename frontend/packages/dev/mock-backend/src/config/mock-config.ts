/**
 * MockBackendConfig — multidimensional configuration for the mock backend.
 *
 * Config dimensions:
 *   persona   — who the "current user" is (controls getCurrentUser() response)
 *   state     — business state scenario (controls seedBaseWorld() shape)
 *   density   — data volume (controls how many records are seeded)
 *   overlays  — content edge-case modifiers
 *   latency   — simulated network delay profile
 *   faultProfile — per-route forced error injection
 *   seed      — deterministic PRNG seed for factories + clock
 *
 * Named presets:
 *   Defined in this file as NAMED_PRESETS.
 *   Apply with: applyPreset(presetName, baseConfig)
 *
 * Environment variable parsing:
 *   parseMockConfigFromEnv(import.meta.env) → Partial<MockBackendConfig>
 *   Merge with defaultConfig in composition root.
 *
 * Plan: 05-SCENARIOS-DENSITY-FAULTS.md, 07-IMPLEMENTATION-MIGRATION-PLAN.md
 */

// ─── Dimension types ──────────────────────────────────────────────────────────

export type MockPersona = "owner" | "admin" | "member" | "viewer";

export type MockBusinessState =
  | "default"
  | "new-user"
  | "empty-workspace"
  | "permission-limited"
  | "expired-session"
  | "onboarding";

export type MockDensity = "tiny" | "normal" | "large" | "stress";

export type MockOverlay =
  | "long-titles"
  | "unicode"
  | "missing-avatars"
  | "many-columns"
  | "many-cards";

export type MockLatency = "instant" | "fast" | "normal" | "slow";

export interface MockFaultProfile {
  /** Key: normalized pathname or "*" for all routes. Value: fault type. */
  [pathOrStar: string]: "network" | "401" | "409" | "500" | "validation";
}

// ─── Config interface ─────────────────────────────────────────────────────────

export interface MockBackendConfig {
  seed: number;
  persona: MockPersona;
  state: MockBusinessState;
  density: MockDensity;
  overlays: readonly MockOverlay[];
  faultProfile: MockFaultProfile;
  latency: MockLatency;
}

// ─── Default config ───────────────────────────────────────────────────────────

export const defaultConfig: MockBackendConfig = {
  seed: 1001,
  persona: "owner",
  state: "default",
  density: "normal",
  overlays: [],
  faultProfile: {},
  latency: "instant",
};

// ─── Named presets (Plan: 05-SCENARIOS-DENSITY-FAULTS.md §Named presets) ──────

export const NAMED_PRESETS: Record<string, Partial<MockBackendConfig>> = {
  "ui-default": {
    persona: "owner",
    state: "default",
    density: "normal",
    latency: "instant",
  },
  "ui-empty": {
    persona: "owner",
    state: "empty-workspace",
    density: "tiny",
    latency: "instant",
  },
  "ui-new-user": {
    persona: "owner",
    state: "new-user",
    density: "tiny",
    latency: "instant",
  },
  "ui-viewer": {
    persona: "viewer",
    state: "permission-limited",
    density: "normal",
    latency: "instant",
  },
  "ui-large-board": {
    persona: "owner",
    state: "default",
    density: "large",
    latency: "instant",
  },
  "ui-stress": {
    persona: "owner",
    state: "default",
    density: "stress",
    latency: "instant",
  },
  "ui-slow-network": {
    persona: "owner",
    state: "default",
    density: "normal",
    latency: "slow",
  },
  "ui-normal-network": {
    persona: "owner",
    state: "default",
    density: "normal",
    latency: "normal",
  },
  "ui-expired-session": {
    persona: "owner",
    state: "expired-session",
    density: "tiny",
    latency: "instant",
  },
  "ui-onboarding": {
    persona: "owner",
    state: "onboarding",
    density: "tiny",
    latency: "fast",
  },
  "ui-all-errors": {
    persona: "owner",
    state: "default",
    density: "normal",
    latency: "instant",
    faultProfile: { "*": "500" },
  },
};

export function applyPreset(
  presetName: string,
  base: MockBackendConfig = defaultConfig,
): MockBackendConfig {
  const preset = NAMED_PRESETS[presetName];
  if (!preset) {
    console.warn(`[MockBackend] Unknown preset: ${presetName}. Using defaultConfig.`);
    return base;
  }
  return { ...base, ...preset };
}

// ─── Env variable parsing (Plan: 05-SCENARIOS-DENSITY-FAULTS.md §Env parsing) ─

const VALID_PERSONAS = new Set<MockPersona>(["owner", "admin", "member", "viewer"]);
const VALID_STATES = new Set<MockBusinessState>([
  "default", "new-user", "empty-workspace", "permission-limited", "expired-session", "onboarding",
]);
const VALID_DENSITIES = new Set<MockDensity>(["tiny", "normal", "large", "stress"]);
const VALID_LATENCIES = new Set<MockLatency>(["instant", "fast", "normal", "slow"]);

export function parseMockConfigFromEnv(
  env: Record<string, string | undefined>,
): Partial<MockBackendConfig> {
  const partial: Partial<MockBackendConfig> = {};

  // VITE_MOCK_PRESET — apply named preset first; individual vars override
  if (env["VITE_MOCK_PRESET"] && NAMED_PRESETS[env["VITE_MOCK_PRESET"]]) {
    Object.assign(partial, NAMED_PRESETS[env["VITE_MOCK_PRESET"]]);
  }

  if (env["VITE_MOCK_PERSONA"] && VALID_PERSONAS.has(env["VITE_MOCK_PERSONA"] as MockPersona)) {
    partial.persona = env["VITE_MOCK_PERSONA"] as MockPersona;
  }

  if (env["VITE_MOCK_STATE"] && VALID_STATES.has(env["VITE_MOCK_STATE"] as MockBusinessState)) {
    partial.state = env["VITE_MOCK_STATE"] as MockBusinessState;
  }

  if (env["VITE_MOCK_DENSITY"] && VALID_DENSITIES.has(env["VITE_MOCK_DENSITY"] as MockDensity)) {
    partial.density = env["VITE_MOCK_DENSITY"] as MockDensity;
  }

  if (env["VITE_MOCK_LATENCY"] && VALID_LATENCIES.has(env["VITE_MOCK_LATENCY"] as MockLatency)) {
    partial.latency = env["VITE_MOCK_LATENCY"] as MockLatency;
  }

  if (env["VITE_MOCK_SEED"]) {
    const seed = parseInt(env["VITE_MOCK_SEED"], 10);
    if (!isNaN(seed)) partial.seed = seed;
  }

  return partial;
}
