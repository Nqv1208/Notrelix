import { describe, expect, test, vi } from "vitest";
import {
  initObservability,
  trackEvent,
  reportError,
  getObservabilityConfig,
  RecordingTelemetryAdapter,
  redactTelemetryProperties,
} from "../index";

describe("Observability", () => {
  test("initializes with custom configuration", () => {
    initObservability({ enabled: true, environment: "staging" });
    const config = getObservabilityConfig();
    expect(config.environment).toBe("staging");
    expect(config.enabled).toBe(true);
  });

  test("does not report when disabled", () => {
    initObservability({ enabled: false });
    const consoleSpy = vi.spyOn(console, "log").mockImplementation(() => {});

    trackEvent("user_login", { method: "oauth" });
    expect(consoleSpy).not.toHaveBeenCalled();

    consoleSpy.mockRestore();
  });

  test("redacts sensitive telemetry properties", () => {
    expect(
      redactTelemetryProperties({
        email: "person@example.com",
        accessToken: "secret",
        workspaceId: "workspace-1",
      }),
    ).toEqual({
      email: "[redacted]",
      accessToken: "[redacted]",
      workspaceId: "workspace-1",
    });
  });

  test("records events and merges context immutably", () => {
    const root = new RecordingTelemetryAdapter({ releaseSha: "abc" });
    const scoped = root.withContext({ workspaceId: "workspace-1" });

    scoped.track("route.navigation", { route: "/workspaces/a" });
    scoped.reportError(new Error("boom"), { token: "secret" });

    expect(root.events).toHaveLength(0);
    expect((scoped as RecordingTelemetryAdapter).events[0]).toMatchObject({
      name: "route.navigation",
      context: {
        releaseSha: "abc",
        workspaceId: "workspace-1",
      },
    });
    expect(
      (scoped as RecordingTelemetryAdapter).errors[0]?.context,
    ).toMatchObject({
      token: "[redacted]",
    });
  });
});
