import { afterEach, describe, expect, it, vi } from "vitest";

async function importEnv() {
  vi.resetModules();
  return import("../env");
}

describe("mobile app env config", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("maps Expo public values to canonical runtime env input", async () => {
    vi.stubEnv("NODE_ENV", "production");
    vi.stubEnv("EXPO_PUBLIC_API_URL", "https://api.mobile.test");
    vi.stubEnv("EXPO_PUBLIC_REALTIME_URL", "wss://realtime.mobile.test");
    vi.stubEnv("EXPO_PUBLIC_APP_URL", "https://app.mobile.test");
    vi.stubEnv("EXPO_PUBLIC_RELEASE_SHA", "mobile-sha");

    const { env } = await importEnv();

    expect(env.mode).toBe("production");
    expect(env.apiUrl).toBe("https://api.mobile.test");
    expect(env.realtimeUrl).toBe("wss://realtime.mobile.test");
    expect(env.appUrl).toBe("https://app.mobile.test");
    expect(env.releaseSha).toBe("mobile-sha");
  });

  it("defaults non-production NODE_ENV values to development mode", async () => {
    vi.stubEnv("NODE_ENV", "staging");

    const { env } = await importEnv();

    expect(env.mode).toBe("development");
    expect(new URL(env.apiUrl).protocol).toMatch(/^https?:$/);
    expect(new URL(env.realtimeUrl).protocol).toMatch(/^(https?|wss?):$/);
  });

  it("fails production validation when API URL is absent", async () => {
    vi.stubEnv("NODE_ENV", "production");
    vi.stubEnv("EXPO_PUBLIC_API_URL", "");
    vi.stubEnv("EXPO_PUBLIC_REALTIME_URL", "wss://realtime.mobile.test");
    vi.stubEnv("EXPO_PUBLIC_APP_URL", "https://app.mobile.test");

    await expect(importEnv()).rejects.toThrow(/apiUrl/);
  });

  it("fails production validation when realtime URL is absent", async () => {
    vi.stubEnv("NODE_ENV", "production");
    vi.stubEnv("EXPO_PUBLIC_API_URL", "https://api.mobile.test");
    vi.stubEnv("EXPO_PUBLIC_REALTIME_URL", "");
    vi.stubEnv("EXPO_PUBLIC_APP_URL", "https://app.mobile.test");

    await expect(importEnv()).rejects.toThrow(/realtimeUrl/);
  });
});
