import { act } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import {
  createUseDocEditor,
  type DocsApiClient,
  type PageApiEndpoints,
} from "@notrelix/docs-state";

const endpoints = {
  pages: {
    detail: (pageId: string) => `/pages/${pageId}`,
    breadcrumb: (pageId: string) => `/pages/${pageId}/breadcrumb`,
    list: (workspaceId: string) => `/workspaces/${workspaceId}/pages`,
    blocks: (pageId: string) => `/pages/${pageId}/blocks`,
  },
  blocks: {
    detail: (pageId: string, blockId: string) =>
      `/pages/${pageId}/blocks/${blockId}`,
  },
} as unknown as PageApiEndpoints;

const blockDto = (id: string) => ({
  id,
  pageId: "p1",
  type: "Text",
  content: {},
  properties: { text: id },
  position: id,
  version: 1,
  createdAt: "2026-10-01T00:00:00.000Z",
});
const later = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));
const refused = () => Object.assign(new Error("server"), { status: 500 });

/** A producer where the first block's write fails slowly and the second one's succeeds at once. */
function producer() {
  return {
    get: vi.fn(async (url: string) => {
      if (url.endsWith("/breadcrumb")) return [];
      if (url.endsWith("/blocks"))
        return { items: [blockDto("b1"), blockDto("b2")] };
      return {
        id: "p1",
        workspaceId: "ws",
        title: "T",
        version: 1,
        createdAt: "2026-10-01T00:00:00.000Z",
      };
    }),
    patch: vi.fn(async (url: string, body: { title?: string }) => {
      if (url.endsWith("/blocks/b1") || body.title === "A") {
        await later(80);
        throw refused();
      }
      return url.includes("/blocks/") ? blockDto("b2") : undefined;
    }),
    delete: vi.fn(async (url: string) => {
      if (url.includes("/blocks/b1")) {
        await later(80);
        throw refused();
      }
    }),
    post: vi.fn(),
  };
}

async function mountEditor() {
  const api = producer();
  const useDocEditor = createUseDocEditor(
    api as unknown as DocsApiClient,
    endpoints,
  );
  const seen: { editor?: ReturnType<typeof useDocEditor> } = {};
  function Probe() {
    seen.editor = useDocEditor("ws", "p1");
    return null;
  }
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const root = createRoot(document.createElement("div"));
  await act(async () => {
    root.render(
      <QueryClientProvider client={queryClient}>
        <Probe />
      </QueryClientProvider>,
    );
  });
  const settle = () =>
    act(async () => {
      await later(250);
    });
  return { api, seen, settle, unmount: () => act(async () => root.unmount()) };
}

describe("two writes of the page in flight at once keep their own failure", () => {
  it("shows the save error when the first block edit fails after the second succeeded", async () => {
    const page = await mountEditor();
    await act(async () => {
      page.seen.editor?.updateBlock("b1", { properties: { text: "one" } });
      page.seen.editor?.updateBlock("b2", { properties: { text: "two" } });
    });
    await page.settle();

    expect(page.seen.editor?.saveState).toBe("error");
    await page.unmount();
  });

  it("shows the save error when the first block removal fails after the second succeeded", async () => {
    const page = await mountEditor();
    await act(async () => {
      page.seen.editor?.deleteBlock("b1");
      page.seen.editor?.deleteBlock("b2");
    });
    await page.settle();

    expect(page.seen.editor?.saveState).toBe("error");
    await page.unmount();
  });

  it("shows the save error when the first rename fails after the second succeeded", async () => {
    const page = await mountEditor();
    await act(async () => {
      page.seen.editor?.updateTitle("A");
      page.seen.editor?.updateTitle("B");
    });
    await page.settle();

    expect(page.seen.editor?.saveState).toBe("error");
    await page.unmount();
  });
});
