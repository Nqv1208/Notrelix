import { describe, expect, it, vi } from "vitest";
import type React from "react";

const mocks = vi.hoisted(() => ({
  cleanup: undefined as undefined | (() => void),
  dispose: vi.fn(),
}));

vi.mock("react", async () => {
  const actual = await vi.importActual<typeof import("react")>("react");
  return {
    ...actual,
    default: actual,
    useMemo: <T,>(factory: () => T) => factory(),
    useEffect: (effect: () => void | (() => void)) => {
      const cleanup = effect();
      mocks.cleanup = typeof cleanup === "function" ? cleanup : undefined;
    },
  };
});

vi.mock("@tanstack/react-query", () => ({
  QueryClientProvider: ({ children }: { children: React.ReactNode }) =>
    children,
}));

vi.mock("@notrelix/runtime-mobile", () => ({
  createMobileRuntime: vi.fn(() => ({ runtime: true })),
  createMobileApplicationServices: vi.fn(() => ({
    runtime: { runtime: true },
    queryClient: { queryClient: true },
    dispose: mocks.dispose,
  })),
  MobileApplicationServicesProvider: ({
    children,
  }: {
    children: React.ReactNode;
  }) => children,
  MobileRuntimeProvider: ({ children }: { children: React.ReactNode }) =>
    children,
}));

vi.mock("../../config/env", () => ({
  env: {
    mode: "test",
    apiUrl: "https://api.mobile.test",
    realtimeUrl: "wss://realtime.mobile.test",
    appUrl: "https://app.mobile.test",
    releaseSha: "test-sha",
  },
}));

describe("MobileAppProviders", () => {
  it("disposes application services on unmount", async () => {
    const { MobileAppProviders } = await import("../mobile-app-providers");

    MobileAppProviders({ children: null });
    mocks.cleanup?.();

    expect(mocks.dispose).toHaveBeenCalledTimes(1);
  });
});
