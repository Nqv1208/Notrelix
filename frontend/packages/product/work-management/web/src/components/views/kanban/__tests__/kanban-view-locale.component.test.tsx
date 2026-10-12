import { describe, expect, it, vi } from "vitest";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { renderPureUi, screen } from "@notrelix/testing";
import {
  boardGroupFixture,
  cardFixture,
  fixedIso,
  kanbanDefaultScenario,
} from "@notrelix/work-management-testing";
import { kanbanCardCatalog } from "../kanban-card.catalog";
import { KanbanView } from "../kanban-view";

const scenario = kanbanDefaultScenario();
const columns = [
  boardGroupFixture({
    id: "group-view",
    title: "Doing",
    position: 1,
    cards: [
      cardFixture({
        id: "card-view",
        listId: "group-view",
        title: "View card",
        status: "status-stuck",
        dueDate: fixedIso(-5),
      }),
    ],
  }),
];

vi.mock("@notrelix/work-management-state", () => {
  const mutation = () => ({ mutate: vi.fn() });
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
    useSelectedCardPanel: () => ({
      selectedCardId: null,
      openCard: vi.fn(),
      closePanel: vi.fn(),
    }),
    useDuplicateCard: mutation,
    useDeleteCard: mutation,
    useCreateCard: mutation,
  };
});

function renderView(locale: Locale) {
  renderPureUi(
    <I18nProvider catalogs={[kanbanCardCatalog]} initialLocale={locale}>
      <KanbanView boardId={scenario.board.id} workspaceId="workspace-view" />
    </I18nProvider>,
  );
}

describe("KanbanView resolves the card copy by locale", () => {
  it("shows the Vietnamese card copy for the vi locale", () => {
    renderView("vi");
    expect(screen.getByText(/^Quá hạn \d+ ngày$/)).toBeTruthy();
    expect(screen.queryByText(/^Overdue/)).toBeNull();
  });

  it("shows the English card copy for the en locale", () => {
    renderView("en");
    expect(screen.getByText(/^Overdue \d+ days?$/)).toBeTruthy();
    expect(screen.queryByText(/^Quá hạn/)).toBeNull();
  });
});
