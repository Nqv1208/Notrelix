import { useState, type ReactNode } from "react";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import {
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
import { BoardTimelineView } from "../board-timeline-view";
import { timelineCatalog } from "../timeline.catalog";

const toast = vi.hoisted(() => ({ success: vi.fn(), error: vi.fn() }));
vi.mock("sonner", () => ({ toast }));

/** Monday 12/10/2026 10:40 in Hanoi. */
const NOW = new Date("2026-10-12T03:40:00.000Z");
const ZONE = "Asia/Ho_Chi_Minh";

beforeAll(() => {
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
    addDependency?: () => Promise<void>;
  } = {},
) {
  const board = erpBoardScenario();
  const cards = {
    getCard: vi.fn(async (id: string) => detailOf(board, id)),
    getCardFiles: vi.fn(async () => []),
    getCardActivity: vi.fn(async () => []),
    updateStatus: vi.fn(async () => undefined),
    createCard: vi.fn(async () => "item-new"),
    moveCard: vi.fn(async () => undefined),
    updateFieldValue: vi.fn(async () => undefined),
    updateCard: vi.fn(overrides.updateCard ?? (async () => undefined)),
    deleteCard: vi.fn(async () => undefined),
    archiveCard: vi.fn(async () => undefined),
    duplicateCard: vi.fn(async () => undefined),
  };
  const extras = {
    addDependency: vi.fn(overrides.addDependency ?? (async () => undefined)),
    removeDependency: vi.fn(async () => true),
  };
  const services = {
    boards: { getFullBoard: vi.fn(async () => board) },
    access: { getBoardAccess: vi.fn(async () => access) },
    cards,
    comments: { getCardUpdates: vi.fn(async () => []) },
    itemExtras: {
      ...extras,
      getPlan: vi.fn(async () => ({
        items: [],
        links: [],
        percents: {},
        groups: {},
      })),
    },
  } as unknown as WorkManagementServices;
  return { services, cards, extras };
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
          timelineCatalog,
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

/** Holds `item` like the route does. */
function RoutedTimeline({
  onItem,
  onOpenCalendar,
}: {
  onItem?: (id: string | undefined) => void;
  onOpenCalendar?: () => void;
}) {
  const [item, setItem] = useState<string | undefined>();
  const board = erpBoardScenario().board;
  return (
    <BoardTimelineView
      boardId={board.id}
      workspaceId={board.workspaceId}
      now={NOW}
      timeZone={ZONE}
      selectedItemId={item}
      onSelectedItemChange={(id) => {
        onItem?.(id);
        setItem(id);
      }}
      onOpenCalendar={onOpenCalendar}
    />
  );
}

function renderTimeline(
  access: BoardAccess = erpOwnerAccess(),
  overrides: Parameters<typeof fakeServices>[1] = {},
  props: {
    onItem?: (id: string | undefined) => void;
    onOpenCalendar?: () => void;
  } = {},
) {
  const fake = fakeServices(access, overrides);
  const view = renderPureUi(
    <Providers services={fake.services}>
      <RoutedTimeline {...props} />
    </Providers>,
  );
  return { ...view, ...fake };
}

async function ready() {
  await screen.findByRole("grid");
}

const listRow = (container: HTMLElement, key: string) => {
  const element = container.querySelector<HTMLElement>(
    `[data-tl="lrow"][data-row-id="${erpItemId(key)}"]`,
  );
  if (!element) throw new Error(`no row ${key}`);
  return element;
};

const dueOf = (container: HTMLElement, key: string) =>
  listRow(container, key).querySelector<HTMLElement>(":scope > span:last-child")
    ?.textContent;

describe("BoardTimelineView on the ERP board", () => {
  it("draws the board's dated items on one group row and counts the undated one", async () => {
    const { container } = renderTimeline();
    await ready();
    // Only items with a start day are listed (no plan here); ERP-153 has no date.
    expect(container.querySelectorAll('[data-tl="lrow"]')).toHaveLength(2);
    expect(container.querySelectorAll('[data-tl="lgrp"]')).toHaveLength(1);
    expect(
      screen.getByRole("link", { name: "1 việc chưa có ngày" }),
    ).toBeTruthy();
    expect(screen.getByRole("grid").getAttribute("aria-label")).toContain(
      "Dòng thời gian",
    );
    expect(dueOf(container, "ERP-142")).toBe("10/10");
    // ERP-142: 3 of 5 child items done is 60%, doing (blue).
    const bar = container.querySelector<HTMLElement>(
      `[data-tl="trow"][data-row-id="${erpItemId("ERP-142")}"] [data-tl="gb"]`,
    );
    expect(bar?.querySelector<HTMLElement>('[data-tl="pg"]')?.style.width).toBe(
      "60%",
    );
    expect(bar?.className).toContain("bg-timeline-bar-doing");
    // No dependency data: no arrows, and the toggles are inert.
    expect(container.querySelectorAll('[data-tl="dl"]')).toHaveLength(0);
    expect(
      screen
        .getByRole("button", { name: /^Đường găng/ })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });

  it("opens the full form with the shown day as start and due when Add item is pressed", async () => {
    const { cards } = renderTimeline();
    await ready();
    fireEvent.click(screen.getByRole("button", { name: "Thêm công việc" }));
    const start = (await screen.findByLabelText(
      "Ngày bắt đầu",
    )) as HTMLInputElement;
    expect(start.value).toBe("12/10/2026");
    expect(
      (screen.getByLabelText("Hạn hoàn thành") as HTMLInputElement).value,
    ).toBe("12/10/2026");
    fireEvent.change(screen.getByRole("textbox", { name: /^Tên công việc/ }), {
      target: { value: "Dựng khung kế hoạch" },
    });
    fireEvent.click(screen.getByRole("button", { name: /^Tạo công việc/ }));
    await waitFor(() => expect(cards.createCard).toHaveBeenCalledTimes(1));
  });

  it("opens the item sheet from the key and clears it on close", async () => {
    const onItem = vi.fn();
    const { container } = renderTimeline(erpOwnerAccess(), {}, { onItem });
    await ready();
    fireEvent.click(
      within(listRow(container, "ERP-142")).getByRole("link", {
        name: /ERP-142/,
      }),
    );
    expect(onItem).toHaveBeenCalledWith(erpItemId("ERP-142"));
    expect(
      await screen.findByRole("heading", {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      }),
    ).toBeTruthy();
  });

  it("opens the Chưa lên lịch tray from the undated count", async () => {
    renderTimeline(erpOwnerAccess());
    await ready();
    fireEvent.click(screen.getByRole("link", { name: "1 việc chưa có ngày" }));
    const tray = screen.getByRole("region", { name: "Chưa lên lịch" });
    expect(within(tray).getByText("ERP-153")).toBeTruthy();
  });

  it("shifts the due day, and the start with it when moving, through the item update", async () => {
    const { container, cards } = renderTimeline();
    await ready();
    // ERP-146: 13/10 to 16/10 (17:00 Hanoi).
    const title = within(listRow(container, "ERP-146")).getAllByRole(
      "link",
    )[1]!;
    fireEvent.keyDown(title, { key: "ArrowRight", altKey: true });
    await waitFor(() => expect(cards.updateCard).toHaveBeenCalledTimes(1));
    expect(cards.updateCard).toHaveBeenLastCalledWith(
      erpItemId("ERP-146"),
      {
        dueDate: "2026-10-17T10:00:00.000Z",
        startDate: "2026-10-14T02:00:00.000Z",
      },
      expect.objectContaining({ expectedVersion: expect.any(Number) }),
    );
    // Alt and Shift change only the due day (the fake board does not keep the
    // first write, so it shifts from 16/10 again).
    fireEvent.keyDown(title, {
      key: "ArrowRight",
      altKey: true,
      shiftKey: true,
    });
    await waitFor(() => expect(cards.updateCard).toHaveBeenCalledTimes(2));
    expect(
      (cards.updateCard.mock.calls[1] as unknown[] | undefined)?.[1],
    ).toEqual({
      dueDate: "2026-10-17T10:00:00.000Z",
    });
  });

  it("puts the item back and says so when the write fails", async () => {
    const { container } = renderTimeline(erpOwnerAccess(), {
      updateCard: async () => {
        throw new Error("rejected");
      },
    });
    await ready();
    const title = within(listRow(container, "ERP-146")).getAllByRole(
      "link",
    )[1]!;
    fireEvent.keyDown(title, { key: "ArrowRight", altKey: true });
    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(toast.error.mock.calls[0]?.[0]).toContain("Không dời được");
    await waitFor(() => expect(dueOf(container, "ERP-146")).toBe("16/10"));
  });

  it("gives a commenter no add and no reschedule, but still opens items", async () => {
    const onItem = vi.fn();
    const { container, cards } = renderTimeline(
      erpCommenterAccess(),
      {},
      { onItem },
    );
    await ready();
    await waitFor(() =>
      expect(
        screen.queryByRole("button", { name: /^Thêm công việc/ }),
      ).toBeNull(),
    );
    const title = within(listRow(container, "ERP-146")).getAllByRole(
      "link",
    )[1]!;
    fireEvent.keyDown(title, { key: "ArrowRight", altKey: true });
    expect(cards.updateCard).not.toHaveBeenCalled();
    fireEvent.click(title);
    expect(onItem).toHaveBeenCalledWith(erpItemId("ERP-146"));
  });
});

describe("BoardTimelineView dependencies and the tray", () => {
  const connectorOf = (container: HTMLElement, key: string) =>
    container.querySelector<HTMLElement>(
      `[data-tl="trow"][data-row-id="${erpItemId(key)}"] [data-tl="connector"]`,
    )!;

  /** Links ERP-142 to the other listed item through the dialog. */
  async function linkFromDialog(container: HTMLElement) {
    fireEvent.click(connectorOf(container, "ERP-142"));
    const dialog = await screen.findByRole("dialog");
    fireEvent.click(within(dialog).getAllByRole("option")[0]!);
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Tạo liên kết" }),
    );
  }

  it("links through the dialog, says so with Hoàn tác, and undoes it with the inverse request", async () => {
    const { container, extras } = renderTimeline();
    await ready();
    await linkFromDialog(container);
    await waitFor(() => expect(extras.addDependency).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    const [message, options] = toast.success.mock.calls[0] as [
      string,
      { action: { label: string; onClick: () => void } },
    ];
    expect(message).toMatch(/^Đã nối: ERP-\d+ chờ ERP-\d+$/);
    expect(options.action.label).toBe("Hoàn tác");
    options.action.onClick();
    await waitFor(() =>
      expect(extras.removeDependency).toHaveBeenCalledTimes(1),
    );
    expect(extras.removeDependency).toHaveBeenCalledWith(
      ...(extras.addDependency.mock.calls[0] as unknown as [string, string]),
    );
  });

  it("keeps a failed link red with Thử lại that sends it again", async () => {
    const { container, extras } = renderTimeline(erpOwnerAccess(), {
      addDependency: async () => {
        throw new Error("offline");
      },
    });
    await ready();
    await linkFromDialog(container);
    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    const [message, options] = toast.error.mock.calls[0] as [
      string,
      { id: string; action: { label: string; onClick: () => void } },
    ];
    expect(message).toMatch(/^Không nối được/);
    expect(options.action.label).toBe("Thử lại");
    options.action.onClick();
    await waitFor(() => expect(extras.addDependency).toHaveBeenCalledTimes(2));
    // Sonner removes the toast Thử lại sits on; the next result needs its own id.
    await waitFor(() => expect(toast.error).toHaveBeenCalledTimes(2));
    expect(toast.error.mock.calls[1]?.[1]?.id).not.toBe(options.id);
  });

  it("schedules an undated item to one day from the tray, with Hoàn tác", async () => {
    const { cards } = renderTimeline();
    await ready();
    fireEvent.click(screen.getByRole("link", { name: "1 việc chưa có ngày" }));
    const tray = screen.getByRole("region", { name: "Chưa lên lịch" });
    fireEvent.click(
      within(tray).getByRole("button", { name: "Đặt ngày cho ERP-153" }),
    );
    fireEvent.change(within(tray).getByLabelText("Đặt ngày cho ERP-153"), {
      target: { value: "2026-10-15" },
    });
    await waitFor(() => expect(cards.updateCard).toHaveBeenCalledTimes(1));
    expect(cards.updateCard).toHaveBeenLastCalledWith(
      erpItemId("ERP-153"),
      {
        startDate: "2026-10-15T01:00:00.000Z",
        dueDate: "2026-10-15T10:00:00.000Z",
      },
      expect.objectContaining({ expectedVersion: expect.any(Number) }),
    );
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    expect(
      (toast.success.mock.calls[0]?.[1] as { action: { label: string } }).action
        .label,
    ).toBe("Hoàn tác");
  });

  it("gives a viewer no connector action and no tray scheduling", async () => {
    const { container } = renderTimeline(erpCommenterAccess());
    await ready();
    expect(
      connectorOf(container, "ERP-142").getAttribute("aria-disabled"),
    ).toBe("true");
    fireEvent.click(screen.getByRole("link", { name: "1 việc chưa có ngày" }));
    expect(
      screen.queryByRole("button", { name: "Đặt ngày cho ERP-153" }),
    ).toBeNull();
  });
});
