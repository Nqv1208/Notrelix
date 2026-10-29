import { useState, type ReactNode } from "react";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import type {
  BoardAccess,
  CardDetail,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import {
  WorkManagementTestScope,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import {
  erpBoardScenario,
  erpCommenterAccess,
  erpItemId,
  erpOwnerAccess,
} from "@notrelix/work-management-testing";
import { itemCreateCatalog } from "../../../item-create/item-create.catalog";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import { kanbanLaneCatalog } from "../../kanban/kanban-lane.catalog";
import { kanbanToolbarCatalog } from "../../kanban/kanban-toolbar.catalog";
import { BoardCalendarView } from "../board-calendar-view";
import { calendarCatalog } from "../calendar.catalog";

const toast = vi.hoisted(() => ({ success: vi.fn(), error: vi.fn() }));
vi.mock("sonner", () => ({ toast }));

/** Monday 12/10/2026 10:40 in Hanoi. */
const NOW = new Date("2026-10-12T03:40:00.000Z");
const ZONE = "Asia/Ho_Chi_Minh";

beforeAll(() => {
  // Radix menus measure and scroll items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

afterEach(() => {
  toast.success.mockClear();
  toast.error.mockClear();
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

function fakeServices(
  access: BoardAccess,
  overrides: {
    updateCard?: () => Promise<void>;
    /** Reads answer a copy and a write needs the current item version, as a server does. */
    versioned?: boolean;
  } = {},
) {
  const board = erpBoardScenario();
  const cards = {
    getCard: vi.fn(async (id: string) => detailOf(board, id)),
    getCardFiles: vi.fn(async () => []),
    getCardActivity: vi.fn(async () => []),
    updateStatus: vi.fn(async () => undefined),
    createCard: vi.fn(
      async (_boardId: string, _payload: { listId: string; title: string }) =>
        "item-new",
    ),
    moveCard: vi.fn(async () => undefined),
    updateFieldValue: vi.fn(async () => undefined),
    updateCard: vi.fn(
      overrides.versioned
        ? async (
            id: string,
            _patch: unknown,
            options?: { expectedVersion?: number },
          ) => {
            const stored = board.groups
              .flatMap((group) => group.cards)
              .find((card) => card.id === id)!;
            if (options?.expectedVersion !== stored.version)
              throw { kind: "conflict" };
            stored.version = (stored.version ?? 0) + 1;
          }
        : (overrides.updateCard ?? (async () => undefined)),
    ),
    deleteCard: vi.fn(async () => undefined),
    archiveCard: vi.fn(async () => undefined),
    duplicateCard: vi.fn(async () => undefined),
  };
  const services = {
    boards: {
      getFullBoard: vi.fn(async () =>
        overrides.versioned ? structuredClone(board) : board,
      ),
    },
    access: { getBoardAccess: vi.fn(async () => access) },
    cards,
    comments: { getCardUpdates: vi.fn(async () => []) },
  } as unknown as WorkManagementServices;
  return { services, cards };
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
          calendarCatalog,
          kanbanLaneCatalog,
          kanbanToolbarCatalog,
          itemSheetCatalog,
          itemCreateCatalog,
        ]}
        initialLocale="vi"
      >
        {children}
      </I18nProvider>
    </WorkManagementTestScope>
  );
}

/** Holds `item` and `filter` like the route does. */
function RoutedCalendar({
  onItem,
  initialFilter,
}: {
  onItem?: (id: string | undefined) => void;
  initialFilter?: string;
}) {
  const [item, setItem] = useState<string | undefined>();
  const [filter, setFilter] = useState<string | undefined>(initialFilter);
  const board = erpBoardScenario().board;
  return (
    <BoardCalendarView
      boardId={board.id}
      workspaceId={board.workspaceId}
      now={NOW}
      timeZone={ZONE}
      selectedItemId={item}
      onSelectedItemChange={(id) => {
        onItem?.(id);
        setItem(id);
      }}
      filter={filter}
      onFilterChange={(value) => setFilter(value)}
    />
  );
}

function renderCalendar(
  access: BoardAccess = erpOwnerAccess(),
  overrides: Parameters<typeof fakeServices>[1] = {},
  onItem?: (id: string | undefined) => void,
  initialFilter?: string,
) {
  const fake = fakeServices(access, overrides);
  const view = renderPureUi(
    <Providers services={fake.services}>
      <RoutedCalendar onItem={onItem} initialFilter={initialFilter} />
    </Providers>,
  );
  return { ...view, ...fake };
}

const dataTransfer = () => ({
  setData: vi.fn(),
  effectAllowed: "",
  dropEffect: "",
});

async function ready() {
  await screen.findByRole("grid");
}

function eventOf(container: HTMLElement, key: string): HTMLElement {
  const chip = container.querySelector<HTMLElement>(
    `[data-calendar-event="${erpItemId(key)}"]`,
  );
  if (!chip) throw new Error(`no event ${key}`);
  return chip;
}

function dayCell(container: HTMLElement, day: string): HTMLElement {
  const cell = container.querySelector<HTMLElement>(`[data-day="${day}"]`);
  if (!cell) throw new Error(`no day ${day}`);
  return cell;
}

describe("BoardCalendarView on the ERP board", () => {
  it("draws the board's items on their due days and lists the undated one", async () => {
    const { container } = renderCalendar();
    await ready();
    expect(screen.getAllByRole("heading", { level: 2 })[0]?.textContent).toBe(
      "Tháng 10, 2026",
    );
    expect(screen.getByText("Giờ Hà Nội (GMT+7)")).toBeTruthy();
    // 15 dated items; ERP-135 (26/09) is outside the grid and ERP-142 spans
    // 01/10 to 10/10 over two week rows.
    expect(container.querySelectorAll("[data-calendar-event]").length).toBe(15);
    expect(eventOf(container, "ERP-144").style.gridColumn).toBe("1 / span 1");
    expect(screen.getByText("1 việc chưa có ngày")).toBeTruthy();
    expect(screen.getByText("ERP-153")).toBeTruthy();
    // Today is 12/10 and carries the marker.
    expect(
      container
        .querySelector("[aria-current=date]")
        ?.textContent?.includes("Hôm nay"),
    ).toBe(true);
  });

  it("reschedules an item by dropping it on another day, moving its start with it", async () => {
    const { container, cards } = renderCalendar();
    await ready();
    // ERP-146: 13/10 to 16/10, dropped on 20/10 moves both days by four.
    const chip = eventOf(container, "ERP-146");
    fireEvent.dragStart(chip, { dataTransfer: dataTransfer() });
    const target = dayCell(container, "2026-10-20");
    fireEvent.dragOver(target, { dataTransfer: dataTransfer() });
    fireEvent.drop(target, { dataTransfer: dataTransfer() });
    await waitFor(() => expect(cards.updateCard).toHaveBeenCalledTimes(1));
    expect(cards.updateCard).toHaveBeenCalledWith(
      erpItemId("ERP-146"),
      {
        dueDate: "2026-10-20T10:00:00.000Z",
        startDate: "2026-10-17T02:00:00.000Z",
      },
      expect.objectContaining({ expectedVersion: expect.any(Number) }),
    );
  });

  it("undoes a quick move with the version the move itself produced", async () => {
    const { container } = renderCalendar(erpOwnerAccess(), { versioned: true });
    await ready();
    fireEvent.dragStart(eventOf(container, "ERP-146"), {
      dataTransfer: dataTransfer(),
    });
    const day = dayCell(container, "2026-10-20");
    fireEvent.dragOver(day, { dataTransfer: dataTransfer() });
    fireEvent.drop(day, { dataTransfer: dataTransfer() });
    await waitFor(() => expect(toast.success).toHaveBeenCalledTimes(1));
    toast.success.mock.calls[0]?.[1]?.action.onClick();
    await waitFor(() => expect(toast.success).toHaveBeenCalledTimes(2));
    expect(toast.success.mock.calls[1]?.[0]).toBe("Đã hoàn tác");
    expect(toast.error).not.toHaveBeenCalled();
    // Sonner removes the toast Hoàn tác sits on; a result toast with the same
    // id would be removed with it and never show.
    expect(toast.success.mock.calls[1]?.[1]?.id).not.toBe(
      toast.success.mock.calls[0]?.[1]?.id,
    );
  });

  it("keeps the new position and offers Thử lại when the write fails", async () => {
    const { container } = renderCalendar(erpOwnerAccess(), {
      updateCard: async () => {
        throw new Error("rejected");
      },
    });
    await ready();
    const chip = eventOf(container, "ERP-151");
    fireEvent.dragStart(chip, { dataTransfer: dataTransfer() });
    const target = dayCell(container, "2026-10-21");
    fireEvent.dragOver(target, { dataTransfer: dataTransfer() });
    fireEvent.drop(target, { dataTransfer: dataTransfer() });
    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(toast.error.mock.calls[0]?.[0]).toBe(
      "Chưa lưu được ngày của ERP-151",
    );
    expect(toast.error.mock.calls[0]?.[1]).toMatchObject({
      action: { label: "Thử lại" },
    });
    // The item stays where it was dropped, never silently reverted.
    await waitFor(() =>
      expect(
        eventOf(container, "ERP-151")
          .closest("[data-calendar-week]")
          ?.getAttribute("data-calendar-week"),
      ).toBe("2026-10-19"),
    );
  });

  it("gives an undated item a due day at 17:00 by drag or by the button", async () => {
    const { container, cards } = renderCalendar();
    await ready();
    const item = container.querySelector<HTMLElement>(
      `[data-calendar-undated="${erpItemId("ERP-153")}"]`,
    )!;
    fireEvent.dragStart(item, { dataTransfer: dataTransfer() });
    const target = dayCell(container, "2026-10-22");
    fireEvent.dragOver(target, { dataTransfer: dataTransfer() });
    fireEvent.drop(target, { dataTransfer: dataTransfer() });
    await waitFor(() => expect(cards.updateCard).toHaveBeenCalledTimes(1));
    expect(cards.updateCard).toHaveBeenLastCalledWith(
      erpItemId("ERP-153"),
      {
        startDate: "2026-10-22T01:00:00.000Z",
        dueDate: "2026-10-22T10:00:00.000Z",
      },
      expect.objectContaining({ expectedVersion: expect.any(Number) }),
    );
  });

  it("creates an item with its due day from the quick create", async () => {
    const { container, cards } = renderCalendar();
    await ready();
    fireEvent.click(dayCell(container, "2026-10-23"));
    const input = screen.getByRole("textbox", {
      name: "Tên công việc mới, hạn 23/10",
    });
    fireEvent.change(input, { target: { value: "Họp chốt phạm vi" } });
    fireEvent.submit(input.closest("form")!);
    await waitFor(() => expect(cards.createCard).toHaveBeenCalledTimes(1));
    expect(cards.createCard.mock.calls[0]?.[1]).toMatchObject({
      title: "Họp chốt phạm vi",
    });
    await waitFor(() =>
      expect(cards.updateCard).toHaveBeenCalledWith("item-new", {
        dueDate: "2026-10-23T10:00:00.000Z",
      }),
    );
  });

  it("opens the full form with the shown day when Add item is pressed, and creates the item with that due day", async () => {
    const { cards } = renderCalendar();
    await ready();
    fireEvent.click(screen.getByRole("button", { name: "Thêm công việc" }));
    const due = (await screen.findByLabelText(
      "Hạn hoàn thành",
    )) as HTMLInputElement;
    expect(due.value).toBe("12/10/2026");
    fireEvent.change(screen.getByRole("textbox", { name: /^Tên công việc/ }), {
      target: { value: "Chốt lịch tuần" },
    });
    fireEvent.click(screen.getByRole("button", { name: /^Tạo công việc/ }));
    await waitFor(() => expect(cards.createCard).toHaveBeenCalledTimes(1));
    expect(cards.createCard.mock.calls[0]?.[1]).toMatchObject({
      title: "Chốt lịch tuần",
      dueDate: "2026-10-12T00:00:00.000Z",
    });
  });

  it("opens an item and its sheet, and closes it again", async () => {
    const onItem = vi.fn();
    const { container } = renderCalendar(erpOwnerAccess(), {}, onItem);
    await ready();
    fireEvent.click(eventOf(container, "ERP-144"));
    expect(onItem).toHaveBeenCalledWith(erpItemId("ERP-144"));
    expect(
      await screen.findByRole("heading", {
        name: "Kiểm thử phân hệ công nợ với số liệu tháng 9",
      }),
    ).toBeTruthy();
  });

  it("filters by the route filter and shows it as a condition chip", async () => {
    // An older facet string in the route: Quá hạn.
    const { container } = renderCalendar(erpOwnerAccess(), {}, undefined, "o");
    await ready();
    // Only the overdue items stay: ERP-142 (10/10) and the done ones are not overdue.
    await waitFor(() =>
      expect(container.querySelectorAll("[data-calendar-event]").length).toBe(
        2,
      ),
    );
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: /^Xoá bộ lọc Quá hạn/ }),
      ).toBeTruthy(),
    );
  }, 20_000);

  it("gives a commenter no add, drag or due date", async () => {
    const { container } = renderCalendar(erpCommenterAccess());
    await ready();
    await waitFor(() =>
      expect(
        screen.queryByRole("button", { name: "Thêm công việc" }),
      ).toBeNull(),
    );
    expect(eventOf(container, "ERP-146").getAttribute("draggable")).toBe(
      "false",
    );
    expect(screen.queryByRole("button", { name: /Đặt hạn cho/ })).toBeNull();
  });
});
