import { beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
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
const columns = [
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

const cardQuery = vi.hoisted(() => ({
  current: {
    card: undefined as unknown,
    isLoading: false,
    error: null as unknown,
  },
}));

vi.mock("@notrelix/work-management-state", () => {
  const mutation = () => ({ mutate: vi.fn() });
  const list = () => ({
    data: undefined,
    isLoading: false,
    isFetching: false,
    refetch: vi.fn(),
  });
  return {
    useBoardKanban: () => ({
      board: scenario.board,
      columns,
      isLoading: false,
      error: null,
      search: { query: "", setQuery: vi.fn() },
      filters: { clear: vi.fn() },
      moveCard: mutation(),
      reorderColumns: mutation(),
      createColumn: mutation(),
      updateColumn: mutation(),
      deleteColumn: mutation(),
    }),
    useCard: () => cardQuery.current,
    useCardFiles: list,
    useCardActivity: list,
    useCardComments: list,
    useUpdateCard: mutation,
    useDuplicateCard: mutation,
    useDeleteCard: mutation,
    useCreateCard: mutation,
    useUpdateFieldValue: mutation,
    useCreateCardUpdate: mutation,
    useUpdateCardUpdate: mutation,
    useDeleteCardUpdate: mutation,
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

function renderView(props: {
  selectedItemId?: string;
  onSelectedItemChange?: (id: string | undefined) => void;
}) {
  return renderPureUi(
    <I18nProvider catalogs={[kanbanCardCatalog]} initialLocale="en">
      <KanbanView
        boardId={scenario.board.id}
        workspaceId={scenario.board.workspaceId}
        {...props}
      />
    </I18nProvider>,
  );
}

describe("KanbanView item detail through selectedItemId", () => {
  beforeEach(() => {
    cardQuery.current = { card: undefined, isLoading: false, error: null };
  });

  it("opens the detail sheet with the selected card's title", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    renderView({ selectedItemId: "card-item", onSelectedItemChange: vi.fn() });
    const dialog = screen.getByRole("dialog", { name: "Item card" });
    expect(dialog).toBeTruthy();
    expect(
      screen.getByRole("textbox", { name: "Edit task title" }).textContent,
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
    fireEvent.click(screen.getByRole("button", { name: "Close task details" }));
    expect(onSelectedItemChange).toHaveBeenCalledWith(undefined);
  });

  it("clears the selection on Escape", () => {
    cardQuery.current = { card: existingCard(), isLoading: false, error: null };
    const onSelectedItemChange = vi.fn();
    renderView({ selectedItemId: "card-item", onSelectedItemChange });
    fireEvent.keyDown(screen.getByLabelText("Task detail panel"), {
      key: "Escape",
    });
    expect(onSelectedItemChange).toHaveBeenCalledWith(undefined);
    expect(onSelectedItemChange).not.toHaveBeenCalledWith("card-item");
  });

  it("selects the clicked card", () => {
    const onSelectedItemChange = vi.fn();
    renderView({ onSelectedItemChange });
    fireEvent.click(screen.getByRole("button", { name: "Item card" }));
    expect(onSelectedItemChange).toHaveBeenCalledWith("card-item");
  });

  it("copies a card link that carries item and keeps the board path and view", () => {
    window.history.pushState(
      {},
      "",
      "/workspaces/ws-1/boards/board-1?view=kanban&filter=mine",
    );
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", {
      value: { writeText },
      configurable: true,
    });
    renderView({ onSelectedItemChange: vi.fn() });
    fireEvent.keyDown(
      screen.getByRole("button", { name: "Open Item card menu" }),
      { key: "Enter" },
    );
    fireEvent.click(screen.getByRole("menuitem", { name: "Copy link" }));
    expect(writeText).toHaveBeenCalledTimes(1);
    const link = new URL(writeText.mock.calls[0]![0] as string);
    expect(link.pathname).toBe("/workspaces/ws-1/boards/board-1");
    expect(link.searchParams.get("item")).toBe("card-item");
    expect(link.searchParams.get("view")).toBe("kanban");
    expect(link.searchParams.get("filter")).toBe("mine");
    expect(link.searchParams.has("taskId")).toBe(false);
  });
});
