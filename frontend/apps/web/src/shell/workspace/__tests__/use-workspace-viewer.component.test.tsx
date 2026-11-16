import { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  focusManager,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";

const h = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock("@notrelix/features-auth", () => ({
  useCurrentUser: () => ({ id: "u1" }),
}));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: { get: h.get } } }),
}));
vi.mock("@notrelix/features-workspace/web", () => ({
  createUseMyWorkspaceMembership: () => () => ({ data: { role: "member" } }),
  createUseWorkspaceMembers: () => () => ({ data: [] }),
}));

import { handleWorkspaceRecovery } from "../../../realtime/workspace-recovery-policy";
import { useWorkspaceViewer } from "../use-workspace-viewer";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const ACCESS = "/workspaces/ws/members/me/access";
const accessCalls = () => h.get.mock.calls.filter(([url]) => url === ACCESS);
const tick = (ms: number) =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });

describe("workspace access check refetch", () => {
  let queryClient: QueryClient;
  let unmount: () => void;

  beforeEach(async () => {
    vi.useFakeTimers();
    h.get.mockReset().mockResolvedValue({ removedActions: [] });
    queryClient = new QueryClient();
    const host = document.createElement("div");
    const root = createRoot(host);
    function Probe() {
      useWorkspaceViewer("ws");
      return null;
    }
    await act(async () => {
      root.render(
        <QueryClientProvider client={queryClient}>
          <Probe />
        </QueryClientProvider>,
      );
    });
    await tick(0);
    unmount = () => act(() => root.unmount());
  });

  afterEach(() => {
    unmount();
    focusManager.setFocused(undefined);
    vi.useRealTimers();
  });

  it("refetches at most once for 10 window focuses within 30 s", async () => {
    expect(accessCalls()).toHaveLength(1);
    for (let i = 0; i < 10; i += 1) {
      focusManager.setFocused(false);
      focusManager.setFocused(true);
      await tick(3_000);
    }
    expect(accessCalls().length - 1).toBeLessThanOrEqual(1);
  });

  it("does not poll while the document is hidden", async () => {
    focusManager.setFocused(false);
    await tick(180_000);
    expect(accessCalls()).toHaveLength(1);
  });

  it("an access-changed recovery refetches at once", async () => {
    await handleWorkspaceRecovery({
      workspaceId: "ws",
      accountId: "a1",
      invalidateQueries: (keys) =>
        Promise.all(
          keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
        ).then(() => undefined),
    });
    await tick(0);
    expect(accessCalls()).toHaveLength(2);
  });
});
