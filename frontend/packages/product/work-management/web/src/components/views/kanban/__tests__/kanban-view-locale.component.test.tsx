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

vi.mock("@notrelix/work-management-state", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@notrelix/work-management-state")>();
  const mutation = () => ({ mutate: vi.fn() });
  const lanes = () =>
    columns.map((column) => ({
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
    useBoardView: () => ({
      viewConfig: actual.defaultTableViewConfig,
    }),
    useBoardKanban: () => ({
      board: scenario.board,
      groups: columns,
      statusField: undefined,
      groupByField: undefined,
      groupByFields: [],
      allLanes: lanes(),
      lanes: lanes(),
      visibleCount: 1,
      totalCount: 1,
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
    useBoardAccess: () => ({ access: null, isLoading: true, error: null }),
    useDuplicateCard: mutation,
    useDeleteCard: mutation,
    useMoveItemToBoard: mutation,
    useWorkspaceBoards: () => ({ data: [] }),
    useBoardTrash: () => ({ entries: [], restore: vi.fn() }),
    useUpdateCard: mutation,
    useCreateGroup: mutation,
    useAddFieldOption: () => ({ mutate: vi.fn(), isPending: false }),
    useUpdateFieldOption: mutation,
    useMoveFieldOption: mutation,
    useRemoveFieldOption: mutation,
    useArchiveCard: mutation,
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
