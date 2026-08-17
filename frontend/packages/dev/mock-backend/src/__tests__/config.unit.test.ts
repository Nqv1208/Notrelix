import { describe, it, expect } from "vitest";
import {
  parseMockConfigFromEnv,
  applyPreset,
  MockConfigurationError,
} from "../config/mock-config";

describe("MFB-FZ-00: Mock Configuration and Env Parsing", () => {
  it("parses valid v3 environment variables", () => {
    const parsed = parseMockConfigFromEnv({
      VITE_MOCK_PERSONA: "admin",
      VITE_MOCK_STATE: "empty-workspace",
      VITE_MOCK_DENSITY: "large",
      VITE_MOCK_LATENCY: "fast",
      VITE_MOCK_SEED: "42",
    });

    expect(parsed.persona).toBe("admin");
    expect(parsed.state).toBe("empty-workspace");
    expect(parsed.density).toBe("large");
    expect(parsed.latency).toBe("fast");
    expect(parsed.seed).toBe(42);
  });

  it("applies valid preset correctly", () => {
    const config = applyPreset("ui-empty");
    expect(config.state).toBe("empty-workspace");
    expect(config.density).toBe("tiny");
  });

  it("throws MockConfigurationError for unknown preset via applyPreset", () => {
    expect(() => applyPreset("unknown-preset")).toThrow(MockConfigurationError);
  });

  it("throws MockConfigurationError for unknown VITE_MOCK_PRESET", () => {
    expect(() =>
      parseMockConfigFromEnv({ VITE_MOCK_PRESET: "does-not-exist" }),
    ).toThrow(MockConfigurationError);
  });

  it("throws MockConfigurationError for invalid VITE_MOCK_PERSONA", () => {
    expect(() =>
      parseMockConfigFromEnv({ VITE_MOCK_PERSONA: "invalid-persona" }),
    ).toThrow(MockConfigurationError);
  });

  it("throws MockConfigurationError for invalid VITE_MOCK_STATE", () => {
    expect(() =>
      parseMockConfigFromEnv({ VITE_MOCK_STATE: "invalid-state" }),
    ).toThrow(MockConfigurationError);
  });

  it("throws MockConfigurationError for invalid VITE_MOCK_DENSITY", () => {
    expect(() =>
      parseMockConfigFromEnv({ VITE_MOCK_DENSITY: "invalid-density" }),
    ).toThrow(MockConfigurationError);
  });

  it("throws MockConfigurationError for invalid VITE_MOCK_LATENCY", () => {
    expect(() =>
      parseMockConfigFromEnv({ VITE_MOCK_LATENCY: "invalid-latency" }),
    ).toThrow(MockConfigurationError);
  });

  it("throws MockConfigurationError for invalid VITE_MOCK_SEED (text/negative/float)", () => {
    expect(() =>
      parseMockConfigFromEnv({ VITE_MOCK_SEED: "not-a-number" }),
    ).toThrow(MockConfigurationError);

    expect(() => parseMockConfigFromEnv({ VITE_MOCK_SEED: "-5" })).toThrow(
      MockConfigurationError,
    );

    expect(() => parseMockConfigFromEnv({ VITE_MOCK_SEED: "3.14" })).toThrow(
      MockConfigurationError,
    );
  });
});
