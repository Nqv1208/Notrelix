import { useState, type ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  WorkManagementTestScope,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import {
  itemCapabilitiesStub,
  boardGroupFixture,
  cardDetailFixture,
  cardFixture,
  kanbanDefaultScenario,
} from "@notrelix/work-management-testing";
import { kanbanCardCatalog } from "../kanban-card.catalog";
import { KanbanView } from "../kanban-view";

vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));

const scenario = kanbanDefaultScenario();
const boardState = vi.hoisted(() => ({ showCards: true }));
const fullColumns = [
  boardGroupFixture({
    id: "group-item",
    title: "Doing",
    position: 1,
    cards: [
      cardFixture({
        id: "card-item",
        listId: "group-item",
        title: "Item card",
      }),
    ],
  }),
];

const columns = {
  get current() {
    return boardState.showCards
      ? fullColumns
      : fullColumns.map((column) => ({ ...column, cards: [] }));
  },
};

const itemConflict = vi.hoisted(() => ({
  current: {
    conflict: false,
    staleSchema: null as unknown,
    changedBy: null as string | null,
  },
}));

const cardQuery = vi.hoisted(() => ({
  current: {
    card: undefined as unknown,
    isLoading: false,
    error: null as unknown,
  },
  useCard: vi.fn(),
  deleteMutate: vi.fn(
    (
      _input: { cardId: string; expectedVersion?: number },
      options?: { onSuccess?: () => void },
    ) => options?.onSuccess?.(),
  ),
}));

vi.mock("@notrelix/work-management-state", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@notrelix/work-management-state")>();
  const mutation = () => ({ mutate: vi.fn() });
  const list = () => ({
    data: undefined,
    isLoading: false,
    isFetching: false,
    refetch: vi.fn(),
  });
  const lanes = () =>
    columns.current.map((column) => ({
      optionId: "status-doing",
      name: column.title,
      category: "in_progress" as const,
      color: null,
      cards: column.cards,
      total: column.cards.length,
    }));
  return {
    ...actual,
    // The filter bar's saved filters: none, and no write is made by these tests.
    useSavedFilters: () => ({
      data: [],
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    }),
    useSavedFilterWrites: () => ({
      create: vi.fn(),
      rename: vi.fn(),
      setVisibility: vi.fn(),
      remove: vi.fn(),
      restore: vi.fn(),
    }),
    // The member's own view state: nothing saved, nothing persisted by these tests.
    useBoardPreference: () => ({
      criteria: undefined,
      isLoading: false,
      persistent: false,
      save: vi.fn(),
      saving: false,
      saveFailed: false,
      retry: vi.fn(),
      columns: {},
      saveColumns: vi.fn(),
      columnsSaving: false,
      columnsFailed: false,
      retryColumns: vi.fn(),
    }),
    useBoardView: () => ({
      viewConfig: actual.defaultTableViewConfig,
    }),
    useBoardKanban: () => ({
      board: scenario.board,
      groups: columns.current,
      statusField: undefined,
      groupByField: undefined,
      groupByFields: [],
      filterFields: [],
      memberName: (userId: string) => userId,
      allLanes: lanes(),
      lanes: lanes(),
      visibleCount: columns.current[0]!.cards.length,
      totalCount: columns.current[0]!.cards.length,
      filterActive: false,
      filterChoices: { statuses: [], assignees: [], labels: [] },
      isLoading: false,
      error: null,
      search: { query: "", setQuery: vi.fn(), debouncedQuery: "" },
      updateStatus: mutation(),
      updateFieldValue: mutation(),
      moveCard: mutation(),
      createItemInLane: mutation(),
    }),
    useBoardAccess: () => ({
      access: { myLevel: "editor" },
      isLoading: false,
      error: null,
    }),
    useCard: (...args: unknown[]) => {
      cardQuery.useCard(...args);
      return cardQuery.current;
    },
    useCardFiles: list,
    useCardActivity: list,
    useCardComments: list,
    useUpdateCard: mutation,
    useCreateGroup: mutation,
    useItemLinks: () => ({
      data: undefined,
      addLink: vi.fn(),
      removeLink: vi.fn(),
    }),
    useItemApprovals: () => ({
      approvals: undefined,
      decide: vi.fn(),
      cancel: vi.fn(),
      answerError: null,
    }),
    usePeopleDirectory: () => ({ directory: null }),
    useWorkManagementServices: () => ({
      cards: {},
      itemExtras: {},
      itemCapabilities: {
        recordView: async () => undefined,
        getViewers: async () => ({ count: 0, viewers: [] }),
        getVotes: async () => ({ count: 0, voted: false, voters: [] }),
        getBoardChecklists: async () => [],
      },
    }),
    useItemFileUploads: () => ({
      uploads: [],
      start: vi.fn(),
      retry: vi.fn(),
      dismiss: vi.fn(),
    }),
    useDeleteCardFile: mutation,
    useItemFollowing: () => ({
      following: null,
      followers: [],
      setFollowing: vi.fn(),
    }),
    useItemChildItems: () => ({
      data: undefined,
      toggle: vi.fn(),
      addChild: vi.fn(),
    }),
    useCreateItemField: () => vi.fn(),
    useBoardLabels: list,
    useCardLabelWrites: mutation,
    useCreateItemWithDetails: () => ({ mutate: vi.fn(), isPending: false }),
    useDuplicateCard: mutation,
    useFullBoard: () => ({
      board: undefined,
      groups: [],
      fieldDefinitions: [],
      archivedGroupIds: [] as string[],
      isLoading: false,
      error: null,
    }),
    useRefreshItem: () => vi.fn(),
    useDeleteCard: mutation,
    useMoveItemToBoard: mutation,
    useWorkspaceBoards: () => ({ data: [] }),
    useBoardTrash: () => ({ entries: [], restore: vi.fn() }),
    useAddFieldOption: () => ({ mutate: vi.fn(), isPending: false }),
    useUpdateFieldOption: mutation,
    useMoveFieldOption: mutation,
    useRemoveFieldOption: mutation,
    useArchiveCard: () => ({ mutate: cardQuery.deleteMutate }),
    useSeenItemVersion: () => () => 7,
    useItemConflict: () => ({
      ...itemConflict.current,
      clear: vi.fn(),
      report: vi.fn(),
    }),
    useBoardLabelWrites: () => ({ update: vi.fn(), remove: vi.fn() }),
    useCardAttachmentWrites: () => ({ addWebLink: vi.fn(), linkPage: vi.fn() }),
    useCreateCardUpdate: mutation,
    useUpdateCardUpdate: mutation,
    useDeleteCardUpdate: mutation,
    useToggleCommentReaction: mutation,
    useChecklistWrites: () => ({
      addList: vi.fn(),
      renameList: vi.fn(),
      deleteList: vi.fn(),
      moveList: vi.fn(),
      addItem: vi.fn(),
      renameItem: vi.fn(),
      toggleItem: vi.fn(),
      deleteItem: vi.fn(),
      moveItem: vi.fn(),
      convertItem: vi.fn(),
      undo: vi.fn(),
      retry: vi.fn(),
      discard: vi.fn(),
      dismissConflict: vi.fn(),
      failure: null,
      conflict: false,
      pendingDelete: null,
      busy: false,
    }),
  };
});

function existingCard() {
  return cardDetailFixture({
    id: "card-item",
    boardId: scenario.board.id,
    workspaceId: scenario.board.workspaceId,
    listId: "group-item",
    title: "Item card",
  });
}

type ItemChange = (
  id: string | undefined,
  options?: { replace?: boolean },
) => void;

function view(props: {
  selectedItemId?: string;
  onSelectedItemChange?: ItemChange;
}) {
  return (
    <I18nProvider catalogs={[kanbanCardCatalog]} initialLocale="en">
      <KanbanView
        boardId={scenario.board.id}
        workspaceId={scenario.board.workspaceId}
        {...props}
      />
    </I18nProvider>
  );
}

/** The popup reads its own queries (viewers, votes), so it needs a query client. */
function scoped(children: ReactNode) {
  return (
    <WorkManagementTestScope
      services={
        {
          itemCapabilities: itemCapabilitiesStub(),
        } as unknown as WorkManagementServices
      }
    >
      {children}
    </WorkManagementTestScope>
  );
}

function renderView(props: {
  selectedItemId?: string;
  onSelectedItemChange?: ItemChange;
}) {
  return renderPureUi(scoped(view(props)));
}

/** Holds the selection like the route does, so close really unmounts the sheet. */
function StatefulView({ initial }: { initial?: string }) {
  const [item, setItem] = useState<string | undefined>(initial);
  return scoped(view({ selectedItemId: item, onSelectedItemChange: setItem }));
}

const REPLACE = { replace: true };
const GONE = () =>
  screen.queryByText("The card is gone, or you no longer have access");

describe("KanbanView item detail through selectedItemId", () => {
  beforeEach(() => {
    cardQuery.current = { card: undefined, isLoading: false, error: null };
    cardQuery.useCard.mockClear();
    cardQuery.deleteMutate.mockClear();
    boardState.showCards = true;
    itemConflict.current = {
      conflict: false,
      staleSchema: null,
      changedBy: null,
    };
  });

  it("opens the detail sheet with the selected card's title", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    const dialog = screen.getByRole("dialog", { name: "Item card" });
    expect(dialog).toBeTruthy();
    expect(
      (
        screen.getByRole("textbox", {
          name: "Card title",
        }) as HTMLTextAreaElement
      ).value,
    ).toBe("Item card");
  });

  it("renders no sheet and does not throw for an unknown id", () => {
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: new Error("not found"),
    };
    expect(() =>
      renderView({
        selectedItemId: "missing-card",
        onSelectedItemChange: vi.fn(),
      }),
    ).not.toThrow();
    expect(screen.getByText("Could not load the card")).toBeTruthy();
  });

  it("says the card is gone for a card that belongs to another board", () => {
    cardQuery.current = {
      card: { ...existingCard(), boardId: "another-board" },
      isLoading: false,
      error: null,
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(GONE()).toBeTruthy();
  });

  it("names who deleted the card to a person who reads its board", () => {
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: Object.assign(new Error("not found"), {
        kind: "not_found",
        code: "work-management.item.deleted",
        details: { deletedByName: "Ada Lovelace" },
      }),
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(screen.getByText("Ada Lovelace deleted this card")).toBeTruthy();
    expect(GONE()).toBeNull();
  });

  it("says someone else deleted the card when the server names no one (a Guest, or a person out of reach)", () => {
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: Object.assign(new Error("not found"), {
        kind: "not_found",
        code: "work-management.item.deleted",
        details: { deletedByName: null },
      }),
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(screen.getByText("Someone else deleted this card")).toBeTruthy();
  });

  it("keeps an ordinary not-found as the one generic message", () => {
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: Object.assign(new Error("not found"), {
        kind: "not_found",
        code: "common.resource-not-found",
      }),
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(GONE()).toBeTruthy();
    expect(screen.queryByText(/deleted this card/)).toBeNull();
  });

  it("names who saved first on the conflict notice, and falls back to someone else", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    itemConflict.current = {
      conflict: true,
      staleSchema: null,
      changedBy: "Ada Lovelace",
    };
    const { unmount } = renderView({
      selectedItemId: "card-item",
      onSelectedItemChange: vi.fn(),
    });
    expect(screen.getByText("Ada Lovelace just edited this card")).toBeTruthy();
    unmount();
    itemConflict.current = {
      conflict: true,
      staleSchema: null,
      changedBy: null,
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(screen.getByText("Someone else just edited this card")).toBeTruthy();
  });

  it("renders no sheet when no item is selected", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    renderView({ onSelectedItemChange: vi.fn() });
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("clears the selection when the sheet closes", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    const onSelectedItemChange = vi.fn();
    renderView({ selectedItemId: "card-item", onSelectedItemChange });
    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    expect(onSelectedItemChange).toHaveBeenCalledWith(undefined, REPLACE);
  });

  it("clears the selection on Escape", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    const onSelectedItemChange = vi.fn();
    renderView({ selectedItemId: "card-item", onSelectedItemChange });
    fireEvent.keyDown(screen.getByRole("dialog"), {
      key: "Escape",
    });
    expect(onSelectedItemChange).toHaveBeenCalledWith(undefined, REPLACE);
    expect(onSelectedItemChange).not.toHaveBeenCalledWith("card-item");
  });

  it("selects the clicked card", () => {
    const onSelectedItemChange = vi.fn();
    renderView({ onSelectedItemChange });
    fireEvent.click(screen.getByRole("button", { name: "Item card" }));
    expect(onSelectedItemChange).toHaveBeenCalledWith("card-item");
  });

  it("looks the card up with its id and the board's workspace", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(cardQuery.useCard).toHaveBeenCalledWith(
      "card-item",
      scenario.board.workspaceId,
    );
  });

  it("says the card is gone when the card's board matches but its workspace differs", () => {
    cardQuery.current = {
      card: { ...existingCard(), workspaceId: "another-workspace" },
      isLoading: false,
      error: null,
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(GONE()).toBeTruthy();
  });

  it("clears a gone item with history replace from the way back to the board", () => {
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: Object.assign(new Error("not found"), { kind: "not_found" }),
    };
    const onSelectedItemChange = vi.fn();
    renderView({ selectedItemId: "missing-card", onSelectedItemChange });
    expect(GONE()).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Back to the board" }));
    expect(onSelectedItemChange).toHaveBeenCalledWith(undefined, REPLACE);
  });

  it("does not clear the item while the card is still loading", () => {
    cardQuery.current = { card: undefined, isLoading: true, error: null };
    const onSelectedItemChange = vi.fn();
    renderView({ selectedItemId: "card-item", onSelectedItemChange });
    expect(onSelectedItemChange).not.toHaveBeenCalled();
  });

  it("archiving from the sheet clears item, and a restored id does not reopen from cache", async () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    const onSelectedItemChange = vi.fn();
    const { rerender } = renderView({
      selectedItemId: "card-item",
      onSelectedItemChange,
    });
    fireEvent.keyDown(screen.getByRole("button", { name: "Card actions" }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: "Archive…" }));
    // Archiving asks first; nothing is sent until it is confirmed.
    expect(cardQuery.deleteMutate).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Archive item" }));
    // It names the version the dialog opened with.
    expect(cardQuery.deleteMutate).toHaveBeenCalledWith(
      { cardId: "card-item", expectedVersion: 7 },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
    expect(onSelectedItemChange).toHaveBeenCalledWith(undefined, REPLACE);

    // Back restores `?item=card-item`; the card is gone, so the query fails.
    onSelectedItemChange.mockClear();
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: Object.assign(new Error("not found"), { kind: "not_found" }),
    };
    rerender(
      scoped(view({ selectedItemId: "card-item", onSelectedItemChange })),
    );
    expect(screen.queryByRole("dialog", { name: "Item card" })).toBeNull();
    expect(GONE()).toBeTruthy();
  });

  describe("focus return", () => {
    it("returns focus to the opening card after the close button", async () => {
      cardQuery.current = {
        card: existingCard(),
        isLoading: false,
        error: null,
      };
      renderPureUi(<StatefulView />);
      const opener = screen.getByRole("button", { name: "Item card" });
      opener.focus();
      fireEvent.click(opener);
      fireEvent.click(await screen.findByRole("button", { name: "Close" }));
      await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
      await waitFor(() => expect(document.activeElement).toBe(opener));
    });

    it("returns focus to the opening card after Escape", async () => {
      cardQuery.current = {
        card: existingCard(),
        isLoading: false,
        error: null,
      };
      renderPureUi(<StatefulView />);
      const opener = screen.getByRole("button", { name: "Item card" });
      opener.focus();
      fireEvent.click(opener);
      fireEvent.keyDown(await screen.findByRole("dialog"), {
        key: "Escape",
      });
      await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
      await waitFor(() => expect(document.activeElement).toBe(opener));
    });

    it("focuses the board region when the opening card no longer exists", async () => {
      cardQuery.current = {
        card: existingCard(),
        isLoading: false,
        error: null,
      };
      boardState.showCards = false;
      renderPureUi(<StatefulView initial="card-item" />);
      fireEvent.click(await screen.findByRole("button", { name: "Close" }));
      await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
      await waitFor(() =>
        expect(
          (document.activeElement as HTMLElement | null)?.hasAttribute(
            "data-kanban-board",
          ),
        ).toBe(true),
      );
    });
  });
});
