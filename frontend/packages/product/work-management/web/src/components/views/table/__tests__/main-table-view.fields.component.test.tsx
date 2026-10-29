import { useState, type ReactNode } from "react";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import type {
  BoardAccess,
  CardDetail,
  FieldDefinition,
  FullBoardResponse,
  SavedBoardView,
  ViewConfig,
} from "@notrelix/work-management-core";
import {
  WorkManagementTestScope,
  defaultTableViewConfig,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import {
  erpBoardScenario,
  erpCommenterAccess,
  erpItemId,
  erpOwnerAccess,
} from "@notrelix/work-management-testing";
import { kanbanLaneCatalog } from "../../kanban/kanban-lane.catalog";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import { statusOptionFormCatalog } from "../../../status-option/status-option-form.catalog";
import { mainTableCatalog } from "../main-table.catalog";
import { MainTableView } from "../main-table-view";

const toast = vi.hoisted(() => ({ success: vi.fn(), error: vi.fn() }));
vi.mock("sonner", () => ({ toast }));

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

afterEach(() => {
  toast.success.mockClear();
  toast.error.mockClear();
});

const NOTE = "field-note";
const POINTS = "field-points";
const DOUBLE = "field-double";

const field = (
  id: string,
  name: string,
  fieldType: FieldDefinition["fieldType"],
  position: number,
): FieldDefinition => ({
  id,
  boardId: "board-erp",
  name,
  fieldType,
  options: [],
  position,
  isHidden: false,
  isSystemField: false,
});

/** The ERP board with three custom fields: a note, a number and a formula over it. */
function boardWithFields(): FullBoardResponse {
  const base = erpBoardScenario();
  const points: Record<string, number> = {
    [erpItemId("ERP-147")]: 13,
    [erpItemId("ERP-149")]: 5,
    [erpItemId("ERP-151")]: 8,
  };
  return {
    ...base,
    board: {
      ...base.board,
      fieldDefinitions: [
        ...base.board.fieldDefinitions,
        field(NOTE, "Ghi chú", "text", 90),
        field(POINTS, "Điểm", "number", 91),
        field(DOUBLE, "Điểm gấp đôi", "formula", 92),
      ],
    },
    groups: base.groups.map((group) => ({
      ...group,
      cards: group.cards.map((card) => {
        const value = points[card.id];
        return {
          ...card,
          fieldValues: value === undefined ? {} : { [POINTS]: value },
          computedValues: {
            [DOUBLE]:
              value === undefined
                ? { state: "empty" as const }
                : { state: "ok" as const, value: value * 2 },
          },
        };
      }),
    })),
  };
}

function detailOf(board: FullBoardResponse, id: string): CardDetail {
  const card = board.groups
    .flatMap((group) => group.cards)
    .find((candidate) => candidate.id === id)!;
  return {
    ...card,
    boardTitle: board.board.title,
    watchers: [],
    isWatched: false,
    updates: [],
    files: [],
    activity: [],
  };
}

function setup(access: BoardAccess) {
  let board = boardWithFields();
  let saved: SavedBoardView = {
    id: "view-1",
    name: "Table",
    viewMode: "table",
    viewConfig: defaultTableViewConfig,
    isDefault: true,
    visibility: "board",
    createdBy: null,
    version: 1,
  };
  const getFullBoard = vi.fn(async () => board);
  const updateSavedViewConfig = vi.fn(
    async (_boardId: string, _viewId: string, next: ViewConfig) => {
      saved = { ...saved, viewConfig: next };
    },
  );
  const updateFieldValue = vi.fn(
    async (payload: {
      cardId: string;
      fieldDefinitionId: string;
      value: unknown;
    }) => {
      board = {
        ...board,
        groups: board.groups.map((group) => ({
          ...group,
          cards: group.cards.map((card) =>
            card.id === payload.cardId
              ? {
                  ...card,
                  fieldValues: {
                    ...card.fieldValues,
                    [payload.fieldDefinitionId]: payload.value,
                  },
                  computedValues:
                    payload.fieldDefinitionId === POINTS &&
                    typeof payload.value === "number"
                      ? {
                          [DOUBLE]: {
                            state: "ok" as const,
                            value: payload.value * 2,
                          },
                        }
                      : card.computedValues,
                }
              : card,
          ),
        })),
      };
    },
  );
  const services = {
    boards: {
      getFullBoard,
      listSavedViews: vi.fn(async () => [saved]),
      updateSavedViewConfig,
    },
    fieldOptions: {},
    access: { getBoardAccess: vi.fn(async () => access) },
    cards: {
      getCard: vi.fn(async (id: string) => detailOf(board, id)),
      getCardFiles: vi.fn(async () => []),
      getCardActivity: vi.fn(async () => []),
      updateFieldValue,
      updateCard: vi.fn(async () => undefined),
      updateStatus: vi.fn(async () => undefined),
    },
    comments: { getCardUpdates: vi.fn(async () => []) },
  } as unknown as WorkManagementServices;
  return { services, getFullBoard, updateFieldValue, updateSavedViewConfig };
}

function Providers({
  services,
  children,
}: {
  services: WorkManagementServices;
  children: ReactNode;
}) {
  return (
    <WorkManagementTestScope services={services}>
      <I18nProvider
        catalogs={[
          mainTableCatalog,
          kanbanLaneCatalog,
          itemSheetCatalog,
          statusOptionFormCatalog,
        ]}
        initialLocale="vi"
      >
        {children}
      </I18nProvider>
    </WorkManagementTestScope>
  );
}

function RoutedView() {
  const [filter, setFilter] = useState<string | undefined>();
  const [groupBy, setGroupBy] = useState<string | undefined>();
  const board = erpBoardScenario().board;
  return (
    <MainTableView
      boardId={board.id}
      workspaceId={board.workspaceId}
      filter={filter}
      onFilterChange={setFilter}
      groupBy={groupBy}
      onGroupByChange={setGroupBy}
    />
  );
}

function renderTable(access: BoardAccess = erpOwnerAccess()) {
  const fake = setup(access);
  const view = renderPureUi(
    <Providers services={fake.services}>
      <RoutedView />
    </Providers>,
  );
  return { ...view, ...fake };
}

const row = (key: string) =>
  document.querySelector<HTMLElement>(`[data-row-id="${erpItemId(key)}"]`)!;
const headerCells = () =>
  Array.from(document.querySelectorAll<HTMLElement>('[role="columnheader"]'));
/** The header's name without its ƒx mark. */
const headerName = (header: HTMLElement) =>
  (header.textContent ?? "").replace(/^ƒx/, "");
const headerOf = (column: string) =>
  headerCells().find((header) => headerName(header) === column)!;
const cellOf = (key: string, column: string) => {
  const index = headerCells().findIndex(
    (header) => headerName(header) === column,
  );
  return row(key).querySelectorAll<HTMLElement>('[role="cell"]')[index]!;
};
/** The Điểm text of every row, per group, in the order shown. */
const pointsByGroup = () =>
  Array.from(document.querySelectorAll<HTMLElement>('[role="rowgroup"]')).map(
    (group) =>
      Array.from(group.querySelectorAll<HTMLElement>("[data-row-id]")).map(
        (element) => {
          const index = headerCells().findIndex(
            (header) => headerName(header) === "Điểm",
          );
          const cells = element.querySelectorAll<HTMLElement>('[role="cell"]');
          return (cells[index]?.textContent?.trim() ?? "").replace("—", "");
        },
      ),
  );
const openMenu = (trigger: HTMLElement) =>
  fireEvent.keyDown(trigger, { key: "Enter" });
const edit = (key: string, name: string) =>
  within(cellOf(key, name)).getByRole("button", {
    name: new RegExp(`^Sửa ${name} của ${key}`),
  });

async function ready() {
  await screen.findByRole("rowgroup", { name: "Cần làm" });
}

describe("MainTableView custom field columns", () => {
  it("FUI[wm.table.main:field-columns] shows each custom field as a column with ƒx and one header lock where read-only", async () => {
    renderTable();
    await ready();
    expect(headerCells().map(headerName)).toEqual(
      expect.arrayContaining(["Ghi chú", "Điểm", "Điểm gấp đôi"]),
    );
    const double = headerOf("Điểm gấp đôi");
    expect(double.textContent).toContain("ƒx");
    expect(
      within(double).getByRole("img", {
        name: "Điểm gấp đôi: Tự tính, chỉ xem",
      }),
    ).toBeTruthy();
    expect(within(headerOf("Điểm")).queryByRole("img")).toBeNull();
    // Cells carry no lock of their own.
    expect(
      row("ERP-147").querySelectorAll('[role="img"][title*="Chỉ xem"]'),
    ).toHaveLength(0);
    expect(cellOf("ERP-147", "Điểm gấp đôi").textContent).toContain("26");
  });

  it("a viewer sees values and gets no editor; every field header carries its lock", async () => {
    renderTable(erpCommenterAccess());
    await ready();
    expect(cellOf("ERP-147", "Điểm").textContent).toContain("13");
    expect(
      within(cellOf("ERP-147", "Điểm")).queryByRole("button", { name: /^Sửa/ }),
    ).toBeNull();
    expect(
      within(headerOf("Điểm")).getByRole("img", {
        name: "Điểm: Chỉ xem, bạn không có quyền sửa trường này",
      }),
    ).toBeTruthy();
  });

  it("hides, shows and reorders a custom column through the view config", async () => {
    const { updateSavedViewConfig } = renderTable();
    await ready();
    openMenu(screen.getByRole("button", { name: /^Cột/ }));
    const menu = screen.getByRole("menu");
    const item = within(menu).getByRole("menuitemcheckbox", { name: "Điểm" });
    fireEvent.click(item);
    await waitFor(() =>
      expect(updateSavedViewConfig).toHaveBeenLastCalledWith(
        expect.any(String),
        "view-1",
        expect.objectContaining({
          hiddenFields: expect.arrayContaining([POINTS]),
        }),
      ),
    );
    await waitFor(() =>
      expect(
        headerCells().some((header) => headerName(header) === "Điểm"),
      ).toBe(false),
    );
    // Alt+Down moves the column; the order is saved in the view.
    const note = within(screen.getByRole("menu")).getByRole(
      "menuitemcheckbox",
      { name: "Ghi chú" },
    );
    fireEvent.keyDown(note, { key: "ArrowUp", altKey: true });
    await waitFor(() => {
      const order = updateSavedViewConfig.mock.calls.at(-1)![2].columnOrder;
      expect(order).toContain(NOTE);
      // One place earlier than its default spot, right after the date columns.
      expect(order.indexOf(NOTE)).toBe(order.indexOf("completed") - 1);
    });
  });

  it("sorts by a custom column with the registry's null rule, and saves the sort in the view", async () => {
    const { updateSavedViewConfig } = renderTable();
    await ready();
    openMenu(screen.getByRole("button", { name: /^Sắp xếp/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Điểm" }));
    await waitFor(() =>
      expect(updateSavedViewConfig).toHaveBeenLastCalledWith(
        expect.any(String),
        "view-1",
        expect.objectContaining({
          sortBy: [{ fieldId: POINTS, direction: "asc" }],
        }),
      ),
    );
    // Ascending: values in order, items without a value last.
    await waitFor(() => {
      const all = pointsByGroup();
      expect(all.some((values) => values.some((value) => value !== ""))).toBe(
        true,
      );
      for (const values of all) {
        const filled = values.filter((value) => value !== "");
        expect(filled).toEqual(
          [...filled].sort((a, b) => Number(a) - Number(b)),
        );
        expect(values.slice(0, filled.length)).toEqual(filled);
      }
    });
    // Descending flips the values and puts the empty ones first.
    openMenu(screen.getByRole("button", { name: /^Sắp xếp/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: /^Giảm dần/ }));
    await waitFor(() => {
      for (const values of pointsByGroup()) {
        const empty = values.filter((value) => value === "").length;
        expect(values.slice(0, empty).every((value) => value === "")).toBe(
          true,
        );
        const filled = values.slice(empty);
        expect(filled).toEqual(
          [...filled].sort((a, b) => Number(b) - Number(a)),
        );
      }
    });
  });
});

describe("MainTableView custom field writes", () => {
  it("writes the value with the item's version and shows the stale result until the board is read again", async () => {
    const { updateFieldValue, getFullBoard } = renderTable();
    await ready();
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => {
      release = resolve;
    });
    const before = getFullBoard.getMockImplementation()!;
    getFullBoard.mockImplementation(async () => {
      await held;
      return before();
    });
    fireEvent.click(edit("ERP-147", "Điểm"));
    const input = screen.getByRole("textbox", { name: /Điểm/ });
    fireEvent.change(input, { target: { value: "21" } });
    fireEvent.keyDown(input, { key: "Enter" });
    await waitFor(() => expect(updateFieldValue).toHaveBeenCalledTimes(1));
    expect(updateFieldValue.mock.calls[0]![0]).toEqual({
      cardId: erpItemId("ERP-147"),
      fieldDefinitionId: POINTS,
      value: 21,
    });
    // The source is written; the formula shows its old result as stale until the read comes back.
    await waitFor(() =>
      expect(
        cellOf("ERP-147", "Điểm gấp đôi")
          .querySelector("[data-computed-state]")!
          .getAttribute("data-computed-state"),
      ).toBe("stale"),
    );
    expect(cellOf("ERP-147", "Điểm gấp đôi").textContent).toContain("26");
    await act(async () => {
      release();
      await held;
    });
    await waitFor(() =>
      expect(cellOf("ERP-147", "Điểm gấp đôi").textContent).toContain("42"),
    );
    expect(
      cellOf("ERP-147", "Điểm gấp đôi")
        .querySelector("[data-computed-state]")!
        .getAttribute("data-computed-state"),
    ).toBe("value");
  });

  it("a custom Number cell does not move the item's own due date or assignees", async () => {
    const { updateFieldValue } = renderTable();
    await ready();
    const dueBefore = cellOf("ERP-147", "Hạn").textContent;
    fireEvent.click(edit("ERP-147", "Điểm"));
    fireEvent.change(screen.getByRole("textbox", { name: /Điểm/ }), {
      target: { value: "21" },
    });
    fireEvent.keyDown(screen.getByRole("textbox", { name: /Điểm/ }), {
      key: "Enter",
    });
    await waitFor(() => expect(updateFieldValue).toHaveBeenCalled());
    expect(cellOf("ERP-147", "Hạn").textContent).toBe(dueBefore);
  });

  it("a failed write keeps the typed value with an inline error and Thử lại, with no toast", async () => {
    const { updateFieldValue } = renderTable();
    await ready();
    updateFieldValue.mockRejectedValueOnce(new Error("offline"));
    fireEvent.click(edit("ERP-147", "Điểm"));
    const input = screen.getByRole("textbox", { name: /Điểm/ });
    fireEvent.change(input, { target: { value: "21" } });
    fireEvent.keyDown(input, { key: "Enter" });
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain("Chưa lưu được");
    expect(edit("ERP-147", "Điểm").textContent).toContain("21");
    expect(toast.error).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Thử lại" }));
    await waitFor(() => expect(updateFieldValue).toHaveBeenCalledTimes(2));
    expect(updateFieldValue.mock.calls[1]![0]).toMatchObject({ value: 21 });
    await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
    expect(edit("ERP-147", "Điểm").textContent).toContain("21");
  });

  it("a version conflict shows the in-cell choice; Giữ giá trị của tôi reads the board again and writes again", async () => {
    const { updateFieldValue, getFullBoard } = renderTable();
    await ready();
    updateFieldValue.mockRejectedValueOnce(
      Object.assign(new Error("conflict"), { kind: "conflict" }),
    );
    fireEvent.click(edit("ERP-147", "Điểm"));
    const input = screen.getByRole("textbox", { name: /Điểm/ });
    fireEvent.change(input, { target: { value: "21" } });
    fireEvent.keyDown(input, { key: "Enter" });
    const dialog = await screen.findByRole("alertdialog", {
      name: "Xung đột giá trị",
    });
    const reads = getFullBoard.mock.calls.length;
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Giữ giá trị của tôi" }),
    );
    await waitFor(() => expect(updateFieldValue).toHaveBeenCalledTimes(2));
    expect(getFullBoard.mock.calls.length).toBeGreaterThan(reads);
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
  });

  it("Dùng giá trị mới drops the typed value and shows the board's", async () => {
    const { updateFieldValue } = renderTable();
    await ready();
    updateFieldValue.mockRejectedValueOnce(
      Object.assign(new Error("conflict"), { kind: "conflict" }),
    );
    fireEvent.click(edit("ERP-147", "Điểm"));
    const input = screen.getByRole("textbox", { name: /Điểm/ });
    fireEvent.change(input, { target: { value: "21" } });
    fireEvent.keyDown(input, { key: "Enter" });
    const dialog = await screen.findByRole("alertdialog");
    fireEvent.click(
      within(dialog).getByRole("button", { name: /^Dùng giá trị mới/ }),
    );
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
    expect(edit("ERP-147", "Điểm").textContent).toContain("13");
    expect(updateFieldValue).toHaveBeenCalledTimes(1);
  });

  it("each failure toast takes its own id, so the next result is not swallowed", async () => {
    const { services } = renderTable();
    await ready();
    const cards = services.cards as unknown as {
      updateStatus: ReturnType<typeof vi.fn>;
    };
    cards.updateStatus.mockRejectedValue(new Error("forbidden"));
    for (let attempt = 0; attempt < 2; attempt += 1) {
      fireEvent.click(
        within(row("ERP-149")).getByRole("button", {
          name: "Đổi trạng thái của ERP-149",
        }),
      );
      fireEvent.click(screen.getByRole("option", { name: /Đang làm/ }));
      await waitFor(() =>
        expect(toast.error).toHaveBeenCalledTimes(attempt + 1),
      );
    }
    const ids = toast.error.mock.calls.map((call) => call[1].id);
    expect(new Set(ids).size).toBe(2);
  });
});
