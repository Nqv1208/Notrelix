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
 * Plan: 05-SCENARIOS-DENSITY-FAULTS.md, 01-FREEZE-SPEC.md §FZ-S09, 02-IMPLEMENTATION-PLAN.md §MFB-FZ-00
 */

export class MockConfigurationError extends Error {
  constructor(message: string) {
    super(`[MockConfigurationError] ${message}`);
    this.name = "MockConfigurationError";
  }
}

// ─── Dimension types ──────────────────────────────────────────────────────────

export type MockPersona = "owner" | "admin" | "member" | "viewer";

export type MockBusinessState =
  | "default"
  | "new-user"
  | "empty-workspace"
  | "permission-limited"
  | "expired-session";

export type MockDensity = "tiny" | "normal" | "large" | "stress";

export type MockOverlay =
  "long-titles" | "unicode" | "missing-avatars" | "many-columns" | "many-cards";

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
    throw new MockConfigurationError(
      `Unknown preset "${presetName}". Available presets: ${Object.keys(NAMED_PRESETS).join(", ")}`,
    );
  }
  return { ...base, ...preset };
}

// ─── Env variable parsing (Plan: 05-SCENARIOS-DENSITY-FAULTS.md §Env parsing) ─

const VALID_PERSONAS = new Set<string>(["owner", "admin", "member", "viewer"]);
const VALID_STATES = new Set<string>([
  "default",
  "new-user",
  "empty-workspace",
  "permission-limited",
  "expired-session",
]);
const VALID_DENSITIES = new Set<string>(["tiny", "normal", "large", "stress"]);
const VALID_LATENCIES = new Set<string>(["instant", "fast", "normal", "slow"]);
const VALID_OVERLAYS = new Set<string>([
  "long-titles",
  "unicode",
  "missing-avatars",
  "many-columns",
  "many-cards"
]);


export function parseMockConfigFromEnv(
  env: Record<string, string | undefined>,
): Partial<MockBackendConfig> {
  const partial: Partial<MockBackendConfig> = {};

  // VITE_MOCK_PRESET — fail fast if unknown
  if (env["VITE_MOCK_PRESET"] !== undefined && env["VITE_MOCK_PRESET"] !== "") {
    const presetName = env["VITE_MOCK_PRESET"];
    const preset = NAMED_PRESETS[presetName];
    if (!preset) {
      throw new MockConfigurationError(
        `Invalid VITE_MOCK_PRESET="${presetName}". Available presets: ${Object.keys(NAMED_PRESETS).join(", ")}`,
      );
    }
    Object.assign(partial, preset);
  }

  // VITE_MOCK_PERSONA
  if (
    env["VITE_MOCK_PERSONA"] !== undefined &&
    env["VITE_MOCK_PERSONA"] !== ""
  ) {
    const val = env["VITE_MOCK_PERSONA"];
    if (!VALID_PERSONAS.has(val)) {
      throw new MockConfigurationError(
        `Invalid VITE_MOCK_PERSONA="${val}". Must be one of: ${Array.from(VALID_PERSONAS).join(", ")}`,
      );
    }
    partial.persona = val as MockPersona;
  }

  // VITE_MOCK_STATE
  if (env["VITE_MOCK_STATE"] !== undefined && env["VITE_MOCK_STATE"] !== "") {
    const val = env["VITE_MOCK_STATE"];
    if (!VALID_STATES.has(val)) {
      throw new MockConfigurationError(
        `Invalid VITE_MOCK_STATE="${val}". Must be one of: ${Array.from(VALID_STATES).join(", ")}`,
      );
    }
    partial.state = val as MockBusinessState;
  }

  // VITE_MOCK_DENSITY
  if (
    env["VITE_MOCK_DENSITY"] !== undefined &&
    env["VITE_MOCK_DENSITY"] !== ""
  ) {
    const val = env["VITE_MOCK_DENSITY"];
    if (!VALID_DENSITIES.has(val)) {
      throw new MockConfigurationError(
        `Invalid VITE_MOCK_DENSITY="${val}". Must be one of: ${Array.from(VALID_DENSITIES).join(", ")}`,
      );
    }
    partial.density = val as MockDensity;
  }

  // VITE_MOCK_LATENCY
  if (
    env["VITE_MOCK_LATENCY"] !== undefined &&
    env["VITE_MOCK_LATENCY"] !== ""
  ) {
    const val = env["VITE_MOCK_LATENCY"];
    if (!VALID_LATENCIES.has(val)) {
      throw new MockConfigurationError(
        `Invalid VITE_MOCK_LATENCY="${val}". Must be one of: ${Array.from(VALID_LATENCIES).join(", ")}`,
      );
    }
    partial.latency = val as MockLatency;
  }


  // VITE_MOCK_OVERLAYS
  if (
    env["VITE_MOCK_OVERLAYS"] !== undefined &&
    env["VITE_MOCK_OVERLAYS"] !== ""
  ) {
    const raw = env["VITE_MOCK_OVERLAYS"];
    const overlays = raw.split(",").map((s) => s.trim()).filter(Boolean);
    for (const o of overlays) {
      if (!VALID_OVERLAYS.has(o)) {
        throw new MockConfigurationError(
          `Invalid overlay "${o}" in VITE_MOCK_OVERLAYS. Must be a comma-separated list of: ${Array.from(VALID_OVERLAYS).join(", ")}`
        );
      }
    }
    partial.overlays = overlays as any;
  }

  // VITE_MOCK_SEED
  if (env["VITE_MOCK_SEED"] !== undefined && env["VITE_MOCK_SEED"] !== "") {
    const raw = env["VITE_MOCK_SEED"];
    const seed = Number(raw);
    if (!Number.isSafeInteger(seed) || seed < 0) {
      throw new MockConfigurationError(
        `Invalid VITE_MOCK_SEED="${raw}". Must be a non-negative safe integer.`,
      );
    }
    partial.seed = seed;
  }

  return partial;
}
