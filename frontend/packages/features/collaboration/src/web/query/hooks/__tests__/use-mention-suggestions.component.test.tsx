import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@notrelix/testing";
import type { CollaborationApiClient } from "../../../../core/api/comment.service";
import { createUseMentionSuggestions } from "../use-mention-suggestions";

const endpoints = {
  pages: {
    mentionSuggestions: (id: string) => `/pages/${id}/mention-suggestions`,
  },
  boardItems: {
    mentionSuggestions: (id: string) =>
      `/board-items/${id}/mention-suggestions`,
  },
};
const AN = { userId: "u-an", name: "Ân", avatarUrl: null };
const BINH = { userId: "u-binh", name: "Bình", avatarUrl: null };

function setup(get: (url: string) => Promise<unknown>) {
  const getMock = vi.fn(get);
  const api = {
    get: getMock as unknown as CollaborationApiClient["get"],
    post: vi.fn(),
    delete: vi.fn(),
  };
  const useSuggestions = createUseMentionSuggestions(api, endpoints);
  const client = new QueryClient();
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  const view = renderHook(
    (props: { kind: "page" | "item"; id: string; query: string | null }) =>
      useSuggestions("ws-1", props.kind, props.id, props.query),
    { wrapper, initialProps: { kind: "item", id: "i-1", query: null } },
  );
  return { api: { get: getMock }, view };
}

// Past the debounce, then past the query cache's own notify tick.
const settle = async () => {
  await act(() => vi.advanceTimersByTimeAsync(200));
  await act(() => vi.advanceTimersByTimeAsync(20));
};

describe("useMentionSuggestions", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("reads nothing while no mention is open", async () => {
    const { api } = setup(async () => []);
    await settle();
    expect(api.get).not.toHaveBeenCalled();
  });

  it("asks the item route with the typed text and returns what the server said", async () => {
    const { api, view } = setup(async () => [AN]);
    view.rerender({ kind: "item", id: "i-1", query: "" });
    expect(view.result.current.status).toBe("loading");
    await settle();
    expect(api.get).toHaveBeenCalledWith(
      "/board-items/i-1/mention-suggestions?query=",
    );
    expect(view.result.current).toMatchObject({
      status: "success",
      people: [AN],
    });
  });

  it("uses the page route for a page", async () => {
    const { api, view } = setup(async () => []);
    view.rerender({ kind: "page", id: "p-1", query: "a" });
    await settle();
    expect(api.get).toHaveBeenCalledWith(
      "/pages/p-1/mention-suggestions?query=a",
    );
    expect(view.result.current).toMatchObject({
      status: "success",
      people: [],
    });
  });

  it("waits for typing to pause, keeps the previous people meanwhile, then refetches with the query", async () => {
    const { api, view } = setup(async (url) =>
      url.endsWith("query=b") ? [BINH] : [AN],
    );
    view.rerender({ kind: "item", id: "i-1", query: "" });
    await settle();
    view.rerender({ kind: "item", id: "i-1", query: "b" });
    await act(() => vi.advanceTimersByTimeAsync(50));
    expect(api.get).toHaveBeenCalledTimes(1);
    expect(view.result.current.people).toEqual([AN]);
    await settle();
    expect(api.get).toHaveBeenLastCalledWith(
      "/board-items/i-1/mention-suggestions?query=b",
    );
    expect(view.result.current.people).toEqual([BINH]);
  });

  it("ignores a late answer for an earlier query", async () => {
    const late: Record<string, (value: unknown) => void> = {};
    const { view } = setup(
      (url) =>
        new Promise((resolve) => (late[url.split("query=")[1]!] = resolve)),
    );
    view.rerender({ kind: "item", id: "i-1", query: "a" });
    await settle();
    view.rerender({ kind: "item", id: "i-1", query: "b" });
    await settle();
    await act(async () => late["b"]!([BINH]));
    await settle();
    await act(async () => late["a"]!([AN]));
    await settle();
    expect(view.result.current.people).toEqual([BINH]);
  });

  it("never shows people of another target while the next one loads", async () => {
    const { view } = setup(async (url) =>
      url.includes("/i-1/") ? [AN] : new Promise(() => undefined),
    );
    view.rerender({ kind: "item", id: "i-1", query: "" });
    await settle();
    expect(view.result.current.people).toEqual([AN]);
    view.rerender({ kind: "item", id: "i-2", query: "" });
    await settle();
    expect(view.result.current).toMatchObject({
      status: "loading",
      people: [],
    });
  });

  it("reports a failed read and retries it", async () => {
    let fail = true;
    const { view } = setup(async () => {
      if (fail) throw new Error("boom");
      return [AN];
    });
    view.rerender({ kind: "item", id: "i-1", query: "" });
    await settle();
    expect(view.result.current.status).toBe("error");
    fail = false;
    await act(async () => view.result.current.retry());
    await settle();
    expect(view.result.current).toMatchObject({
      status: "success",
      people: [AN],
    });
  });
});
