import { useState } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import {
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

const cardQuery = vi.hoisted(() => ({
  current: {
    card: undefined as unknown,
    isLoading: false,
    error: null as unknown,
  },
  useCard: vi.fn(),
  deleteMutate: vi.fn(),
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
    useBoardView: () => ({
      viewConfig: actual.defaultTableViewConfig,
    }),
    useBoardKanban: () => ({
      board: scenario.board,
      groups: columns.current,
      statusField: undefined,
      groupByField: undefined,
      groupByFields: [],
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
    useBoardLabels: list,
    useCardLabelWrites: mutation,
    useCreateItemWithDetails: () => ({ mutate: vi.fn(), isPending: false }),
    useDuplicateCard: mutation,
    useDeleteCard: mutation,
    useMoveItemToBoard: mutation,
    useWorkspaceBoards: () => ({ data: [] }),
    useBoardTrash: () => ({ entries: [], restore: vi.fn() }),
    useAddFieldOption: () => ({ mutate: vi.fn(), isPending: false }),
    useUpdateFieldOption: mutation,
    useMoveFieldOption: mutation,
    useRemoveFieldOption: mutation,
    useArchiveCard: () => ({ mutate: cardQuery.deleteMutate }),
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

function renderView(props: {
  selectedItemId?: string;
  onSelectedItemChange?: ItemChange;
}) {
  return renderPureUi(view(props));
}

/** Holds the selection like the route does, so close really unmounts the sheet. */
function StatefulView({ initial }: { initial?: string }) {
  const [item, setItem] = useState<string | undefined>(initial);
  return view({ selectedItemId: item, onSelectedItemChange: setItem });
}

const REPLACE = { replace: true };

describe("KanbanView item detail through selectedItemId", () => {
  beforeEach(() => {
    cardQuery.current = { card: undefined, isLoading: false, error: null };
    cardQuery.useCard.mockClear();
    cardQuery.deleteMutate.mockClear();
    boardState.showCards = true;
  });

  it("opens the detail sheet with the selected card's title", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    const dialog = screen.getByRole("dialog", { name: "Item card" });
    expect(dialog).toBeTruthy();
    expect(
      screen.getByRole("textbox", { name: "Edit title" }).textContent,
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
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("renders no sheet for a card that belongs to another board", () => {
    cardQuery.current = {
      card: { ...existingCard(), boardId: "another-board" },
      isLoading: false,
      error: null,
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(screen.queryByRole("dialog")).toBeNull();
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
    fireEvent.click(screen.getByRole("button", { name: "Close (Esc)" }));
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

  it("renders no sheet when the card's board matches but its workspace differs", () => {
    cardQuery.current = {
      card: { ...existingCard(), workspaceId: "another-workspace" },
      isLoading: false,
      error: null,
    };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("clears a stale item with history replace when the card cannot be loaded", async () => {
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: new Error("not found"),
    };
    const onSelectedItemChange = vi.fn();
    renderView({ selectedItemId: "missing-card", onSelectedItemChange });
    await waitFor(() =>
      expect(onSelectedItemChange).toHaveBeenCalledWith(undefined, REPLACE),
    );
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
    fireEvent.keyDown(screen.getByRole("button", { name: "More" }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: "Archive" }));
    expect(cardQuery.deleteMutate).toHaveBeenCalledWith("card-item");
    expect(onSelectedItemChange).toHaveBeenCalledWith(undefined, REPLACE);

    // Back restores `?item=card-item`; the card is gone, so the query fails.
    onSelectedItemChange.mockClear();
    cardQuery.current = {
      card: undefined,
      isLoading: false,
      error: new Error("not found"),
    };
    rerender(view({ selectedItemId: "card-item", onSelectedItemChange }));
    expect(screen.queryByRole("dialog")).toBeNull();
    await waitFor(() =>
      expect(onSelectedItemChange).toHaveBeenCalledWith(undefined, REPLACE),
    );
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
      fireEvent.click(
        await screen.findByRole("button", { name: "Close (Esc)" }),
      );
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
      fireEvent.click(
        await screen.findByRole("button", { name: "Close (Esc)" }),
      );
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
