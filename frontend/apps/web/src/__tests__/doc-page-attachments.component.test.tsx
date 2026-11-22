import { act } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { createDocPageScreen, docPageCopy } from "@notrelix/docs-web";
import { webCatalogs } from "../i18n/catalogs";

// The page's attachments at screen level (UC-COL-B08 to B10): the Files panel
// the screen hands the surface, the media host it hands the editor, and what
// each does against the API.

type Captured = {
  surface?: {
    files?: {
      status: string;
      entries: {
        id: string;
        name: string;
        kind: string;
        href: string | null;
      }[];
      canEdit: boolean;
      onRetry: () => void;
      onDownload: (id: string) => Promise<void>;
      onDelete: (id: string) => Promise<void>;
      onAddLink: (link: {
        url: string;
        name: string;
      }) => Promise<string | null>;
    };
  };
  rich?: {
    editable: boolean;
    context: {
      readOnly: boolean;
      media: {
        uploads: {
          add: (file: File) => { ok: boolean };
          retry: (id: string) => void;
          snapshot: () => { id: string; status: string; failure: string }[];
        };
        onRefused: (file: File, refusal: string) => void;
        download: (file: unknown) => Promise<void>;
        remove: (file: unknown) => Promise<void>;
      };
    };
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
    content: (p: string, id: string) => `/pages/${p}/attachments/${id}/content`,
    detail: (p: string, id: string) => `/pages/${p}/attachments/${id}`,
  },
  pageTemplates: { list: (w: string) => `/workspaces/${w}/page-templates` },
  blocks: { batch: (p: string) => `/pages/${p}/blocks/batch` },
} as never;

type Host = ReturnType<Parameters<typeof createDocPageScreen>[0]["useHost"]>;

const LIST = "/pages/p1/attachments";
const REPORT = {
  id: "a1",
  filename: "bao-cao.pdf",
  sizeBytes: 2048,
  contentType: "application/pdf",
  kind: "File",
  url: "",
  uploadedByName: "Lan",
  createdAt: "2026-11-10T01:00:00Z",
};
const LINK = {
  id: "a2",
  filename: "Hướng dẫn",
  sizeBytes: 0,
  contentType: "text/uri-list",
  kind: "Link",
  url: "https://example.com/guide",
  createdAt: "2026-11-11T01:00:00Z",
};

const failWith = (init: object) => Object.assign(new Error("api"), init);

function setup(
  options: {
    canEdit?: boolean;
    list?: () => unknown;
    post?: (url: string, body: unknown, request: unknown) => unknown;
    remove?: () => unknown;
    getBlob?: () => unknown;
    upload?: () => unknown;
  } = {},
) {
  const notify = vi.fn();
  const api = {
    get: vi.fn(async (url: string) => {
      if (url === LIST) return (options.list ?? (() => [LINK, REPORT]))();
      if (url.endsWith("/blocks")) return { items: [] };
      if (url.endsWith("/breadcrumb")) return [];
      if (url === "/pages/p1")
        return {
          id: "p1",
          workspaceId: "ws",
          title: "Kế hoạch",
          version: 3,
          createdAt: "2026-10-01T00:00:00.000Z",
          canEdit: options.canEdit ?? true,
        };
      return [];
    }),
    post: vi.fn(async (url: string, body: unknown, request: unknown) => {
      if (url === LIST) return options.post?.(url, body, request);
      return undefined;
    }),
    patch: vi.fn(),
    delete: vi.fn(async () => options.remove?.()),
    getBlob: vi.fn(async () => options.getBlob?.()),
    upload: vi.fn(async () => options.upload?.()),
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
    canEdit: options.canEdit ?? true,
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
  return {
    api,
    notify,
    render,
    unmount: () => act(async () => root.unmount()),
  };
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

const copy = docPageCopy("vi");
const listReads = (page: ReturnType<typeof setup>) =>
  page.api.get.mock.calls.filter(([url]) => url === LIST).length;
const linkPosts = (page: ReturnType<typeof setup>) =>
  page.api.post.mock.calls.filter(([url]) => url === LIST);

async function open(page: ReturnType<typeof setup>, status = "ready") {
  await page.render();
  await until(() => expect(captured.surface?.files?.status).toBe(status));
  await until(() => expect(captured.rich?.context.media).toBeTruthy());
}

afterEach(() => {
  captured.surface = undefined;
  captured.rich = undefined;
});

describe("the Files panel lists the page's attachments (UC-COL-B10)", () => {
  it("lists a stored file and a registered link, and opens the link only through an http address", async () => {
    const page = setup({
      list: () => [
        LINK,
        REPORT,
        { ...LINK, id: "a3", filename: "Xấu", url: "javascript:alert(1)" },
      ],
    });
    await open(page);
    const { entries } = captured.surface!.files!;
    expect(
      entries.map((entry) => [entry.name, entry.kind, entry.href]),
    ).toEqual([
      ["Hướng dẫn", "link", "https://example.com/guide"],
      ["bao-cao.pdf", "file", null],
      ["Xấu", "link", null],
    ]);
    await page.unmount();
  });

  it("says a page the member cannot see has no files to list, and a failed read has Retry that reads again", async () => {
    let status = 500;
    const page = setup({
      list: () => {
        throw failWith({ status });
      },
    });
    await open(page, "error");
    const before = listReads(page);
    status = 403;
    await act(async () => captured.surface!.files!.onRetry());
    await until(() => expect(captured.surface?.files?.status).toBe("denied"));
    expect(listReads(page)).toBeGreaterThan(before);
    await page.unmount();
  });

  it("answers NotFound like an unknown page", async () => {
    const page = setup({
      list: () => {
        throw failWith({ status: 404 });
      },
    });
    await open(page, "notFound");
    await page.unmount();
  });

  it("gives a viewer the list and the files but no way to add, upload, paste or drop", async () => {
    const page = setup({ canEdit: false });
    await open(page);
    expect(captured.surface?.files?.canEdit).toBe(false);
    // The editor is read-only: its paste and drop handlers act only on an editable view.
    expect(captured.rich?.editable).toBe(false);
    expect(captured.rich?.context.readOnly).toBe(true);
    await page.unmount();
  });
});

describe("registering a link (UC-COL-B09)", () => {
  it("sends the address and name with an Idempotency-Key, then reads the list again", async () => {
    const page = setup();
    await open(page);
    const before = listReads(page);
    let result: string | null = "x";
    await act(async () => {
      result = await captured.surface!.files!.onAddLink({
        url: "https://example.com/spec",
        name: "Đặc tả",
      });
    });
    expect(result).toBeNull();
    const [url, body, request] = linkPosts(page)[0] as [
      string,
      unknown,
      { idempotencyKey: string; skipGlobalErrorToast: boolean },
    ];
    expect(url).toBe(LIST);
    expect(body).toEqual({
      filename: "Đặc tả",
      url: "https://example.com/spec",
    });
    expect(request.idempotencyKey).toMatch(/^[0-9a-f-]{36}$/);
    expect(request.skipGlobalErrorToast).toBe(true);
    await until(() => expect(listReads(page)).toBeGreaterThan(before));
    await page.unmount();
  });

  it("names a link after its host when the member gives no name", async () => {
    const page = setup();
    await open(page);
    await act(async () => {
      await captured.surface!.files!.onAddLink({
        url: "https://docs.example.com/a",
        name: "",
      });
    });
    expect((linkPosts(page)[0] as unknown[])[1]).toEqual({
      filename: "docs.example.com",
      url: "https://docs.example.com/a",
    });
    await page.unmount();
  });

  it("maps a refusal to its reason: 400, 409, 403, 404, offline and a server fault", async () => {
    const failures: object[] = [
      { status: 400 },
      { status: 409, code: "attachment.limit_exceeded" },
      { status: 403 },
      { status: 404 },
      { kind: "network" },
      { status: 500 },
    ];
    const page = setup({
      post: () => {
        throw failWith(failures.shift() as object);
      },
    });
    await open(page);
    const reasons: (string | null)[] = [];
    for (let index = 0; index < 6; index += 1) {
      await act(async () => {
        reasons.push(
          await captured.surface!.files!.onAddLink({
            url: `https://example.com/${index}`,
            name: "x",
          }),
        );
      });
    }
    expect(reasons).toEqual([
      "invalid",
      "limitReached",
      "forbidden",
      "notFound",
      "network",
      "failed",
    ]);
    await page.unmount();
  });

  it("reuses the Idempotency-Key when the same link is retried, and takes a new one for the next link", async () => {
    let attempts = 0;
    const page = setup({
      post: () => {
        attempts += 1;
        if (attempts === 1) throw failWith({ kind: "network" });
        return undefined;
      },
    });
    await open(page);
    const add = (url: string) =>
      act(async () => {
        await captured.surface!.files!.onAddLink({ url, name: "Spec" });
      });
    await add("https://example.com/spec");
    await add("https://example.com/spec");
    await add("https://example.com/other");
    const keys = linkPosts(page).map(
      (call) => (call as unknown as unknown[])[2] as { idempotencyKey: string },
    );
    expect(keys).toHaveLength(3);
    expect(keys[1]?.idempotencyKey).toBe(keys[0]?.idempotencyKey);
    expect(keys[2]?.idempotencyKey).not.toBe(keys[0]?.idempotencyKey);
    await page.unmount();
  });
});

describe("files in the page (UC-COL-B08)", () => {
  it("tells the member a refused file was not sent", async () => {
    const page = setup();
    await open(page);
    captured.rich!.context.media.onRefused(
      new File(["x"], "run.exe"),
      "blockedType",
    );
    expect(page.notify).toHaveBeenCalledWith(
      copy.media.upload.refused.blockedType.replace("{{name}}", "run.exe"),
    );
    await page.unmount();
  });

  it("keeps a network failure of an upload as a failed file the member can retry with the same key", async () => {
    let attempts = 0;
    const page = setup({
      upload: () => {
        attempts += 1;
        if (attempts === 1) throw failWith({ kind: "network" });
        return { id: "new", filename: "a.pdf" };
      },
    });
    await open(page);
    const { uploads } = captured.rich!.context.media;
    expect(uploads.add(new File(["x"], "a.pdf")).ok).toBe(true);
    await until(() => expect(uploads.snapshot()[0]?.status).toBe("failed"));
    expect(uploads.snapshot()[0]?.failure).toBe("failed");
    expect(page.notify).toHaveBeenCalledWith(
      copy.media.upload.failed.replace(
        "{{reason}}",
        copy.media.upload.reasons.failed,
      ),
    );
    const id = uploads.snapshot()[0]!.id;
    await act(async () => uploads.retry(id));
    await until(() => expect(attempts).toBe(2));
    const sent = page.api.upload.mock.calls as unknown as unknown[][];
    expect((sent[1]?.[2] as { idempotencyKey: string }).idempotencyKey).toBe(
      (sent[0]?.[2] as { idempotencyKey: string }).idempotencyKey,
    );
    await page.unmount();
  });

  it("tells the member a download failed", async () => {
    const page = setup({
      getBlob: () => {
        throw failWith({ kind: "network" });
      },
    });
    await open(page);
    await act(async () =>
      captured.rich!.context.media.download({ id: "a1", fileName: "a.pdf" }),
    );
    expect(page.notify).toHaveBeenCalledWith(copy.media.file.downloadFailed);
    await page.unmount();
  });

  it("tells the member a delete failed and reads the files again", async () => {
    const page = setup({
      remove: () => {
        throw failWith({ status: 500 });
      },
    });
    await open(page);
    const before = listReads(page);
    await act(async () =>
      captured.rich!.context.media.remove({ id: "a1", fileName: "a.pdf" }),
    );
    expect(page.notify).toHaveBeenCalledWith(copy.media.file.deleteFailed);
    await until(() => expect(listReads(page)).toBeGreaterThan(before));
    await page.unmount();
  });
});
