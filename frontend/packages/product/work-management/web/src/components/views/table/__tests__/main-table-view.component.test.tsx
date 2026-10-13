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
  let view: { viewMode: "table"; viewConfig: ViewConfig } = {
    viewMode: "table",
    viewConfig: defaultTableViewConfig,
  };
  const boards = {
    getFullBoard: vi.fn(async () => board),
    getBoardView: vi.fn(async () => view),
    saveBoardView: vi.fn(
      async (
        _id: string,
        next: { viewMode: "table"; viewConfig: ViewConfig },
      ) => {
        view = next;
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
  };
  const services = {
    boards,
    access: { getBoardAccess: vi.fn(async () => access) },
    cards,
    comments: { getCardUpdates: vi.fn(async () => []) },
  } as unknown as WorkManagementServices;
  return { services, cards, boards };
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
        catalogs={[mainTableCatalog, kanbanLaneCatalog, itemSheetCatalog]}
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
    expect(cards.archiveCard).not.toHaveBeenCalled();

    await act(async () => {
      vi.advanceTimersByTime(ARCHIVE_UNDO_MS + 50);
    });
    await waitFor(() => expect(cards.archiveCard).toHaveBeenCalledTimes(2));
    expect(screen.queryByText("Đã lưu trữ 2 việc")).toBeNull();
  });

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
    await waitFor(() => expect(cards.updateCard).toHaveBeenCalledOnce());
    const [cardId, patch] = cards.updateCard.mock.calls[0] as unknown as [
      string,
      { dueDate: string },
    ];
    expect(cardId).toBe(erpItemId("ERP-151"));
    expect(new Date(patch.dueDate).getTime()).toBe(
      new Date("2026-11-03T17:00:00").getTime(),
    );
    // The selection is spent.
    expect(screen.queryByRole("toolbar")).toBeNull();
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

  it("saves the sort and the hidden columns with the view", async () => {
    const { boards } = renderTable(erpOwnerAccess());
    await ready();
    fireEvent.keyDown(screen.getByRole("button", { name: "Sắp xếp Hạn ↑" }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitemradio", { name: /^Ưu tiên/ }));
    await waitFor(() =>
      expect(boards.saveBoardView).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          viewConfig: expect.objectContaining({
            sortBy: [{ fieldId: "priority", direction: "asc" }],
          }),
        }),
      ),
    );
    expect(
      screen
        .getByRole("columnheader", { name: /^Ưu tiên/ })
        .getAttribute("aria-sort"),
    ).toBe("ascending");
  });

  it("saves a resized column with the view and drops it again on reset", async () => {
    const { boards } = renderTable(erpOwnerAccess());
    await ready();
    const handle = screen.getByRole("separator", {
      name: "Đổi độ rộng cột Tên công việc",
    });
    fireEvent.keyDown(handle, { key: "ArrowRight", shiftKey: true });
    await waitFor(() =>
      expect(boards.saveBoardView).toHaveBeenLastCalledWith(
        expect.any(String),
        expect.objectContaining({
          viewConfig: expect.objectContaining({
            columnWidths: { title: 372 },
          }),
        }),
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
      expect(boards.saveBoardView).toHaveBeenLastCalledWith(
        expect.any(String),
        expect.objectContaining({
          viewConfig: expect.objectContaining({ columnWidths: {} }),
        }),
      ),
    );
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
    expect(onItem).toHaveBeenCalledWith(erpItemId("ERP-142"));
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
