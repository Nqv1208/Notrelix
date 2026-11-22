import { describe, it, expect } from "vitest";
import { readWebRuntimeEnvironment } from "../config/read-runtime-environment";
import { parseEnv } from "@notrelix/kernel";

describe("readWebRuntimeEnvironment & parseEnv resilient fallback", () => {
  it("normalizes valid Vite environment input", () => {
    const rawEnv = {
      MODE: "production",
      VITE_API_URL: "https://api.example.com",
      VITE_WS_URL: "wss://realtime.example.com",
      VITE_APP_URL: "https://app.example.com",
      VITE_RELEASE_SHA: "abc1234",
      VITE_MOCK_API: "false",
    } as unknown as ImportMetaEnv;

    const input = readWebRuntimeEnvironment(rawEnv);
    const resolved = parseEnv(input);

    expect(resolved.mode).toBe("production");
    expect(resolved.apiUrl).toBe("https://api.example.com");
    expect(resolved.realtimeUrl).toBe("wss://realtime.example.com");
    expect(resolved.isProduction).toBe(true);
  });

  it("falls back safely to default local environment when external inputs are missing or invalid", () => {
    const rawEnv = {
      MODE: "invalid_mode",
      VITE_API_URL: "not-a-valid-url",
      VITE_WS_URL: undefined,
    } as unknown as ImportMetaEnv;

    const input = readWebRuntimeEnvironment(rawEnv);
    const resolved = parseEnv(input);

    expect(resolved.mode).toBe("development");
    expect(resolved.apiUrl).toBe("http://localhost:5000");
    expect(resolved.realtimeUrl).toBe("ws://localhost:5000/realtime");
    expect(resolved.isDevelopment).toBe(true);
  });

  it("derives same-origin URLs at runtime when no VITE_* value is set", () => {
    const raw = { MODE: "production" } as unknown as ImportMetaEnv;

    const https = parseEnv(
      readWebRuntimeEnvironment(raw, "https://demo.notrelix.me"),
    );
    expect(https.apiUrl).toBe("https://demo.notrelix.me/api/v1");
    expect(https.realtimeUrl).toBe("wss://demo.notrelix.me");
    expect(https.appUrl).toBe("https://demo.notrelix.me");

    const http = parseEnv(
      readWebRuntimeEnvironment(raw, "http://localhost:5173"),
    );
    expect(http.apiUrl).toBe("http://localhost:5173/api/v1");
    expect(http.realtimeUrl).toBe("ws://localhost:5173");
  });

  it("lets an explicit VITE_* value win over the origin", () => {
    const raw = {
      MODE: "production",
      VITE_API_URL: "https://api.other.test/api/v1",
      VITE_WS_URL: "wss://rt.other.test",
      VITE_APP_URL: "https://app.other.test",
    } as unknown as ImportMetaEnv;

    const input = readWebRuntimeEnvironment(raw, "https://demo.notrelix.me");
    expect(input.apiUrl).toBe("https://api.other.test/api/v1");
    expect(input.realtimeUrl).toBe("wss://rt.other.test");
    expect(input.appUrl).toBe("https://app.other.test");
  });
});
