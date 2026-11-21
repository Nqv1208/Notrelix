import { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const h = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: { get: h.get } } }),
}));

import { useCommandSearch } from "../use-command-search";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

type Result = ReturnType<typeof useCommandSearch>;

describe("useCommandSearch status", () => {
  let last: Result;
  let unmount: () => void;

  async function mount(typed: string) {
    const host = document.createElement("div");
    const root = createRoot(host);
    function Probe() {
      last = useCommandSearch("ws", typed, true);
      return null;
    }
    await act(async () => {
      root.render(
        <QueryClientProvider
          client={
            new QueryClient({ defaultOptions: { queries: { gcTime: 0 } } })
          }
        >
          <Probe />
        </QueryClientProvider>,
      );
    });
    unmount = () => act(() => root.unmount());
  }
  const tick = (ms: number) =>
    act(async () => {
      await vi.advanceTimersByTimeAsync(ms);
    });

  beforeEach(() => {
    vi.useFakeTimers();
    h.get.mockReset();
  });
  afterEach(() => {
    unmount();
    vi.useRealTimers();
  });

  it("is loading while the search is in flight, then ready with the rows", async () => {
    let resolve: (value: unknown) => void = () => {};
    h.get.mockReturnValue(new Promise((r) => (resolve = r)));
    await mount("hoá đơn");
    await tick(200);
    expect(last.status).toBe("loading");
    await act(async () => resolve({ work: [], docs: [], boards: [] }));
    await tick(0);
    expect(last.status).toBe("ready");
    expect(last.data).toEqual({ work: [], docs: [], boards: [] });
  });

  it("is error, not an empty answer, when the search fails, and retry asks again", async () => {
    h.get.mockRejectedValue(new Error("boom"));
    await mount("hoá đơn");
    await tick(200);
    await tick(50);
    expect(last.status).toBe("error");
    expect(last.data).toBeNull();
    const calls = h.get.mock.calls.length;
    h.get.mockResolvedValue({ work: [], docs: [], boards: [] });
    await act(async () => last.retry());
    await tick(0);
    expect(h.get.mock.calls.length).toBe(calls + 1);
    expect(last.status).toBe("ready");
  });

  it("is ready and asks nothing for a short word or a command prefix", async () => {
    await mount(">go");
    await tick(200);
    expect(last.status).toBe("ready");
    expect(h.get).not.toHaveBeenCalled();
  });
});
