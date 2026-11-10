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
} from "@notrelix/work-management-core";
import {
  WorkManagementTestScope,
  applyItemStatus,
  defaultTableViewConfig,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import {
  ERP_PRIORITY_FIELD_ID,
  ERP_STATUS_FIELD_ID,
  ERP_STATUS_OPTION_IDS,
  erpBoardScenario,
  erpCommenterAccess,
  erpItemId,
  erpOwnerAccess,
  itemCapabilitiesStub,
} from "@notrelix/work-management-testing";
import { kanbanCardCatalog } from "../kanban-card.catalog";
import { kanbanLaneCatalog } from "../kanban-lane.catalog";
import { kanbanToolbarCatalog } from "../kanban-toolbar.catalog";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import { KanbanView, type KanbanViewProps } from "../kanban-view";
import { ARCHIVE_UNDO_MS } from "../../table/use-archive-undo-window";

const toast = vi.hoisted(() => ({
  success: vi.fn(),
  error: vi.fn(),
  warning: vi.fn(),
}));
vi.mock("sonner", () => ({ toast }));

beforeAll(() => {
  // Radix menus measure and scroll items; JSDOM has no layout.
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

function fakeServices(
  access: BoardAccess,
  overrides: {
    updateStatus?: (
      cardId: string,
      optionId: string,
    ) => Promise<void> | undefined;
    /** The view's saved config (absent: the view has never been saved). */
    viewConfig?: Record<string, unknown>;
    /** A board just created: it has its statuses but no group yet. */
    noGroups?: boolean;
    /** The archive write; absent: the fake server archives the item. */
    archiveCard?: () => Promise<void>;
  } = {},
) {
  // The fake server keeps written statuses, so refetches see them.
  let board = erpBoardScenario();
  if (overrides.noGroups) board = { ...board, groups: [] };
  const createGroup = vi.fn(async () => undefined);
  const itemExtras = {
    getLinks: vi.fn(async () => [
      {
        id: "link-1",
        relation: "blocked-by" as const,
        item: {
          id: erpItemId("ERP-143"),
          key: "ERP-143",
          title: "Phê duyệt phân quyền",
          statusName: "Đang làm",
          statusCategory: "in_progress" as const,
          isArchived: true,
        },
      },
    ]),
    addLink: vi.fn(async () => undefined),
    removeLink: vi.fn(async () => undefined),
  };
  const cards = {
    getCard: vi.fn(async (id: string) => detailOf(board, id)),
    getCardFiles: vi.fn(async () => []),
    getCardActivity: vi.fn(async () => []),
    updateStatus: vi.fn(async (id: string, optionId: string) => {
      // An override answers for the cards it names; the others persist.
      const answer = overrides.updateStatus?.(id, optionId);
      if (answer) return answer;
      board = applyItemStatus(board, id, optionId) ?? board;
      // The server answers the version the next write names.
      return { version: 2 };
    }),
    createCard: vi.fn(async () => ({ id: "item-new", version: 1 })),
    // A card dropped in a lane is one command: the server keeps the lane's option with the place, or refuses both.
    moveCard: vi.fn(async (move: { cardId: string; laneOptionId?: string }) => {
      if (move.laneOptionId) {
        const answer = overrides.updateStatus?.(move.cardId, move.laneOptionId);
        if (answer) await answer;
        else
          board =
            applyItemStatus(board, move.cardId, move.laneOptionId) ?? board;
      }
      return { version: 2 };
    }),
    updateFieldValue: vi.fn(async () => undefined),
    updateCard: vi.fn(async () => undefined),
    deleteCard: vi.fn(async () => undefined),
    archiveCard: vi.fn(async (id: string) => {
      if (overrides.archiveCard) return overrides.archiveCard();
      board = {
        ...board,
        groups: board.groups.map((group) => ({
          ...group,
          cards: group.cards.filter((item) => item.id !== id),
        })),
      };
    }),
    duplicateCard: vi.fn(async () => undefined),
    // The bulk bar's one request; an archive takes the items off the board.
    bulkUpdate: vi.fn(
      async (
        _boardId: string,
        itemIds: readonly string[],
        change: { type: string },
      ) => {
        if (change.type === "archive")
          board = {
            ...board,
            groups: board.groups.map((group) => ({
              ...group,
              cards: group.cards.filter((item) => !itemIds.includes(item.id)),
            })),
          };
        return itemIds.length;
      },
    ),
  };
  const services = {
    boards: {
      getFullBoard: vi.fn(async () => board),
      listSavedViews: vi.fn(async () => [
        {
          id: "view-1",
          name: "Kanban",
          viewMode: "kanban",
          viewConfig: { ...defaultTableViewConfig, ...overrides.viewConfig },
          isDefault: true,
          visibility: "board",
          createdBy: null,
          version: 1,
        },
      ]),
    },
    access: { getBoardAccess: vi.fn(async () => access) },
    cards,
    groups: { createGroup },
    itemExtras,
    comments: { getCardUpdates: vi.fn(async () => []) },
    itemCapabilities: itemCapabilitiesStub(),
  } as unknown as WorkManagementServices;
  return { services, cards, createGroup, itemExtras };
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
          kanbanCardCatalog,
          kanbanLaneCatalog,
          kanbanToolbarCatalog,
          itemSheetCatalog,
        ]}
        initialLocale="vi"
      >
        {children}
      </I18nProvider>
    </WorkManagementTestScope>
  );
}

/** Holds `item`, `filter` and `groupBy` like the route does. */
function RoutedView(
  props: Partial<KanbanViewProps> & {
    initialItem?: string;
    initialFilter?: string;
    initialGroupBy?: string;
    onItem?: (id: string | undefined, options?: { replace?: boolean }) => void;
    onFilter?: (
      value: string | undefined,
      options?: { replace?: boolean },
    ) => void;
  },
) {
  const [item, setItem] = useState(props.initialItem);
  const [filter, setFilter] = useState(props.initialFilter);
  const [groupBy, setGroupBy] = useState(props.initialGroupBy);
  const board = erpBoardScenario().board;
  return (
    <KanbanView
      boardId={board.id}
      workspaceId={board.workspaceId}
      selectedItemId={item}
      onSelectedItemChange={(id, options) => {
        props.onItem?.(id, options);
        setItem(id);
      }}
      filter={filter}
      onFilterChange={(value, options) => {
        props.onFilter?.(value, options);
        setFilter(value);
      }}
      groupBy={groupBy}
      onGroupByChange={setGroupBy}
    />
  );
}

function renderKanban(
  access: BoardAccess,
  props: Parameters<typeof RoutedView>[0] = {},
  overrides: Parameters<typeof fakeServices>[1] = {},
) {
  const fake = fakeServices(access, overrides);
  const view = renderPureUi(
    <Providers services={fake.services}>
      <RoutedView {...props} />
    </Providers>,
  );
  return { ...view, ...fake };
}

// The card popup is modal and hides the lanes from the accessibility tree while it is open.
const lane = (name: string) =>
  screen.getByRole("region", { name, hidden: true });
const laneCount = (name: string) => lane(name).dataset.laneCount;

async function ready() {
  await screen.findByRole("region", { name: "Cần làm" });
}

describe("KanbanView on the ERP board", () => {
  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("groups the items into the status lanes 6/3/2/5", async () => {
    renderKanban(erpOwnerAccess());
    await ready();
    expect(
      ["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"].map(laneCount),
    ).toEqual(["6", "3", "2", "5"]);
  }, 20_000);

  it("lets an owner add items, from a lane and from the toolbar", async () => {
    const { cards } = renderKanban(erpOwnerAccess());
    await ready();
    await waitFor(() =>
      expect(
        within(lane("Đang làm")).getByRole("button", {
          name: "Thêm vào Đang làm",
        }),
      ).toBeTruthy(),
    );
    fireEvent.click(
      within(lane("Đang làm")).getByRole("button", {
        name: "Thêm vào Đang làm",
      }),
    );
    fireEvent.change(
      screen.getByRole("textbox", { name: "Tên việc mới trong Đang làm" }),
      { target: { value: "Rà soát quyền kế toán" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Thêm" }));
    await waitFor(() =>
      expect(cards.updateStatus).toHaveBeenCalledWith(
        "item-new",
        ERP_STATUS_OPTION_IDS.doing,
        1,
        expect.any(Object),
      ),
    );
    expect(cards.createCard).toHaveBeenCalledWith(
      erpBoardScenario().board.id,
      expect.objectContaining({ title: "Rà soát quyền kế toán" }),
      expect.any(Object),
    );
    expect(screen.getByRole("button", { name: "Thêm công việc" })).toBeTruthy();
  });

  it("keeps the created card when a detail did not save, and retries only that detail", async () => {
    let refuse = true;
    const { cards } = renderKanban(
      erpOwnerAccess(),
      {},
      {
        updateStatus: (id) => {
          if (id !== "item-new") return undefined;
          if (!refuse) return undefined;
          refuse = false;
          return Promise.reject(new Error("network"));
        },
      },
    );
    await ready();
    fireEvent.click(screen.getByRole("button", { name: "Thêm công việc" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Tên thẻ" }), {
      target: { value: "Đối chiếu công nợ" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Tạo thẻ" }));
    expect(
      await screen.findByText("Đã tạo, nhưng còn phần chưa lưu được"),
    ).toBeTruthy();
    expect(cards.createCard).toHaveBeenCalledTimes(1);

    fireEvent.click(
      screen.getAllByRole("button", {
        name: "Thử lại phần còn thiếu",
      })[0] as HTMLElement,
    );
    await waitFor(() => expect(cards.updateStatus).toHaveBeenCalledTimes(2));
    // The retry writes the status on the card that exists: no second card is created.
    expect(cards.updateStatus).toHaveBeenLastCalledWith(
      "item-new",
      ERP_STATUS_OPTION_IDS.todo,
      1,
    );
    expect(cards.createCard).toHaveBeenCalledTimes(1);
    await waitFor(() =>
      expect(
        screen.queryByText("Đã tạo, nhưng còn phần chưa lưu được"),
      ).toBeNull(),
    );
  }, 20_000);

  it("says why nothing was added on a board without a group and offers to add one", async () => {
    const { cards, createGroup } = renderKanban(
      erpOwnerAccess(),
      {},
      { noGroups: true },
    );
    await ready();
    fireEvent.click(
      await within(lane("Đang làm")).findByRole("button", {
        name: "Thêm vào Đang làm",
      }),
    );
    fireEvent.change(
      screen.getByRole("textbox", { name: "Tên việc mới trong Đang làm" }),
      { target: { value: "Việc đầu tiên" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Thêm" }));
    await waitFor(() => expect(toast.error).toHaveBeenCalledTimes(1));
    expect(cards.createCard).not.toHaveBeenCalled();
    const [message, options] = toast.error.mock.calls[0] as [
      string,
      { action: { label: string; onClick: () => void } },
    ];
    expect(message).toContain("chưa có nhóm");
    options.action.onClick();
    await waitFor(() => expect(createGroup).toHaveBeenCalledTimes(1));
    expect(createGroup).toHaveBeenCalledWith(
      expect.objectContaining({ title: "Công việc" }),
    );
  });

  it("gives a commenter no add and a read-only sheet that keeps the composer", async () => {
    renderKanban(erpCommenterAccess(), { initialItem: erpItemId("ERP-142") });
    const dialog = await screen.findByRole(
      "dialog",
      {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      },
      { timeout: 5000 },
    );
    // A commenter's card is read-only: no lane switch, no add controls, but the composer stays.
    await within(dialog).findByRole(
      "button",
      { name: "Thao tác trên thẻ" },
      { timeout: 8000 },
    );
    expect(within(dialog).queryByTitle("Chuyển thẻ sang cột khác")).toBeNull();
    expect(screen.queryByRole("button", { name: /^Thêm vào / })).toBeNull();
    expect(screen.queryByRole("button", { name: "Thêm công việc" })).toBeNull();
    for (const composer of within(dialog).getAllByRole("button", {
      name: "Viết bình luận…",
    }))
      expect(composer.getAttribute("aria-disabled")).not.toBe("true");
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("moves the card to its lane when the status changes in the sheet (keyboard alternative to dragging)", async () => {
    const { cards } = renderKanban(erpOwnerAccess(), {
      initialItem: erpItemId("ERP-149"),
    });
    const dialog = await screen.findByRole("dialog", {
      name: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    });
    const pill = await waitFor(
      () => {
        const button = within(dialog).getByTitle("Chuyển thẻ sang cột khác");
        expect(button.getAttribute("aria-disabled")).not.toBe("true");
        return button;
      },
      { timeout: 8000 },
    );
    fireEvent.click(pill);
    fireEvent.click(await screen.findByRole("option", { name: /Đang làm/ }));
    await waitFor(() => expect(laneCount("Đang làm")).toBe("4"));
    expect(laneCount("Cần làm")).toBe("5");
    expect(
      within(lane("Đang làm")).getByRole("button", {
        name: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
        hidden: true,
      }),
    ).toBeTruthy();
    expect(cards.updateStatus).not.toHaveBeenCalled();
    expect(cards.moveCard).toHaveBeenCalledTimes(1);
    expect(cards.moveCard).toHaveBeenCalledWith(
      expect.objectContaining({
        cardId: erpItemId("ERP-149"),
        expectedVersion: expect.any(Number),
        laneFieldId: ERP_STATUS_FIELD_ID,
        laneOptionId: ERP_STATUS_OPTION_IDS.doing,
      }),
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
  }, 20_000);

  // The server orders a group, not a column: the select names a place where another column holds a card of the group
  // (this fixture has one group; the mock e2e covers a board where no column does).
  it("draws the position select of the column picker when another column holds a card of the group", async () => {
    renderKanban(erpOwnerAccess(), { initialItem: erpItemId("ERP-151") });
    const dialog = await screen.findByRole("dialog", {
      name: "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
    });
    const pill = await waitFor(
      () => {
        const button = within(dialog).getByTitle("Chuyển thẻ sang cột khác");
        expect(button.getAttribute("aria-disabled")).not.toBe("true");
        return button;
      },
      { timeout: 8000 },
    );
    fireEvent.click(pill);
    await screen.findByRole("listbox", { name: "Chuyển sang cột" });
    expect(
      screen.getByRole("combobox", { name: "Vị trí trong cột" }),
    ).toBeTruthy();
    // The current column draws a check and no count; the others keep their count.
    const rows = within(
      screen.getByRole("listbox", { name: "Chuyển sang cột" }),
    ).getAllByRole("option");
    const current = rows.find(
      (row) => row.getAttribute("aria-selected") === "true",
    )!;
    expect(current.querySelector("small")).toBeNull();
    expect(current.querySelector("svg")).not.toBeNull();
    for (const row of rows.filter((entry) => entry !== current))
      expect(row.querySelector("small")).not.toBeNull();
  }, 20_000);

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("puts the card back and says so when the status write fails", async () => {
    renderKanban(
      erpOwnerAccess(),
      { initialItem: erpItemId("ERP-149") },
      {
        updateStatus: async () => {
          throw new Error("forbidden");
        },
      },
    );
    const dialog = await screen.findByRole("dialog", {
      name: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    });
    const pill = await waitFor(
      () => {
        const button = within(dialog).getByTitle("Chuyển thẻ sang cột khác");
        expect(button.getAttribute("aria-disabled")).not.toBe("true");
        return button;
      },
      { timeout: 8000 },
    );
    fireEvent.click(pill);
    fireEvent.click(await screen.findByRole("option", { name: /Đang làm/ }));
    await waitFor(() =>
      expect(toast.error).toHaveBeenCalledWith(
        "Không chuyển được “Viết tài liệu quy trình nhập hoá đơn đầu vào”. Việc đã về lại Cần làm.",
        { id: `kanban-write-${erpItemId("ERP-149")}` },
      ),
    );
    expect(laneCount("Cần làm")).toBe("6");
    expect(laneCount("Đang làm")).toBe("3");
  }, 20_000);

  it("applies the route filter, shows the no-results card and clears the filter with Xoá bộ lọc", async () => {
    // The ERP seed dates its low-priority items up to 30/10: judge "overdue" on that day, not on the
    // day the suite runs, or the filter that is meant to match nothing starts matching on 31/10.
    vi.useFakeTimers({
      toFake: ["Date"],
      now: new Date("2026-10-30T05:00:00+07:00"),
    });
    const onFilter = vi.fn();
    renderKanban(erpOwnerAccess(), { initialFilter: "p.low~o", onFilter });
    const card = await waitFor(
      () => {
        const found = screen
          .getAllByRole("status")
          .find((element) =>
            element.textContent?.includes("Không có việc nào khớp bộ lọc"),
          );
        expect(found).toBeTruthy();
        return found!;
      },
      { timeout: 8000 },
    );
    expect(card.textContent).toContain(
      "2 điều kiện đang áp dụng trên 16 việc.",
    );
    expect(
      within(card).getByRole("button", { name: "Sửa điều kiện" }),
    ).toBeTruthy();
    fireEvent.click(within(card).getByRole("button", { name: "Xoá bộ lọc" }));
    expect(onFilter).toHaveBeenLastCalledWith(undefined, undefined);
    await waitFor(() => expect(laneCount("Cần làm")).toBe("6"), {
      timeout: 8000,
    });
  }, 20_000);

  it("drops ids the board does not have from the URL filter with history replace", async () => {
    const onFilter = vi.fn();
    renderKanban(erpOwnerAccess(), {
      initialFilter: "s.gone-status~p.high~l.gone-label",
      onFilter,
    });
    await ready();
    // The older facet string is rewritten once, as the compound filter, without the ids the board lacks.
    await waitFor(() =>
      expect(onFilter).toHaveBeenCalledWith(expect.stringMatching(/^c2:/), {
        replace: true,
      }),
    );
    // Only the high-priority items remain (5 on the board).
    expect(
      ["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"]
        .map((name) => Number(laneCount(name)))
        .reduce((sum, count) => sum + count, 0),
    ).toBe(5);
  });

  it("removes a filter parameter it cannot read", async () => {
    const onFilter = vi.fn();
    renderKanban(erpOwnerAccess(), { initialFilter: "mine", onFilter });
    await ready();
    await waitFor(() =>
      expect(onFilter).toHaveBeenCalledWith(undefined, { replace: true }),
    );
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("copies the item link from the sheet and confirms it", async () => {
    const writeText = vi.fn(async (_text: string) => undefined);
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    renderKanban(erpOwnerAccess(), { initialItem: erpItemId("ERP-142") });
    const dialog = await screen.findByRole(
      "dialog",
      {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      },
      { timeout: 5000 },
    );
    fireEvent.keyDown(
      await within(dialog).findByRole(
        "button",
        { name: "Thao tác trên thẻ" },
        { timeout: 8000 },
      ),
      { key: "Enter" },
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: /^Sao chép liên kết/ }),
    );
    await waitFor(() =>
      expect(toast.success).toHaveBeenCalledWith("Đã sao chép liên kết"),
    );
    const link = new URL(writeText.mock.calls[0]![0] as string);
    expect(link.searchParams.get("item")).toBe(erpItemId("ERP-142"));
    expect(link.searchParams.get("view")).toBe("kanban");
  });

  it("groups by another Select field named by the groupBy parameter", async () => {
    renderKanban(erpOwnerAccess(), { initialGroupBy: ERP_PRIORITY_FIELD_ID });
    await screen.findByRole("region", { name: "Khẩn" });
    expect(["Khẩn", "Cao", "Trung bình", "Thấp"].map(laneCount)).toEqual([
      "1",
      "5",
      "7",
      "3",
    ]);
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("reports a failed status write even after another write has started, and keeps the other card's change", async () => {
    let failFirst!: (error: Error) => void;
    const first = new Promise<void>((_resolve, reject) => (failFirst = reject));
    renderKanban(
      erpOwnerAccess(),
      { initialItem: erpItemId("ERP-149") },
      {
        updateStatus: (cardId) =>
          cardId === erpItemId("ERP-149") ? first : undefined,
      },
    );
    const pickDoing = async (title: string) => {
      const dialog = await screen.findByRole(
        "dialog",
        { name: title },
        { timeout: 8000 },
      );
      const pill = await waitFor(
        () => {
          const button = within(dialog).getByTitle("Chuyển thẻ sang cột khác");
          expect(button.getAttribute("aria-disabled")).not.toBe("true");
          return button;
        },
        { timeout: 8000 },
      );
      fireEvent.click(pill);
      fireEvent.click(await screen.findByRole("option", { name: /Đang làm/ }));
      return dialog;
    };
    const dialog = await pickDoing(
      "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    );
    // Another item, then a second status write.
    fireEvent.click(within(dialog).getByRole("button", { name: "Đóng" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    fireEvent.click(
      document.querySelector(`[data-card-id="${erpItemId("ERP-147")}"]`)!,
    );
    await pickDoing("Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng");
    await waitFor(() => expect(laneCount("Đang làm")).toBe("5"));

    failFirst(new Error("forbidden"));
    await waitFor(() =>
      expect(toast.error).toHaveBeenCalledWith(
        "Không chuyển được “Viết tài liệu quy trình nhập hoá đơn đầu vào”. Việc đã về lại Cần làm.",
        { id: `kanban-write-${erpItemId("ERP-149")}` },
      ),
    );
    await waitFor(() => expect(laneCount("Đang làm")).toBe("4"));
    expect(
      within(lane("Đang làm")).getByRole("button", {
        name: "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
        hidden: true,
      }),
    ).toBeTruthy();
  }, 20_000);

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("archives the item from the sheet (it is not deleted)", async () => {
    const { cards } = renderKanban(erpOwnerAccess(), {
      initialItem: erpItemId("ERP-142"),
    });
    const dialog = await screen.findByRole(
      "dialog",
      {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      },
      { timeout: 5000 },
    );
    const more = await waitFor(
      () => within(dialog).getByRole("button", { name: "Thao tác trên thẻ" }),
      { timeout: 8000 },
    );
    fireEvent.keyDown(more, { key: "Enter" });
    fireEvent.click(screen.getByRole("menuitem", { name: "Lưu trữ…" }));
    // Archiving asks first, and names what it takes along.
    const confirm = await screen.findByRole("dialog", {
      name: "Lưu trữ việc này?",
    });
    expect(cards.archiveCard).not.toHaveBeenCalled();
    fireEvent.click(
      within(confirm).getByRole("button", {
        name: /^Lưu trữ (việc|\d+ việc)$/,
      }),
    );
    await waitFor(() =>
      expect(cards.archiveCard).toHaveBeenCalledWith(
        erpItemId("ERP-142"),
        expect.any(Number),
        expect.objectContaining({ idempotencyKey: expect.any(String) }),
      ),
    );
    expect(cards.deleteCard).not.toHaveBeenCalled();
    await waitFor(() => expect(laneCount("Đang làm")).toBe("2"));
  }, 20_000);

  it("keeps listing the sub-items in the dialog while the archive is out", async () => {
    let release = () => {};
    const { cards } = renderKanban(
      erpOwnerAccess(),
      { initialItem: erpItemId("ERP-142") },
      {
        archiveCard: () =>
          new Promise<void>((resolve) => {
            release = resolve;
          }),
      },
    );
    const dialog = await screen.findByRole(
      "dialog",
      { name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP" },
      { timeout: 5000 },
    );
    const more = await waitFor(
      () => within(dialog).getByRole("button", { name: "Thao tác trên thẻ" }),
      { timeout: 8000 },
    );
    fireEvent.keyDown(more, { key: "Enter" });
    fireEvent.click(screen.getByRole("menuitem", { name: "Lưu trữ…" }));
    const confirm = await screen.findByRole("dialog", {
      name: "Lưu trữ việc này?",
    });
    const listed = within(confirm).getByText(/việc con/).textContent;
    fireEvent.click(
      within(confirm).getByRole("button", {
        name: /^Lưu trữ (việc|\d+ việc)$/,
      }),
    );
    await waitFor(() => expect(cards.archiveCard).toHaveBeenCalledTimes(1));
    // The card has left the board read by now; the dialog still lists what it asked about.
    expect(within(confirm).getByText(/việc con/).textContent).toBe(listed);
    expect(
      within(confirm).getByRole("button", { name: "Đang lưu trữ…" }),
    ).toBeTruthy();
    release();
  }, 20_000);

  it("says in the dialog that someone changed the item when the archive is refused as stale, and keeps the item", async () => {
    const { cards } = renderKanban(
      erpOwnerAccess(),
      { initialItem: erpItemId("ERP-142") },
      {
        archiveCard: async () => {
          throw Object.assign(new Error("stale"), { status: 412 });
        },
      },
    );
    const dialog = await screen.findByRole(
      "dialog",
      { name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP" },
      { timeout: 5000 },
    );
    const more = await waitFor(
      () => within(dialog).getByRole("button", { name: "Thao tác trên thẻ" }),
      { timeout: 8000 },
    );
    fireEvent.keyDown(more, { key: "Enter" });
    fireEvent.click(screen.getByRole("menuitem", { name: "Lưu trữ…" }));
    const confirm = await screen.findByRole("dialog", {
      name: "Lưu trữ việc này?",
    });
    fireEvent.click(
      within(confirm).getByRole("button", {
        name: /^Lưu trữ (việc|\d+ việc)$/,
      }),
    );
    const alert = await within(confirm).findByRole("alert");
    expect(alert.textContent).toContain("Có người vừa sửa việc này");
    expect(cards.archiveCard).toHaveBeenCalledTimes(1);
    expect(
      screen.getByRole("region", { name: "Đang làm", hidden: true }).dataset
        .laneCount,
    ).toBe("3");
    expect(
      within(confirm).getByRole("button", { name: "Thử lại" }),
    ).toBeTruthy();
  }, 20_000);

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("lists the links of the item in the sheet, adds one through the picker and removes one", async () => {
    const { itemExtras } = renderKanban(erpOwnerAccess(), {
      initialItem: erpItemId("ERP-142"),
    });
    const dialog = await screen.findByRole(
      "dialog",
      { name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP" },
      { timeout: 5000 },
    );
    const unlink = await within(dialog).findByRole(
      "button",
      { name: "Gỡ liên kết ERP-143" },
      { timeout: 8000 },
    );
    // An archived item stays linked and its row says so.
    expect(within(dialog).getByText("Đã lưu trữ")).toBeTruthy();
    fireEvent.click(unlink);
    await waitFor(() =>
      expect(itemExtras.removeLink).toHaveBeenCalledWith(
        erpItemId("ERP-142"),
        "link-1",
      ),
    );

    fireEvent.click(
      within(dialog).getByRole("button", { name: "Thêm liên kết" }),
    );
    // The link form: the relation, then an item of the board, then the submit button.
    fireEvent.change(await screen.findByLabelText("Loại liên kết"), {
      target: { value: "blocked-by" },
    });
    const [first] = within(
      await screen.findByRole("listbox", { name: "Kết quả" }),
    ).getAllByRole("option");
    fireEvent.click(first!);
    const form = screen
      .getByLabelText("Loại liên kết")
      .closest("[role=dialog]")!;
    fireEvent.click(
      within(form as HTMLElement).getByRole("button", {
        name: "Thêm liên kết",
      }),
    );
    await waitFor(() =>
      expect(itemExtras.addLink).toHaveBeenCalledWith(erpItemId("ERP-142"), {
        itemId: expect.any(String),
        relation: "blocked-by",
      }),
    );
  }, 20_000);

  it("takes the covered board out of the tab order on a narrow screen while the sheet is open", async () => {
    const width = window.innerWidth;
    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: 320,
    });
    try {
      renderKanban(erpOwnerAccess(), { initialItem: erpItemId("ERP-142") });
      await screen.findByRole(
        "dialog",
        {
          name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
        },
        { timeout: 5000 },
      );
      const board = document.querySelector("[data-kanban-board]")!;
      const covered = Array.from(board.children).filter((child) =>
        child.hasAttribute("inert"),
      );
      expect(covered).toHaveLength(2);
      for (const element of covered) {
        expect(element.getAttribute("aria-hidden")).toBe("true");
      }
    } finally {
      Object.defineProperty(window, "innerWidth", {
        configurable: true,
        value: width,
      });
    }
  });

  it("leaves the board usable beside the sheet on a wide screen", async () => {
    renderKanban(erpOwnerAccess(), { initialItem: erpItemId("ERP-142") });
    await screen.findByRole(
      "dialog",
      {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      },
      { timeout: 5000 },
    );
    expect(document.querySelector("[data-kanban-board] [inert]")).toBeNull();
  });
});

describe("KanbanView settings of the view", () => {
  const editor = (): BoardAccess => ({
    ...erpOwnerAccess(),
    myLevel: "editor",
  });

  it("shows the add-status slot to an editor while the view allows editors", async () => {
    renderKanban(editor());
    await ready();
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Thêm trạng thái" }),
      ).toBeTruthy(),
    );
  });

  it("hides the add and edit status controls from an editor when only managers may manage statuses", async () => {
    const { cards, services } = renderKanban(
      editor(),
      {},
      {
        viewConfig: { statusManageRoles: ["manager"] },
      },
    );
    await ready();
    // The saved view has been read before judging what is hidden.
    await waitFor(() =>
      expect(services.boards.listSavedViews).toHaveBeenCalled(),
    );
    await new Promise((resolve) => setTimeout(resolve, 20));
    // The editor still adds items: only the status controls are shaped.
    await waitFor(() =>
      expect(
        within(lane("Đang làm")).getByRole("button", {
          name: "Thêm vào Đang làm",
        }),
      ).toBeTruthy(),
    );
    expect(
      screen.queryByRole("button", { name: "Thêm trạng thái" }),
    ).toBeNull();
    fireEvent.click(
      within(lane("Đang làm")).getByRole("button", {
        name: "Tuỳ chọn cột Đang làm",
      }),
    );
    expect(
      screen.queryByRole("menuitem", { name: "Sửa trạng thái" }),
    ).toBeNull();
    expect(cards.createCard).not.toHaveBeenCalled();
  });

  it("never locks a manager out, even when the saved roles are empty", async () => {
    renderKanban(
      erpOwnerAccess(),
      {},
      { viewConfig: { statusManageRoles: [] } },
    );
    await ready();
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Thêm trạng thái" }),
      ).toBeTruthy(),
    );
  });

  it("shows as many cards per lane as the saved lane limit and keeps the rest for the scroll, with no button", async () => {
    renderKanban(erpOwnerAccess(), {}, { viewConfig: { laneCardLimit: 2 } });
    await ready();
    await waitFor(() =>
      expect(lane("Cần làm").querySelectorAll("[data-card-id]")).toHaveLength(
        2,
      ),
    );
    expect(
      within(lane("Cần làm")).queryByRole("button", { name: /Xem thêm/ }),
    ).toBeNull();
  });
});

describe("KanbanView bulk archive (UC-WM-J01)", () => {
  const ERP_151 = "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán";
  const ERP_149 = "Viết tài liệu quy trình nhập hoá đơn đầu vào";
  const pick = (title: string) =>
    fireEvent.click(screen.getByRole("checkbox", { name: `Chọn ${title}` }));
  const cardShown = (title: string) =>
    screen.queryByRole("checkbox", { name: `Chọn ${title}` }) !== null;

  afterEach(() => {
    vi.useRealTimers();
  });

  it("archives the picked cards in one request after the Table's undo window, not before", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const { cards } = renderKanban(erpOwnerAccess());
    await ready();
    const before = Number(laneCount("Cần làm"));
    pick(ERP_151);
    pick(ERP_149);
    fireEvent.click(screen.getByRole("button", { name: "Lưu trữ" }));

    // The cards leave the board at once; the write waits for the window.
    expect(cardShown(ERP_151)).toBe(false);
    expect(cardShown(ERP_149)).toBe(false);
    expect(Number(laneCount("Cần làm"))).toBe(before - 2);
    expect(screen.getByText("Đã lưu trữ 2 việc")).toBeTruthy();
    expect(cards.bulkUpdate).not.toHaveBeenCalled();

    await act(async () => {
      vi.advanceTimersByTime(ARCHIVE_UNDO_MS + 50);
    });
    await waitFor(() => expect(cards.bulkUpdate).toHaveBeenCalledTimes(1));
    expect(cards.bulkUpdate).toHaveBeenCalledWith(
      expect.any(String),
      [erpItemId("ERP-151"), erpItemId("ERP-149")],
      { type: "archive" },
      expect.any(Object),
      expect.anything(),
    );
    expect(cards.archiveCard).not.toHaveBeenCalled();
    await waitFor(() =>
      expect(screen.queryByText("Đã lưu trữ 2 việc")).toBeNull(),
    );
    expect(cardShown(ERP_151)).toBe(false);
  }, 20_000);

  it("brings the cards back and sends nothing when the archive is undone", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const { cards } = renderKanban(erpOwnerAccess());
    await ready();
    pick(ERP_151);
    fireEvent.click(screen.getByRole("button", { name: "Lưu trữ" }));
    expect(cardShown(ERP_151)).toBe(false);
    fireEvent.click(screen.getByRole("button", { name: "Hoàn tác" }));
    expect(cardShown(ERP_151)).toBe(true);
    await act(async () => {
      vi.advanceTimersByTime(ARCHIVE_UNDO_MS + 50);
    });
    expect(cards.bulkUpdate).not.toHaveBeenCalled();
    expect(cards.archiveCard).not.toHaveBeenCalled();
  }, 20_000);

  it("sends the archive at once when the notice is closed", async () => {
    const { cards } = renderKanban(erpOwnerAccess());
    await ready();
    pick(ERP_151);
    fireEvent.click(screen.getByRole("button", { name: "Lưu trữ" }));
    expect(cards.bulkUpdate).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Đóng thông báo" }));
    await waitFor(() => expect(cards.bulkUpdate).toHaveBeenCalledTimes(1));
  }, 20_000);
});
