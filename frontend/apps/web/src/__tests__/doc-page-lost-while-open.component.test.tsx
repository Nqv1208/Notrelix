import { act } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { createDocPageScreen, docPageCopy } from "@notrelix/docs-web";
import type { RealtimeRecoverySignal } from "@notrelix/realtime";
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

function fakeRealtime() {
  let recovery: ((signal: RealtimeRecoverySignal) => void) | null = null;
  const stopPresence = vi.fn();
  const stopEvents = vi.fn();
  const realtime = {
    subscribe: vi.fn(() => stopEvents),
    subscribePagePresence: vi.fn(() => stopPresence),
    subscribeRecovery: vi.fn((listener) => {
      recovery = listener;
      return () => {
        recovery = null;
      };
    }),
    subscribeState: vi.fn(() => () => undefined),
    getState: vi.fn(() => "connected"),
    sendPresence: vi.fn(),
  };
  return {
    realtime,
    stopPresence,
    stopEvents,
    revoke: () =>
      recovery?.({
        reason: "permission-lost",
        workspaceId: "ws",
        scope: { kind: "page", id: "p1" },
        subscriptionId: "page:p1",
        code: "subscription_revoked",
      }),
  };
}

async function waitFor(mount: HTMLElement, text: string) {
  for (let attempt = 0; attempt < 80; attempt += 1) {
    if ((mount.textContent ?? "").includes(text)) return;
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 25));
    });
  }
}

describe("the document page when the person loses it while it is open (UC-DCT-I07, UC-COL-E05)", () => {
  it("rereads on the revoked page stream, shows the no-access state instead of the stored copy, and stops live updates", async () => {
    let revoked = false;
    const api = {
      get: vi.fn(async (url: string) => {
        if (
          revoked &&
          (url === "/pages/p1" || url.startsWith("/pages/p1/blocks"))
        )
          throw Object.assign(new Error("gone"), { status: 404 });
        if (url.endsWith("/breadcrumb")) return [];
        if (url === "/pages/p1")
          return {
            id: "p1",
            workspaceId: "ws",
            title: "Kế hoạch bí mật",
            version: 3,
            createdAt: "2026-10-01T00:00:00.000Z",
            canEdit: true,
          };
        if (url.startsWith("/pages/p1/blocks"))
          return { items: [], nextCursor: null };
        return [];
      }),
      post: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    const live = fakeRealtime();
    const host = {
      space: null,
      personFor: (id: string) => ({
        id,
        name: id,
        initials: "U",
        color: "#000",
      }),
      tasks: new Map(),
      tasksReady: true,
      board: null,
      suggestTasks: () => [],
      share: null,
      viewerReason: null,
      canEdit: true,
      now: new Date("2026-11-16T00:00:00Z"),
      realtime: live.realtime,
    } as unknown as DocPageHost;
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
    await waitFor(mount, "Kế hoạch bí mật");
    expect(mount.textContent).toContain("Kế hoạch bí mật");
    expect(live.realtime.subscribePagePresence).toHaveBeenCalled();

    revoked = true;
    await act(async () => live.revoke());
    const notFound = docPageCopy("vi").notFoundTitle;
    await waitFor(mount, notFound);

    expect(mount.textContent).toContain(notFound);
    expect(mount.textContent).not.toContain("Kế hoạch bí mật");
    expect(live.stopPresence).toHaveBeenCalled();
    expect(live.stopEvents).toHaveBeenCalled();
    await act(async () => root.unmount());
    mount.remove();
  });
});
