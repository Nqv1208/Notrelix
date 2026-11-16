import { act } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { createDocPageScreen, docPageCopy } from "@notrelix/docs-web";
import { webCatalogs } from "../i18n/catalogs";

type Captured = {
  surface?: {
    status?: string;
    callbacks?: Record<string, (...args: unknown[]) => unknown>;
    accessLost?: { text: string };
  };
  rich?: {
    initial: { id: string; content: { runs: unknown[] } }[];
    onDrafts: (d: unknown[]) => void;
  };
};
const captured = vi.hoisted(() => ({}) as Captured);

vi.mock(
  "../../../../packages/product/docs/web/src/components/doc-page-screen-surface",
  () => ({
    DocPageScreenSurface: (props: Captured["surface"]) => {
      captured.surface = props;
      return null;
    },
  }),
);
vi.mock(
  "../../../../packages/product/docs/web/src/editor/use-doc-rich-editor",
  () => ({
    useDocRichEditor: (input: Captured["rich"]) => {
      captured.rich = input;
      return {
        editor: {},
        completeUpload: () => undefined,
        applyDrafts: () => undefined,
        currentDrafts: () => [],
        updateBlockAttrs: () => undefined,
      };
    },
  }),
);
vi.mock(
  "../../../../packages/product/docs/web/src/editor/use-comment-anchors",
  () => ({
    revealComment: () => undefined,
    useDetachedComments: () => [],
    useShowCommentAnchors: () => undefined,
  }),
);
vi.mock("../../../../packages/product/docs/web/src/editor/outline", () => ({
  revealHeading: () => false,
  useDocSummary: () => null,
}));
vi.mock(
  "../../../../packages/product/docs/web/src/editor/comment-anchors",
  () => ({
    PENDING_ID: "pending",
    anchorFromSelection: () => ({
      blockId: "b1",
      from: 0,
      to: 4,
      quote: "Xin",
    }),
  }),
);
vi.mock(
  "../../../../packages/product/docs/web/src/editor/editor-toolbar",
  () => ({
    OPEN_LINK_EVENT: "open-link",
    editorDom: () => null,
  }),
);

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
    duplicate: (w: string, p: string) =>
      `/workspaces/${w}/pages/${p}/duplicate`,
    trash: (w: string) => `/workspaces/${w}/pages/trash`,
  },
  pageAttachments: {
    list: (p: string) => `/pages/${p}/attachments`,
    files: (p: string) => `/pages/${p}/attachments/files`,
  },
  blocks: { batch: (p: string) => `/pages/${p}/blocks/batch` },
} as never;

type Host = ReturnType<Parameters<typeof createDocPageScreen>[0]["useHost"]>;

function setup(options: { pageError?: { status?: number } } = {}) {
  const writes = { batchFail: null as number | null, posts: [] as string[] };
  const notify = vi.fn();
  const api = {
    get: vi.fn(async (url: string) => {
      if (url === "/pages/p1" && options.pageError)
        throw Object.assign(new Error("page"), options.pageError);
      if (url.endsWith("/blocks"))
        return {
          items: [
            {
              id: "b1",
              pageId: "p1",
              type: "Text",
              content: {},
              properties: { text: "Xin chào" },
              position: "a",
              version: 1,
              createdAt: "2026-10-01T00:00:00.000Z",
            },
          ],
        };
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
    post: vi.fn(async (url: string, body: unknown) => {
      writes.posts.push(url);
      if (url.endsWith("/blocks/batch")) {
        if (writes.batchFail !== null)
          throw Object.assign(new Error("batch"), { status: writes.batchFail });
        return (body as { blocks: { id: string }[] }).blocks.map((b) => ({
          id: b.id,
          version: 2,
        }));
      }
      if (url.endsWith("/duplicate"))
        return { pageId: "p2", droppedBlockCount: 0, title: "Bản sao" };
      if (url.endsWith("/comments")) return "c1";
      return { id: "v1", name: "v1", createdAt: "2026-11-16T00:00:00.000Z" };
    }),
    patch: vi.fn(),
    delete: vi.fn(),
  };
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
    notify,
    now: new Date("2026-11-16T00:00:00Z"),
  } as unknown as Host;
  const Screen = createDocPageScreen({
    api: api as never,
    endpoints,
    useHost: () => host,
  });
  const mount = document.createElement("div");
  document.body.appendChild(mount);
  const root = createRoot(mount);
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const render = () =>
    act(async () => {
      root.render(
        <QueryClientProvider client={queryClient}>
          <I18nProvider catalogs={webCatalogs} initialLocale="vi">
            <Screen workspaceId="ws" pageId="p1" />
          </I18nProvider>
        </QueryClientProvider>,
      );
    });
  const unmount = () => act(async () => root.unmount());
  return { api, writes, notify, render, unmount, mount };
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

/** Opens the page and types into the first block while the save is refused. */
async function openWithUnsavedEdit(
  page: ReturnType<typeof setup>,
  failWith: number,
) {
  await page.render();
  await until(() => expect(captured.rich?.initial.length).toBe(1));
  await until(() => expect(captured.surface?.callbacks).toBeTruthy());
  page.writes.batchFail = failWith;
  const first = (captured.rich as NonNullable<Captured["rich"]>).initial[0];
  await act(async () => {
    captured.rich?.onDrafts([
      {
        ...first,
        content: { ...first.content, runs: [{ text: "đã sửa", marks: [] }] },
      },
    ]);
  });
}

const copy = docPageCopy("vi");
const writesTo = (page: ReturnType<typeof setup>, tail: string) =>
  page.writes.posts.filter((url) => url.endsWith(tail)).length;

afterEach(() => {
  captured.surface = undefined;
  captured.rich = undefined;
});

describe("actions that read the stored page wait for the save", () => {
  it("does not save a version that misses the unsaved edit, and saves it after a retry", async () => {
    const page = setup();
    await openWithUnsavedEdit(page, 500);
    await act(async () => {
      captured.surface?.callbacks?.onSaveVersion?.("Bản 1");
    });
    await until(() => expect(page.notify).toHaveBeenCalled());

    expect(writesTo(page, "/versions")).toBe(0);
    expect(page.notify.mock.calls[0]?.[0]).toBe(copy.header.saveBlocked);

    page.writes.batchFail = null;
    await act(async () => {
      page.notify.mock.calls[0]?.[1].run();
    });
    await until(() => expect(writesTo(page, "/versions")).toBe(1));
    await page.unmount();
  });

  it("does not duplicate a page that misses the unsaved edit", async () => {
    const page = setup();
    await openWithUnsavedEdit(page, 500);
    await act(async () => {
      captured.surface?.callbacks?.onDuplicatePage?.();
    });
    await until(() =>
      expect(page.notify.mock.calls.map((c) => c[0])).toContain(
        copy.header.saveBlocked,
      ),
    );

    expect(writesTo(page, "/duplicate")).toBe(0);
    await page.unmount();
  });

  it("does not post an anchored comment before the block it names is stored", async () => {
    const page = setup();
    await openWithUnsavedEdit(page, 500);
    await act(async () => {
      captured.surface?.callbacks?.onCommentStart?.();
    });
    await act(async () => {
      captured.surface?.callbacks?.onCommentCreate?.("Ý kiến", undefined);
    });
    await until(() =>
      expect(page.notify.mock.calls.map((c) => c[0])).toContain(
        copy.header.saveBlocked,
      ),
    );

    expect(writesTo(page, "/comments")).toBe(0);
    await page.unmount();
  });

  it("still writes the pending edit when the page is left, and hands over text when access is lost", async () => {
    const left = setup();
    await openWithUnsavedEdit(left, 500);
    left.writes.batchFail = null;
    await left.unmount();
    await until(() =>
      expect(writesTo(left, "/blocks/batch")).toBeGreaterThanOrEqual(1),
    );

    const lost = setup();
    await openWithUnsavedEdit(lost, 403);
    await until(() =>
      expect(captured.surface?.accessLost?.text).toContain("đã sửa"),
    );
    await lost.unmount();
  });
});

describe("a page detail that cannot be read", () => {
  it("shows a load error with a retry on a 500", async () => {
    const page = setup({ pageError: { status: 500 } });
    await page.render();
    await until(() => expect(captured.surface?.status).not.toBe("loading"));
    expect(captured.surface).toMatchObject({ status: "loadFailed" });
    await page.unmount();
  });

  it("shows a load error with a retry on a network failure", async () => {
    const page = setup({ pageError: {} });
    await page.render();
    await until(() => expect(captured.surface?.status).not.toBe("loading"));
    expect(captured.surface).toMatchObject({ status: "loadFailed" });
    await page.unmount();
  });

  it.each([404, 403])("keeps the not-found screen on a %i", async (status) => {
    const page = setup({ pageError: { status } });
    await page.render();
    await until(() => expect(captured.surface?.status).not.toBe("loading"));
    expect(captured.surface).toMatchObject({ status: "notFound" });
    await page.unmount();
  });
});
