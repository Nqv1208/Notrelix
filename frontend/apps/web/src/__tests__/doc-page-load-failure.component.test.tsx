import { act } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { createDocPageScreen, docPageCopy } from "@notrelix/docs-web";
import { webCatalogs } from "../i18n/catalogs";

type DocPageHost = ReturnType<
  Parameters<typeof createDocPageScreen>[0]["useHost"]
>;

const endpoints = {
  pages: {
    list: (w: string) => `/workspaces/${w}/pages`,
    detail: (p: string) => `/pages/${p}`,
    breadcrumb: (p: string) => `/pages/${p}/breadcrumb`,
    blocks: (p: string) => `/pages/${p}/blocks`,
    comments: (p: string) => `/pages/${p}/comments`,
    commentsPage: (p: string) => `/pages/${p}/comments/page`,
    history: (p: string) => `/pages/${p}/history`,
    activity: (p: string) => `/pages/${p}/activity`,
    backlinks: (p: string) => `/pages/${p}/backlinks`,
    versions: (p: string) => `/pages/${p}/versions`,
    trash: (w: string) => `/workspaces/${w}/pages/trash`,
  },
  pageAttachments: {
    list: (p: string) => `/pages/${p}/attachments`,
    files: (p: string) => `/pages/${p}/attachments/files`,
  },
  blocks: { batch: (p: string) => `/pages/${p}/blocks/batch` },
} as never;

const host = {
  space: null,
  personFor: (id: string) => ({ id, name: id, initials: "U", color: "#000" }),
  tasks: new Map(),
  tasksReady: true,
  board: null,
  suggestTasks: () => [],
  share: null,
  viewerReason: null,
  canEdit: true,
  now: new Date("2026-11-16T00:00:00Z"),
} as unknown as DocPageHost;

describe("the document page when its blocks cannot be read", () => {
  it("says the document did not open instead of showing an empty page to type in", async () => {
    const api = {
      get: vi.fn(async (url: string) => {
        if (url.endsWith("/blocks") || url.includes("/blocks?"))
          throw Object.assign(new Error("server"), { status: 500 });
        if (url.endsWith("/breadcrumb")) return [];
        if (url === "/pages/p1")
          return {
            id: "p1",
            workspaceId: "ws",
            title: "Kế hoạch",
            version: 3,
            createdAt: "2026-10-01T00:00:00.000Z",
            canEdit: true,
          };
        return [];
      }),
      post: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const Screen = createDocPageScreen({
      api: api as never,
      endpoints,
      useHost: () => host,
    });
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const mount = document.createElement("div");
    document.body.appendChild(mount);
    const root = createRoot(mount);
    await act(async () => {
      root.render(
        <QueryClientProvider client={queryClient}>
          <I18nProvider catalogs={webCatalogs} initialLocale="vi">
            <Screen workspaceId="ws" pageId="p1" />
          </I18nProvider>
        </QueryClientProvider>,
      );
    });
    const title = docPageCopy("vi").notFoundTitle;
    for (let attempt = 0; attempt < 60; attempt += 1) {
      if ((mount.textContent ?? "").includes(title)) break;
      await act(async () => {
        await new Promise((resolve) => setTimeout(resolve, 25));
      });
    }

    expect(mount.textContent).toContain(title);
    expect(mount.querySelector('[contenteditable="true"]')).toBeNull();
    await act(async () => root.unmount());
    mount.remove();
  });
});
