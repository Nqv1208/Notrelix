import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  RouterProvider,
  createMemoryHistory,
  createRootRoute,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import { boardSearchSchema } from "../router/schemas/board-search-schema";

const loadedBoard = vi.hoisted(() => ({
  current: { title: "Roadmap" } as { title: string } | undefined,
}));

vi.mock("@notrelix/work-management-state", () => ({
  useFullBoard: () => ({ board: loadedBoard.current }),
  useSavedViews: () => ({ views: [], create: vi.fn() }),
  pickSavedView: () => undefined,
  BoardViewSelectionProvider: ({ children }: { children: React.ReactNode }) =>
    children,
}));

vi.mock("@notrelix/features-auth", () => ({
  useCurrentUser: () => ({ id: "user-1" }),
}));

vi.mock("../shell/workspace/board-share-dialog", () => ({
  BoardShareDialog: () => null,
}));

vi.mock("../shell/workspace/use-board-header-data", () => ({
  useBoardHeaderData: () => ({}),
}));

vi.mock("../realtime/use-realtime-connection", () => ({
  useRealtimeConnection: () => ({
    state: "connected",
    lostAt: null,
    reconnect: () => undefined,
  }),
}));

vi.mock("@notrelix/i18n", () => ({ useLocale: () => ({ locale: "vi" }) }));

vi.mock("@notrelix/work-management-web", () => ({
  BoardConnectionNotice: () => null,
  BoardHeaderCopyContext: {
    Provider: ({ children }: { children: React.ReactNode }) => children,
  },
  BoardHeaderDataContext: {
    Provider: ({ children }: { children: React.ReactNode }) => children,
  },
  useBoardHeaderCopy: () => ({ untitledBoard: "Bảng chưa tải" }),
  BoardTrashHost: () => null,
  BoardLayoutShell: ({
    boardTitle,
    onViewChange,
    children,
  }: {
    boardTitle: string;
    onViewChange: (view: string) => void;
    children: React.ReactNode;
  }) => (
    <div>
      <h2 data-testid="board-title">{boardTitle}</h2>
      <button type="button" onClick={() => onViewChange("table")}>
        switch to table
      </button>
      <button type="button" onClick={() => onViewChange("not-a-view")}>
        switch to unsupported
      </button>
      {children}
    </div>
  ),
  BoardWorkspaceViewContent: ({
    view,
    selectedItemId,
    onSelectedItemChange,
    onFilterChange,
    onGroupByChange,
  }: {
    view: { type: string };
    selectedItemId?: string;
    onSelectedItemChange: (
      id: string | undefined,
      options?: { replace?: boolean },
    ) => void;
    onFilterChange: (
      value: string | undefined,
      options?: { replace?: boolean },
    ) => void;
    onGroupByChange: (fieldId: string) => void;
  }) => (
    <div>
      <button type="button" onClick={() => onFilterChange("p.low~o")}>
        filter low overdue
      </button>
      <button
        type="button"
        onClick={() => onFilterChange(undefined, { replace: true })}
      >
        clean filter
      </button>
      <button type="button" onClick={() => onGroupByChange("field-priority")}>
        group by priority
      </button>
      <p data-testid="view">{view.type}</p>
      <p data-testid="item">{selectedItemId ?? "none"}</p>
      <button type="button" onClick={() => onSelectedItemChange("card-1")}>
        open card
      </button>
      <button
        type="button"
        onClick={() => onSelectedItemChange(undefined, { replace: true })}
      >
        close card
      </button>
    </div>
  ),
}));

import { BoardPage } from "../routes/workspaces/$workspaceId/boards/$boardId";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | undefined;
let host: HTMLElement | undefined;

afterEach(() => {
  loadedBoard.current = { title: "Roadmap" };
  act(() => root?.unmount());
  host?.remove();
  root = undefined;
  host = undefined;
});

const text = (testId: string) =>
  host?.querySelector(`[data-testid="${testId}"]`)?.textContent;

async function click(label: string) {
  const button = Array.from(host?.querySelectorAll("button") ?? []).find(
    (element) => element.textContent === label,
  );
  if (!button) throw new Error(`No button "${label}"`);
  await act(async () => {
    button.click();
  });
}

async function renderBoardPage(url: string) {
  const rootRoute = createRootRoute();
  const boardRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/workspaces/$workspaceId/boards/$boardId",
    validateSearch: (search) => boardSearchSchema.parse(search),
    component: BoardPage,
  });
  const history = createMemoryHistory({ initialEntries: [url] });
  const router = createRouter({
    routeTree: rootRoute.addChildren([boardRoute]),
    history,
  });
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await act(async () => {
    root?.render(<RouterProvider router={router} />);
  });
  await vi.waitFor(() => expect(text("view")).toBeDefined());
  const search = () => router.state.location.search as Record<string, unknown>;
  return { router, history, search };
}

const BASE = "/workspaces/w1/boards/b1";

describe("BoardPage header title", () => {
  it("shows the board title once it is loaded", async () => {
    await renderBoardPage("/workspaces/w1/boards/b1?view=kanban");
    expect(text("board-title")).toBe("Roadmap");
  });

  it("falls back to the localized header copy, not an English literal, while the board is not loaded", async () => {
    loadedBoard.current = undefined;
    await renderBoardPage("/workspaces/w1/boards/b1?view=kanban");
    expect(text("board-title")).toBe("Bảng chưa tải");
  });
});

describe("BoardPage route search", () => {
  it("switching the view clears item and keeps filter, sort and groupBy", async () => {
    const { search } = await renderBoardPage(
      `${BASE}?view=kanban&filter=mine&sort=due&groupBy=status&item=card-1`,
    );
    expect(text("item")).toBe("card-1");

    await click("switch to table");

    await vi.waitFor(() => expect(text("view")).toBe("table"));
    expect(search()).toEqual({
      view: "table",
      filter: "mine",
      sort: "due",
      groupBy: "status",
    });
    expect(text("item")).toBe("none");
  });

  it("ignores a view the schema does not support", async () => {
    const { search } = await renderBoardPage(`${BASE}?view=kanban&filter=mine`);
    await click("switch to unsupported");
    expect(search()).toMatchObject({ view: "kanban", filter: "mine" });
  });

  it("opening a card pushes history and keeps the other parameters", async () => {
    const { history, search } = await renderBoardPage(
      `${BASE}?view=kanban&filter=mine&sort=due&groupBy=status`,
    );
    const before = history.length;
    await click("open card");
    await vi.waitFor(() => expect(text("item")).toBe("card-1"));
    expect(history.length).toBe(before + 1);
    expect(search()).toMatchObject({
      view: "kanban",
      filter: "mine",
      sort: "due",
      groupBy: "status",
      item: "card-1",
    });
  });

  it("closing with replace removes item without adding a history entry", async () => {
    const { history, search } = await renderBoardPage(
      `${BASE}?view=kanban&filter=mine&item=card-1`,
    );
    const before = history.length;
    await click("close card");
    await vi.waitFor(() => expect(text("item")).toBe("none"));
    expect(history.length).toBe(before);
    expect(search()).toEqual({ view: "kanban", filter: "mine" });
  });

  it("a filter change pushes history and keeps the other parameters", async () => {
    const { history, search } = await renderBoardPage(
      `${BASE}?view=kanban&groupBy=status`,
    );
    const before = history.length;
    await click("filter low overdue");
    await vi.waitFor(() =>
      expect(search()).toMatchObject({ filter: "p.low~o", groupBy: "status" }),
    );
    expect(history.length).toBe(before + 1);
  });

  it("the view's cleanup of a stale filter replaces history", async () => {
    const { history, search } = await renderBoardPage(
      `${BASE}?view=kanban&filter=mine&item=card-1`,
    );
    const before = history.length;
    await click("clean filter");
    await vi.waitFor(() =>
      expect(search()).toEqual({ view: "kanban", item: "card-1" }),
    );
    expect(history.length).toBe(before);
  });

  it("Nhóm theo writes the groupBy parameter", async () => {
    const { search } = await renderBoardPage(`${BASE}?view=kanban`);
    await click("group by priority");
    await vi.waitFor(() =>
      expect(search()).toMatchObject({ groupBy: "field-priority" }),
    );
  });
});
