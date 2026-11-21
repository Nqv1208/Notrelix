import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { I18nProvider } from "@notrelix/i18n";
import { ThemeProvider } from "@notrelix/ui-web";
import { endpoints } from "@notrelix/contracts";
import { webCatalogs } from "../i18n/catalogs";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const h = vi.hoisted(() => ({
  api: null as unknown,
  toasts: [] as { kind: "ok" | "err"; message: string; description?: string }[],
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: h.api, endpoints } }),
}));
vi.mock("sonner", () => ({
  toast: {
    success: (message: string) => h.toasts.push({ kind: "ok", message }),
    error: (message: string, options?: { description?: string }) =>
      h.toasts.push({
        kind: "err",
        message,
        description: options?.description,
      }),
  },
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({
    workspaceId: "ws-1",
    workspace: { name: "Acme", memberCount: 2 },
  }),
}));
vi.mock("@/shell/workspace/use-workspace-viewer", () => ({
  useWorkspaceViewer: () => ({ role: "member", can: () => true }),
}));
vi.mock("@/shell/workspace/use-workspace-resources", () => ({
  useWorkspaceResources: () => ({
    ready: true,
    failed: false,
    retry: () => undefined,
    docs: [],
    boards: [
      {
        id: "b-1",
        name: "Q4 Launch",
        parentName: null,
        parentId: null,
        updatedAt: null,
        spaceId: null,
      },
      {
        id: "b-2",
        name: "Sprint 42 retro",
        parentName: null,
        parentId: null,
        updatedAt: null,
        spaceId: null,
      },
    ],
  }),
}));
vi.mock("@/shell/workspace/use-workspace-spaces", () => ({
  useSpaceHooks: () => ({ useSpaceTree: () => ({ data: [] }) }),
}));
vi.mock("@/shell/workspace/board-placement-bridge", () => ({
  useBoardPlacements: () => ({ data: [] }),
}));
vi.mock("@/shell/workspace/use-move-to-space", () => ({
  useMoveToSpace: () => ({
    openForBoard: () => undefined,
    openForPage: () => undefined,
    dialog: null,
  }),
}));
vi.mock("@/shell/workspace/use-create-resource", () => ({
  useCreateResource: () => ({ open: () => undefined, dialog: null }),
}));
vi.mock("@/shell/workspace/use-shell-navigation", () => ({
  useShellNavigation: () => ({
    getHref: () => "#",
    onNavigate: () => undefined,
  }),
}));

import { LibraryPage } from "../routes/workspaces/$workspaceId/library";

beforeAll(() => {
  // Radix Select measures and scrolls items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

let root: Root | null = null;
let host: HTMLDivElement | null = null;
let queryClient: QueryClient | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  host = null;
  h.toasts.length = 0;
});

async function flush() {
  for (let i = 0; i < 20; i += 1)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 0));
    });
}

function mount(locale: "en" | "vi" = "en") {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  queryClient = client;
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  act(() => {
    root?.render(
      <QueryClientProvider client={client}>
        <AccountScopeProvider accountId="acc-test">
          <I18nProvider catalogs={webCatalogs} initialLocale={locale}>
            <ThemeProvider target={null}>
              <LibraryPage />
            </ThemeProvider>
          </I18nProvider>
        </AccountScopeProvider>
      </QueryClientProvider>,
    );
  });
}

/** A producer whose saved pins are `saved`; `put` and `delete` are the scenario's. */
function producer(
  saved: string[],
  write: {
    put?: (url: string) => Promise<unknown>;
    delete?: (url: string) => Promise<unknown>;
  },
) {
  const put = vi.fn(async (url: string) => {
    if (write.put) return write.put(url);
    saved.push(url.split("/").pop()!);
    return undefined;
  });
  const del = vi.fn(async (url: string) => {
    if (write.delete) return write.delete(url);
    const id = url.split("/").pop()!;
    saved.splice(saved.indexOf(id), 1);
    return undefined;
  });
  h.api = {
    get: vi.fn(async () =>
      saved.map((resourceId) => ({
        kind: "work-management.board",
        resourceId,
        pinnedAt: "2026-11-19T08:00:00Z",
      })),
    ),
    put,
    delete: del,
    post: vi.fn(),
    patch: vi.fn(),
  };
  return { put, del };
}

function button(name: string): HTMLButtonElement | undefined {
  return Array.from(host!.querySelectorAll("button")).find(
    (candidate) => candidate.getAttribute("aria-label") === name,
  );
}

async function click(name: string) {
  const target = button(name);
  expect(target, `the "${name}" button`).toBeDefined();
  await act(async () => target!.click());
}

function pinsInCache(): string[] {
  return (queryClient?.getQueryCache().findAll() ?? [])
    .map((query) => query.state.data)
    .filter((data): data is { resourceId: string }[] => Array.isArray(data))
    .flatMap((rows) => rows.map((row) => row.resourceId));
}

const PIN_LIMIT = {
  kind: "unknown",
  status: 400,
  details: {
    status: 400,
    errorCode: "business_rule.violation",
    ruleCode: "Workspaces_Navigation_PinLimitReached",
    detail: "A member can pin at most 50 resources in a workspace.",
  },
};

describe("Library pin toggle", () => {
  it("FUI[workspace.library:pin] pins a Board, then shows it as pinned", async () => {
    const { put } = producer([], {});
    mount();
    await flush();

    await click("Pin Q4 Launch");
    await flush();

    expect(put).toHaveBeenCalledTimes(1);
    expect(put.mock.calls[0]![0]).toContain("/pins/work-management.board/b-1");
    expect(button("Unpin Q4 Launch"), "the row now offers Unpin").toBeDefined();
    expect(
      button("Pin Sprint 42 retro"),
      "the other row is untouched",
    ).toBeDefined();
    expect(h.toasts).toEqual([{ kind: "ok", message: "Pinned 1 item." }]);
  });

  it("FUI[workspace.library:unpin] unpins a pinned Board", async () => {
    const { del } = producer(["b-2"], {});
    mount();
    await flush();

    await click("Unpin Sprint 42 retro");
    await flush();

    expect(del).toHaveBeenCalledTimes(1);
    expect(del.mock.calls[0]![0]).toContain("/pins/work-management.board/b-2");
    expect(
      button("Pin Sprint 42 retro"),
      "the row offers Pin again",
    ).toBeDefined();
    expect(h.toasts).toEqual([{ kind: "ok", message: "Unpinned 1 item." }]);
  });

  it("FUI[workspace.library:pin-rollback] shows the pin at once and rolls it back when the producer refuses", async () => {
    let refuse: (reason: unknown) => void = () => undefined;
    producer([], {
      put: () =>
        new Promise((_resolve, reject) => {
          refuse = reject;
        }),
    });
    mount();
    await flush();

    await click("Pin Q4 Launch");
    await flush();
    expect(
      pinsInCache(),
      "the pin list is patched while the write is open",
    ).toEqual(["b-1"]);

    await act(async () => refuse({ kind: "network", status: 0 }));
    await flush();

    expect(pinsInCache(), "the failed pin leaves the list").toEqual([]);
    expect(button("Pin Q4 Launch"), "the row offers Pin again").toBeDefined();
    expect(h.toasts).toEqual([
      {
        kind: "err",
        message: "Some pins couldn't be updated.",
        description: "The pin list shows what was saved. Try again.",
      },
    ]);
  });

  it("FUI[workspace.library:pin-limit] says the limit of 50 in the member's words, not the producer's text", async () => {
    producer([], { put: () => Promise.reject(PIN_LIMIT) });
    mount();
    await flush();

    await click("Pin Q4 Launch");
    await flush();

    expect(
      button("Pin Q4 Launch"),
      "the refused pin is not shown as pinned",
    ).toBeDefined();
    expect(h.toasts).toEqual([
      {
        kind: "err",
        message: "Some pins couldn't be updated.",
        description:
          "You can pin up to 50 items in a workspace. Unpin one to pin another.",
      },
    ]);
  });

  it("FUI[workspace.library:pin-limit] says the limit in Vietnamese for a Vietnamese member", async () => {
    producer([], { put: () => Promise.reject(PIN_LIMIT) });
    mount("vi");
    await flush();

    await click("Ghim Q4 Launch");
    await flush();

    expect(h.toasts).toHaveLength(1);
    expect(h.toasts[0]).toMatchObject({
      kind: "err",
      message: "Không cập nhật được một số ghim.",
    });
    expect(h.toasts[0]!.description).toContain("tối đa 50 mục");
    expect(h.toasts[0]!.description).not.toContain("A member can pin");
  });
});
