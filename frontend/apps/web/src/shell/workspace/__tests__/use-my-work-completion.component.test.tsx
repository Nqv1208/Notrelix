import { act } from "react";
import { createRoot } from "react-dom/client";
import { describe, expect, it, vi } from "vitest";
import {
  QueryClient,
  QueryClientProvider,
  useMutation,
} from "@tanstack/react-query";

const h = vi.hoisted(() => ({
  runs: new Map<string, () => void>(),
  toasts: [] as string[],
}));

vi.mock("sonner", () => ({
  toast: {
    success: (m: string) => h.toasts.push(`ok:${m}`),
    error: (m: string) => h.toasts.push(`err:${m}`),
  },
}));
vi.mock("@notrelix/features-workspace/web", () => ({
  formatCopy: (text: string) => text,
  useWorkspaceHomeCopy: () => ({
    toasts: new Proxy({}, { get: (_t, key) => String(key) }),
  }),
}));
vi.mock("../work-management-bridge", () => ({
  useAttemptKey: () => () => "key",
  useCompleteItem: () =>
    useMutation({
      mutationFn: ({ item }: { item: { id: string } }) =>
        new Promise<void>((resolve) => h.runs.set(item.id, resolve)),
    }),
}));

import { useMyWorkCompletion } from "../use-my-work-completion";

const item = (id: string) => ({ id, name: id }) as never;

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

describe("My work: completing two items at once", () => {
  it("frees both items and reports both outcomes", async () => {
    const result: { current: ReturnType<typeof useMyWorkCompletion> } = {
      current: undefined as never,
    };
    function Probe() {
      result.current = useMyWorkCompletion("ws");
      return null;
    }
    const host = document.createElement("div");
    const root = createRoot(host);
    act(() =>
      root.render(
        <QueryClientProvider client={new QueryClient()}>
          <Probe />
        </QueryClientProvider>,
      ),
    );
    act(() => result.current.onComplete(item("a")));
    act(() => result.current.onComplete(item("b")));
    await act(async () => {
      await new Promise((r) => setTimeout(r, 0));
    });
    expect(h.runs.size).toBe(2);
    await act(async () => {
      h.runs.get("a")?.();
      h.runs.get("b")?.();
      await new Promise((r) => setTimeout(r, 0));
    });
    expect(
      [...result.current.completingIds],
      "an item is still marked as completing after it was completed",
    ).toEqual([]);
    expect(h.toasts.filter((t) => t.startsWith("ok:"))).toHaveLength(2);
    act(() => root.unmount());
  });
});
