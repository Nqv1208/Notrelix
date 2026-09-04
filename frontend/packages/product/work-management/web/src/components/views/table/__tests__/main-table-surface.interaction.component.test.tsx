import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { tableDefaultScenario } from "@notrelix/work-management-testing";
import { MainTableSurface } from "../main-table-surface";

function renderTable(
  overrides: Partial<Parameters<typeof MainTableSurface>[0]> = {},
) {
  const scenario = tableDefaultScenario();
  const selectedCardIds: string[] = [];
  const props: Parameters<typeof MainTableSurface>[0] = {
    board: scenario.board,
    columns: scenario.columns,
    groups: scenario.groups,
    fieldDefinitions: scenario.columns.map((column) => column.field),
    selectedCardIds,
    selectedCardIdSet: new Set(selectedCardIds),
    isAllSelected: false,
    activeDetailCardId: null,
    searchQuery: "",
    hiddenFieldIds: [],
    onSearchChange: vi.fn(),
    onNewTaskIntent: vi.fn(),
    onCreateGroup: vi.fn(),
    onCreateColumn: vi.fn(),
    onClearFilters: vi.fn(),
    onSetFilters: vi.fn(),
    onClearSort: vi.fn(),
    onSetSort: vi.fn(),
    onSetGroupBy: vi.fn(),
    onResetTableView: vi.fn(),
    onToggleFieldVisible: vi.fn(),
    onDeleteSelectedCards: vi.fn(),
    onToggleAll: vi.fn(),
    onResizeColumn: vi.fn(),
    onHideColumn: vi.fn(),
    onRenameColumn: vi.fn(),
    onDeleteColumn: vi.fn(),
    onSetCardSelected: vi.fn(),
    onOpenDetail: vi.fn(),
    onToggleGroup: vi.fn(),
    onCreateTask: vi.fn(),
    onRenameGroup: vi.fn(),
    onUpdateGroupColor: vi.fn(),
    onDuplicateGroup: vi.fn(),
    onDeleteGroup: vi.fn(),
    onDuplicateCard: vi.fn(),
    onDeleteCard: vi.fn(),
    onUpdateCard: vi.fn(),
    onUpdateFieldValue: vi.fn(),
    onMoveRow: vi.fn(),
    ...overrides,
  };
  renderPureUi(<MainTableSurface {...props} />);
  return { scenario, props };
}

describe("MainTableSurface interactions", () => {
  it("opens rows by pointer and keyboard through injected callbacks", () => {
    const { props } = renderTable();
    const row = screen.getByLabelText(/Table card 1\.1 in Group 1/i);

    fireEvent.click(row);
    fireEvent.keyDown(row, { key: "Enter" });
    fireEvent.keyDown(row, { key: " " });

    expect(props.onOpenDetail).toHaveBeenCalledTimes(3);
    expect(props.onOpenDetail).toHaveBeenCalledWith("table-card-default-1-1");
  });

  it("submits a new task through the injected create callback", () => {
    const { scenario, props } = renderTable();
    const input = screen.getByRole("textbox", { name: /Add task to Group 1/i });

    fireEvent.change(input, { target: { value: "Table interaction task" } });
    fireEvent.submit(input.closest("form")!);

    expect(props.onCreateTask).toHaveBeenCalledWith(
      scenario.groups[0]!.id,
      "Table interaction task",
      expect.any(Number),
    );
    expect(
      Number.isFinite(vi.mocked(props.onCreateTask).mock.calls[0]?.[2]),
    ).toBe(true);
  });

  it("commits a representative title edit through the injected update callback", () => {
    const { props } = renderTable();

    fireEvent.click(
      screen.getByRole("button", { name: /Edit Table card 1\.1/i }),
    );
    const input = screen.getByRole("textbox", { name: /Edit Task/i });
    fireEvent.change(input, { target: { value: "Renamed table task" } });
    fireEvent.keyDown(input, { key: "Enter" });

    expect(props.onUpdateCard).toHaveBeenCalledWith("table-card-default-1-1", {
      title: "Renamed table task",
    });
  });

  it("commits a group rename through the injected group callback", () => {
    const { scenario, props } = renderTable();

    fireEvent.doubleClick(screen.getByRole("heading", { name: "Group 1" }));
    const input = screen.getByRole("textbox", { name: /Rename Group 1/i });
    fireEvent.change(input, { target: { value: "Renamed group" } });
    fireEvent.keyDown(input, { key: "Enter" });

    expect(props.onRenameGroup).toHaveBeenCalledWith(
      scenario.groups[0]!.id,
      "Renamed group",
    );
  });
});
