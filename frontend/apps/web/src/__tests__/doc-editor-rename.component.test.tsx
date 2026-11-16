import { act } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import {
  createUseDocEditor,
  createUsePage,
  type DocsApiClient,
  type PageApiEndpoints,
} from "@notrelix/docs-state";

const endpoints = {
  pages: {
    detail: (pageId: string) => `/pages/${pageId}`,
    breadcrumb: (pageId: string) => `/pages/${pageId}/breadcrumb`,
    list: (workspaceId: string) => `/workspaces/${workspaceId}/pages`,
  },
  blocks: {},
} as unknown as PageApiEndpoints;

/** A producer whose page version moves on every rename and refuses a stale one with 412. */
function producer() {
  const page = { title: "Start", version: 1 };
  const patch = vi.fn(async (_url: string, body: unknown) => {
    const sent = body as { title?: string; expectedVersion: number };
    if (sent.expectedVersion !== page.version)
      throw Object.assign(new Error("stale"), { status: 412 });
    page.title = sent.title ?? page.title;
    page.version += 1;
  });
  const get = vi.fn(async (url: string) => {
    // Reads take a moment, as they do over the network.
    await new Promise((resolve) => setTimeout(resolve, 30));
    return url.endsWith("/breadcrumb")
      ? []
      : {
          id: "p1",
          workspaceId: "ws",
          title: page.title,
          version: page.version,
          createdAt: "2026-10-01T00:00:00.000Z",
        };
  });
  const api = { get, patch, post: vi.fn(), delete: vi.fn() };
  return { api: api as unknown as DocsApiClient, patch };
}

async function until(assertion: () => void) {
  for (let attempt = 0; ; attempt += 1) {
    try {
      assertion();
      return;
    } catch (error) {
      if (attempt >= 100) throw error;
      await act(async () => {
        await new Promise((resolve) => setTimeout(resolve, 20));
      });
    }
  }
}

describe("renaming a page twice in a row", () => {
  it("sends the second rename against the version the first one produced", async () => {
    const { api, patch } = producer();
    const usePage = createUsePage(api, endpoints);
    const useDocEditor = createUseDocEditor(api, endpoints);
    const seen: {
      page?: { title: string; version?: number | null };
      editor?: ReturnType<typeof useDocEditor>;
    } = {};
    function Probe() {
      seen.page = usePage("ws", "p1").data;
      seen.editor = useDocEditor("ws", "p1");
      return null;
    }
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const mount = document.createElement("div");
    const root = createRoot(mount);
    await act(async () => {
      root.render(
        <QueryClientProvider client={queryClient}>
          <Probe />
        </QueryClientProvider>,
      );
    });
    await until(() => expect(seen.page?.version).toBe(1));

    act(() => seen.editor?.updateTitle("First"));
    await until(() => expect(patch).toHaveBeenCalledTimes(1));
    await until(() => expect(seen.editor?.saveState).toBe("saved"));
    // The page read the rename caused is still on its way.
    act(() => seen.editor?.updateTitle("Second"));

    await until(() => expect(seen.page?.title).toBe("Second"));
    expect(seen.editor?.titleConflict).toBe(false);
    expect(patch).toHaveBeenLastCalledWith(expect.any(String), {
      title: "Second",
      expectedVersion: 2,
    });
    await act(async () => root.unmount());
  });
});
