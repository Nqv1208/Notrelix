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
  FullBoardResponse,
  SavedBoardView,
  ViewConfig,
} from "@notrelix/work-management-core";
import {
  WorkManagementTestScope,
  applyItemStatus,
  defaultTableViewConfig,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import {
  ERP_STATUS_OPTION_IDS,
  erpBoardScenario,
  erpCommenterAccess,
  erpItemId,
  erpOwnerAccess,
} from "@notrelix/work-management-testing";
import { kanbanLaneCatalog } from "../../kanban/kanban-lane.catalog";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import { statusOptionFormCatalog } from "../../../status-option/status-option-form.catalog";
import { mainTableCatalog } from "../main-table.catalog";
import { ARCHIVE_UNDO_MS, MainTableView } from "../main-table-view";

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
  vi.useRealTimers();
});

function detailOf(board: FullBoardResponse, id: string): CardDetail {
  const card = board.groups
    .flatMap((group) => group.cards)
    .find((candidate) => candidate.id === id);
  if (!card) throw new Error(`no card ${id}`);
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

function fakeServices(access: BoardAccess) {
  let board = erpBoardScenario();
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
  const boards = {
    getFullBoard: vi.fn(async () => board),
    listSavedViews: vi.fn(async () => [saved]),
    updateSavedViewConfig: vi.fn(
      async (_boardId: string, _viewId: string, next: ViewConfig) => {
        saved = { ...saved, viewConfig: next };
      },
    ),
  };
  const cards = {
    getCard: vi.fn(async (id: string) => detailOf(board, id)),
    getCardFiles: vi.fn(async () => []),
    getCardActivity: vi.fn(async () => []),
    updateStatus: vi.fn(async (id: string, optionId: string) => {
      board = applyItemStatus(board, id, optionId) ?? board;
    }),
    createCard: vi.fn(async () => "item-new"),
    moveCard: vi.fn(async () => undefined),
    updateFieldValue: vi.fn(async () => undefined),
    updateCard: vi.fn(async () => undefined),
    deleteCard: vi.fn(async () => undefined),
    archiveCard: vi.fn(async (id: string) => {
      board = {
        ...board,
        groups: board.groups.map((group) => ({
          ...group,
          cards: group.cards.filter((item) => item.id !== id),
        })),
      };
    }),
    duplicateCard: vi.fn(async () => undefined),
    // One request for several rows (all or nothing).
    bulkUpdate: vi.fn(
      async (
        _boardId: string,
        itemIds: readonly string[],
        change: { type: string },
      ) => {
        if (change.type === "archive") {
          board = {
            ...board,
            groups: board.groups.map((group) => ({
              ...group,
              cards: group.cards.filter((item) => !itemIds.includes(item.id)),
            })),
          };
        }
        return itemIds.length;
      },
    ),
  };
  const fieldOptions = {
    addOption: vi.fn(
      async (input: {
        fieldId: string;
        name: string;
        color: string;
        category?: string;
      }) => {
        board = {
          ...board,
          board: {
            ...board.board,
            fieldDefinitions: board.board.fieldDefinitions.map((field) =>
              field.id === input.fieldId
                ? {
                    ...field,
                    version: (field.version ?? 1) + 1,
                    options: [
                      ...field.options,
                      {
                        id: `option-${field.options.length + 1}`,
                        label: input.name,
                        color: input.color,
                        position: "zz",
                        ...(input.category
                          ? { category: input.category as "done" }
                          : {}),
                      },
                    ],
                  }
                : field,
            ),
          },
        };
      },
    ),
    updateOption: vi.fn(async () => undefined),
    removeOption: vi.fn(async () => undefined),
    moveOption: vi.fn(async () => undefined),
  };
  const services = {
    boards,
    fieldOptions,
    access: { getBoardAccess: vi.fn(async () => access) },
    cards,
    comments: { getCardUpdates: vi.fn(async () => []) },
  } as unknown as WorkManagementServices;
  return { services, cards, boards, fieldOptions };
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

/** Holds `item`, `filter` and `groupBy` like the route does. */
function RoutedView({
  initialItem,
  initialFilter,
  onItem,
}: {
  initialItem?: string;
  initialFilter?: string;
  onItem?: (id: string | undefined) => void;
}) {
  const [item, setItem] = useState(initialItem);
  const [filter, setFilter] = useState(initialFilter);
  const [groupBy, setGroupBy] = useState<string | undefined>();
  const board = erpBoardScenario().board;
  return (
    <MainTableView
      boardId={board.id}
      workspaceId={board.workspaceId}
      selectedItemId={item}
      onSelectedItemChange={(id) => {
        onItem?.(id);
        setItem(id);
      }}
      filter={filter}
      onFilterChange={setFilter}
      groupBy={groupBy}
      onGroupByChange={setGroupBy}
    />
  );
}

function renderTable(
  access: BoardAccess,
  props: Parameters<typeof RoutedView>[0] = {},
) {
  const fake = fakeServices(access);
  const view = renderPureUi(
    <Providers services={fake.services}>
      <RoutedView {...props} />
    </Providers>,
  );
  return { ...view, ...fake };
}

const group = (name: string) => screen.getByRole("rowgroup", { name });
const rowsOf = (name: string) =>
  group(name).querySelectorAll("[data-row-id]").length;
const row = (key: string) =>
  document.querySelector<HTMLElement>(`[data-row-id="${erpItemId(key)}"]`);

async function ready() {
  await screen.findByRole("rowgroup", { name: "Cần làm" });
}

describe("MainTableView on the ERP board", () => {
  it("shows the open items in three groups and counts the hidden ones", async () => {
    renderTable(erpOwnerAccess());
    await ready();
    expect(
      ["Cần làm", "Đang làm", "Chờ duyệt"].map((name) => rowsOf(name)),
    ).toEqual([6, 3, 2]);
    expect(screen.queryByRole("rowgroup", { name: "Hoàn thành" })).toBeNull();
    expect(
      screen.getByText("11 công việc đang hiện · 5 ẩn bởi bộ lọc"),
    ).toBeTruthy();
  });

  it("shows the finished group when Hiện việc đã xong is chosen", async () => {
    renderTable(erpOwnerAccess());
    await ready();
    fireEvent.click(screen.getByRole("button", { name: "Hiện việc đã xong" }));
    expect(rowsOf("Hoàn thành")).toBe(5);
    expect(screen.queryByText("Trạng thái: chưa xong")).toBeNull();
    expect(
      screen.getByText("16 công việc đang hiện · 0 ẩn bởi bộ lọc"),
    ).toBeTruthy();
  });

  it("moves the row to its group when the status changes and writes the status", async () => {
    const { cards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.click(
      within(row("ERP-149")!).getByRole("button", {
        name: "Đổi trạng thái của ERP-149",
      }),
    );
    fireEvent.click(screen.getByRole("option", { name: /Đang làm/ }));
    await waitFor(() => expect(rowsOf("Đang làm")).toBe(4));
    expect(rowsOf("Cần làm")).toBe(5);
    expect(cards.updateStatus).toHaveBeenCalledWith(
      erpItemId("ERP-149"),
      ERP_STATUS_OPTION_IDS.doing,
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
  });

  it("puts the row back and says so when the status write fails", async () => {
    const { cards } = renderTable(erpOwnerAccess());
    cards.updateStatus.mockRejectedValueOnce(new Error("forbidden"));
    await ready();
    fireEvent.click(
      within(row("ERP-149")!).getByRole("button", {
        name: "Đổi trạng thái của ERP-149",
      }),
    );
    fireEvent.click(screen.getByRole("option", { name: /Đang làm/ }));
    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(toast.error.mock.calls[0]?.[0]).toBe(
      "Không đổi được trạng thái của “Viết tài liệu quy trình nhập hoá đơn đầu vào”.",
    );
    await waitFor(() => expect(rowsOf("Cần làm")).toBe(6));
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("archives selected rows after the undo window, not before", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const { cards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-142" }));
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-146" }));
    fireEvent.click(screen.getByRole("button", { name: "Lưu trữ" }));

    // Rows leave the table at once; the write waits for the undo window.
    expect(row("ERP-142")).toBeNull();
    expect(row("ERP-146")).toBeNull();
    expect(rowsOf("Đang làm")).toBe(1);
    expect(screen.getByText("Đã lưu trữ 2 việc")).toBeTruthy();
    expect(cards.bulkUpdate).not.toHaveBeenCalled();

    await act(async () => {
      vi.advanceTimersByTime(ARCHIVE_UNDO_MS + 50);
    });
    // Both rows go in one request.
    await waitFor(() => expect(cards.bulkUpdate).toHaveBeenCalledTimes(1));
    expect(cards.archiveCard).not.toHaveBeenCalled();
    expect(cards.bulkUpdate).toHaveBeenCalledWith(
      expect.any(String),
      [erpItemId("ERP-142"), erpItemId("ERP-146")],
      { type: "archive" },
    );
    expect(screen.queryByText("Đã lưu trữ 2 việc")).toBeNull();
  }, 20_000);

  it("brings the rows back and sends nothing when the archive is undone", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const { cards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-142" }));
    fireEvent.click(screen.getByRole("button", { name: "Lưu trữ" }));
    expect(row("ERP-142")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Hoàn tác" }));
    expect(row("ERP-142")).not.toBeNull();
    await act(async () => {
      vi.advanceTimersByTime(ARCHIVE_UNDO_MS + 50);
    });
    expect(cards.archiveCard).not.toHaveBeenCalled();
  });

  it("sets the due date of the selected rows", async () => {
    const { cards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-151" }));
    fireEvent.click(screen.getByRole("button", { name: "Đặt hạn" }));
    fireEvent.change(screen.getByLabelText("Hạn"), {
      target: { value: "2026-11-03" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Áp dụng" }));
    await waitFor(() => expect(cards.bulkUpdate).toHaveBeenCalledOnce());
    expect(cards.updateCard).not.toHaveBeenCalled();
    const [, itemIds, change] = cards.bulkUpdate.mock.calls[0] as unknown as [
      string,
      string[],
      { type: string; dueDate: string },
    ];
    expect(itemIds).toEqual([erpItemId("ERP-151")]);
    expect(change.type).toBe("set-due-date");
    expect(new Date(change.dueDate).getTime()).toBe(
      new Date("2026-11-03T17:00:00").getTime(),
    );
    // The selection is spent.
    expect(screen.queryByRole("toolbar")).toBeNull();
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("changes the status of several selected rows in one request", async () => {
    const { cards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-151" }));
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-149" }));
    const bar = screen.getByRole("toolbar");
    fireEvent.keyDown(within(bar).getByRole("button", { name: /Trạng thái/ }), {
      key: "Enter",
    });
    fireEvent.click(
      await screen.findByRole("menuitem", { name: /Hoàn thành/ }),
    );
    await waitFor(() => expect(cards.bulkUpdate).toHaveBeenCalledOnce());
    const [, itemIds, change] = cards.bulkUpdate.mock.calls[0] as unknown as [
      string,
      string[],
      { type: string; statusOptionId: string },
    ];
    expect(new Set(itemIds)).toEqual(
      new Set([erpItemId("ERP-151"), erpItemId("ERP-149")]),
    );
    expect(change.type).toBe("set-status");
    // No per-row status write was made.
    expect(cards.updateStatus).not.toHaveBeenCalled();
  }, 20_000);

  it("says nothing was changed when the batch is refused", async () => {
    const { cards } = renderTable(erpOwnerAccess());
    cards.bulkUpdate.mockRejectedValueOnce(new Error("rejected"));
    await ready();
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-151" }));
    fireEvent.click(screen.getByRole("button", { name: "Đặt hạn" }));
    fireEvent.change(screen.getByLabelText("Hạn"), {
      target: { value: "2026-11-03" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Áp dụng" }));
    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(toast.error.mock.calls.at(-1)?.[0]).toContain(
      "Chưa có gì bị thay đổi",
    );
  });

  it("applies the route filter and says when nothing matches", async () => {
    renderTable(erpOwnerAccess(), { initialFilter: "p.low~o" });
    await screen.findByText("Không có việc khớp bộ lọc");
    expect(
      screen.getByText("0 công việc đang hiện · 16 ẩn bởi bộ lọc"),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Bỏ lọc" }));
    await ready();
    expect(rowsOf("Cần làm")).toBe(6);
  });

  it("saves the hidden columns with the view", async () => {
    const { boards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.keyDown(screen.getByRole("button", { name: /^Cột \d+\/\d+/ }), {
      key: "Enter",
    });
    fireEvent.click(
      await screen.findByRole("menuitemcheckbox", { name: "Bắt đầu" }),
    );
    await waitFor(() =>
      expect(boards.updateSavedViewConfig).toHaveBeenCalledWith(
        expect.any(String),
        "view-1",
        expect.objectContaining({
          hiddenFields: expect.not.arrayContaining(["start"]),
        }),
      ),
    );
  });

  it("saves a resized column with the view and drops it again on reset", async () => {
    const { boards } = renderTable(erpOwnerAccess());
    await ready();
    const handle = screen.getByRole("separator", {
      name: "Đổi độ rộng cột Tên công việc",
    });
    fireEvent.keyDown(handle, { key: "ArrowRight", shiftKey: true });
    await waitFor(() =>
      expect(boards.updateSavedViewConfig).toHaveBeenLastCalledWith(
        expect.any(String),
        "view-1",
        expect.objectContaining({ columnWidths: { title: 372 } }),
      ),
    );
    await waitFor(() =>
      expect(
        screen
          .getByRole("separator", { name: "Đổi độ rộng cột Tên công việc" })
          .getAttribute("aria-valuenow"),
      ).toBe("372"),
    );
    fireEvent.keyDown(
      screen.getByRole("separator", { name: "Đổi độ rộng cột Tên công việc" }),
      { key: "Home" },
    );
    await waitFor(() =>
      expect(boards.updateSavedViewConfig).toHaveBeenLastCalledWith(
        expect.any(String),
        "view-1",
        expect.objectContaining({ columnWidths: {} }),
      ),
    );
  });

  it("adds a status as a new group at the end of the table", async () => {
    const { fieldOptions } = renderTable(erpOwnerAccess());
    await ready();
    expect(screen.getAllByRole("rowgroup")).toHaveLength(3);
    fireEvent.click(screen.getByRole("button", { name: "Thêm trạng thái" }));
    const name = screen.getByRole("textbox", { name: "Tên trạng thái" });
    fireEvent.change(name, { target: { value: "Kiểm thử" } });
    fireEvent.click(screen.getByRole("radio", { name: "Hoàn thành" }));
    fireEvent.submit(name.closest("form")!);
    await waitFor(() =>
      expect(fieldOptions.addOption).toHaveBeenCalledWith(
        expect.objectContaining({
          name: "Kiểm thử",
          category: "done",
          expectedVersion: expect.any(Number),
        }),
        expect.anything(),
      ),
    );
    // The new status is an empty group of the table, after the others.
    await waitFor(() =>
      expect(
        screen
          .getAllByRole("rowgroup")
          .map((g) => g.getAttribute("aria-label")),
      ).toContain("Kiểm thử"),
    );
    expect(
      screen.queryByRole("textbox", { name: "Tên trạng thái" }),
    ).toBeNull();
  });

  it("moves a status group down from its menu and writes the move between its new neighbours", async () => {
    const { fieldOptions } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.keyDown(
      screen.getByRole("button", { name: "Tuỳ chọn nhóm Đang làm" }),
      { key: "Enter" },
    );
    fireEvent.click(
      screen.getByRole("menuitem", { name: "Chuyển nhóm xuống" }),
    );
    await waitFor(() =>
      expect(fieldOptions.moveOption).toHaveBeenCalledTimes(1),
    );
    const [input] = fieldOptions.moveOption.mock.calls[0] as unknown as [
      {
        optionId: string;
        previousSiblingId: string | null;
        nextSiblingId: string | null;
        expectedVersion: number;
      },
    ];
    expect(input.previousSiblingId).not.toBeNull();
    expect(input.nextSiblingId).not.toBeNull();
    expect(input.expectedVersion).toBeGreaterThan(0);
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("deletes a status after asking where its items go and writes that replacement", async () => {
    const { fieldOptions } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.keyDown(
      screen.getByRole("button", { name: "Tuỳ chọn nhóm Chờ duyệt" }),
      { key: "Enter" },
    );
    fireEvent.click(screen.getByRole("menuitem", { name: "Xoá trạng thái…" }));
    const dialog = await screen.findByRole("alertdialog");
    fireEvent.click(within(dialog).getByRole("radio", { name: "Đang làm" }));
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Chuyển việc và xoá" }),
    );
    await waitFor(() =>
      expect(fieldOptions.removeOption).toHaveBeenCalledTimes(1),
    );
    expect(fieldOptions.removeOption).toHaveBeenCalledWith(
      expect.objectContaining({
        replacementOptionId: expect.any(String),
        expectedVersion: expect.any(Number),
      }),
      expect.anything(),
    );
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
  }, 20_000);

  it("gives a commenter no group menu", async () => {
    renderTable(erpCommenterAccess());
    await ready();
    expect(
      screen.queryByRole("button", { name: "Tuỳ chọn nhóm Đang làm" }),
    ).toBeNull();
  });

  it("gives a commenter no add-status control", async () => {
    renderTable(erpCommenterAccess());
    await ready();
    expect(
      screen.queryByRole("button", { name: "Thêm trạng thái" }),
    ).toBeNull();
  });

  it("opens the item sheet through the route item", async () => {
    const onItem = vi.fn();
    renderTable(erpOwnerAccess(), { onItem });
    await ready();
    fireEvent.click(
      within(row("ERP-142")!).getByRole("button", {
        name: /Chuyển dữ liệu khách hàng/,
      }),
    );
    // An editor's click waits out a possible double click (rename).
    await waitFor(() =>
      expect(onItem).toHaveBeenCalledWith(erpItemId("ERP-142")),
    );
    await screen.findByRole("dialog", {
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    });
  });

  it("gives a commenter no add, no status edit and no archive", async () => {
    renderTable(erpCommenterAccess());
    await ready();
    await waitFor(() =>
      expect(
        (
          within(row("ERP-142")!).getByRole("button", {
            name: "Đổi trạng thái của ERP-142",
          }) as HTMLButtonElement
        ).disabled,
      ).toBe(true),
    );
    expect(screen.queryByRole("button", { name: /^Thêm vào / })).toBeNull();
    expect(screen.queryByRole("button", { name: "Thêm công việc" })).toBeNull();
    fireEvent.click(screen.getByRole("checkbox", { name: "Chọn ERP-142" }));
    expect(
      (screen.getByRole("button", { name: "Lưu trữ" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });

  it("adds an item into a group through the status write", async () => {
    const { cards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.click(screen.getByRole("button", { name: "Thêm vào Đang làm" }));
    const input = screen.getByRole("textbox", {
      name: "Tên việc mới trong Đang làm",
    });
    fireEvent.change(input, { target: { value: "Rà soát quyền kế toán" } });
    fireEvent.submit(input.closest("form")!);
    await waitFor(() =>
      expect(cards.updateStatus).toHaveBeenCalledWith(
        "item-new",
        ERP_STATUS_OPTION_IDS.doing,
        expect.any(Object),
      ),
    );
    expect(cards.createCard).toHaveBeenCalledWith(
      erpBoardScenario().board.id,
      expect.objectContaining({ title: "Rà soát quyền kế toán" }),
      expect.any(Object),
    );
  });
});
