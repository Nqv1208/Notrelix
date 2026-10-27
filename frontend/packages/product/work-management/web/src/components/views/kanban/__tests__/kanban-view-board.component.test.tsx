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
  applyItemStatus,
  defaultTableViewConfig,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import {
  ERP_PRIORITY_FIELD_ID,
  ERP_STATUS_OPTION_IDS,
  erpBoardScenario,
  erpCommenterAccess,
  erpItemId,
  erpOwnerAccess,
} from "@notrelix/work-management-testing";
import { kanbanCardCatalog } from "../kanban-card.catalog";
import { kanbanLaneCatalog } from "../kanban-lane.catalog";
import { kanbanToolbarCatalog } from "../kanban-toolbar.catalog";
import { itemSheetCatalog } from "../../../card-detail/item-sheet.catalog";
import { KanbanView, type KanbanViewProps } from "../kanban-view";

const toast = vi.hoisted(() => ({ success: vi.fn(), error: vi.fn() }));
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
  } = {},
) {
  // The fake server keeps written statuses, so refetches see them.
  let board = erpBoardScenario();
  if (overrides.noGroups) board = { ...board, groups: [] };
  const createGroup = vi.fn(async () => undefined);
  const cards = {
    getCard: vi.fn(async (id: string) => detailOf(board, id)),
    getCardFiles: vi.fn(async () => []),
    getCardActivity: vi.fn(async () => []),
    updateStatus: vi.fn(async (id: string, optionId: string) => {
      // An override answers for the cards it names; the others persist.
      const answer = overrides.updateStatus?.(id, optionId);
      if (answer) return answer;
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
    comments: { getCardUpdates: vi.fn(async () => []) },
  } as unknown as WorkManagementServices;
  return { services, cards, createGroup };
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

const lane = (name: string) => screen.getByRole("region", { name });
const laneCount = (name: string) => lane(name).dataset.laneCount;

async function ready() {
  await screen.findByRole("region", { name: "Cần làm" });
}

describe("KanbanView on the ERP board", () => {
  it("groups the items into the status lanes 6/3/2/5", async () => {
    renderKanban(erpOwnerAccess());
    await ready();
    expect(
      ["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"].map(laneCount),
    ).toEqual(["6", "3", "2", "5"]);
  });

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
    await waitFor(() =>
      expect(
        within(dialog)
          .getByRole("button", { name: /^Trạng thái: / })
          .getAttribute("aria-disabled"),
      ).toBe("true"),
    );
    expect(screen.queryByRole("button", { name: /^Thêm vào / })).toBeNull();
    expect(screen.queryByRole("button", { name: "Thêm công việc" })).toBeNull();
    expect(
      within(dialog)
        .getByRole("textbox", { name: /bình luận/i })
        .getAttribute("aria-disabled"),
    ).not.toBe("true");
  });

  it("moves the card to its lane when the status changes in the sheet (keyboard alternative to dragging)", async () => {
    const { cards } = renderKanban(erpOwnerAccess(), {
      initialItem: erpItemId("ERP-149"),
    });
    const dialog = await screen.findByRole("dialog", {
      name: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    });
    const pill = await waitFor(() => {
      const button = within(dialog).getByRole("button", {
        name: /^Trạng thái: Cần làm/,
      });
      expect(button.getAttribute("aria-disabled")).not.toBe("true");
      return button;
    });
    fireEvent.keyDown(pill, { key: "Enter" });
    fireEvent.click(screen.getByRole("menuitem", { name: "Đang làm" }));
    await waitFor(() => expect(laneCount("Đang làm")).toBe("4"));
    expect(laneCount("Cần làm")).toBe("5");
    expect(
      within(lane("Đang làm")).getByRole("button", {
        name: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
      }),
    ).toBeTruthy();
    expect(cards.updateStatus).toHaveBeenCalledWith(
      erpItemId("ERP-149"),
      ERP_STATUS_OPTION_IDS.doing,
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
  });

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
    const pill = await waitFor(() => {
      const button = within(dialog).getByRole("button", {
        name: /^Trạng thái: Cần làm/,
      });
      expect(button.getAttribute("aria-disabled")).not.toBe("true");
      return button;
    });
    fireEvent.keyDown(pill, { key: "Enter" });
    fireEvent.click(screen.getByRole("menuitem", { name: "Đang làm" }));
    await waitFor(() =>
      expect(toast.error).toHaveBeenCalledWith(
        "Không chuyển được “Viết tài liệu quy trình nhập hoá đơn đầu vào”. Việc đã về lại Cần làm.",
        { id: `kanban-write-${erpItemId("ERP-149")}` },
      ),
    );
    expect(laneCount("Cần làm")).toBe("6");
    expect(laneCount("Đang làm")).toBe("3");
  });

  it("applies the route filter, shows the no-results bar and clears the filter with Bỏ lọc", async () => {
    const onFilter = vi.fn();
    renderKanban(erpOwnerAccess(), { initialFilter: "p.low~o", onFilter });
    await ready();
    const bar = await waitFor(() => {
      const found = screen
        .getAllByRole("status")
        .find((element) =>
          element.textContent?.includes("Không có việc khớp bộ lọc"),
        );
      expect(found).toBeTruthy();
      return found!;
    });
    expect(bar.textContent).toContain("Không có việc khớp bộ lọc");
    expect(bar.textContent).toContain("16 việc của board đang bị ẩn");
    expect(
      ["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"].map(laneCount),
    ).toEqual(["0", "0", "0", "0"]);
    expect(
      within(lane("Cần làm")).getByText("Không có việc khớp bộ lọc"),
    ).toBeTruthy();
    fireEvent.click(within(bar).getByRole("button", { name: "Bỏ lọc" }));
    expect(onFilter).toHaveBeenLastCalledWith(undefined, undefined);
    await waitFor(() => expect(laneCount("Cần làm")).toBe("6"));
  });

  it("drops ids the board does not have from the URL filter with history replace", async () => {
    const onFilter = vi.fn();
    renderKanban(erpOwnerAccess(), {
      initialFilter: "s.gone-status~p.high~l.gone-label",
      onFilter,
    });
    await ready();
    await waitFor(() =>
      expect(onFilter).toHaveBeenCalledWith("p.high", { replace: true }),
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

  it("steps through the items in lane order with K and J, keeping the sheet open", async () => {
    const onItem = vi.fn();
    renderKanban(erpOwnerAccess(), {
      initialItem: erpItemId("ERP-149"),
      onItem,
    });
    const dialog = await screen.findByRole("dialog", {
      name: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    });
    fireEvent.keyDown(dialog, { key: "j" });
    expect(onItem).toHaveBeenLastCalledWith(erpItemId("ERP-147"), {
      replace: true,
    });
    await screen.findByRole("dialog", {
      name: "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
    });
    // The same sheet element: it was never unmounted while the next item loaded.
    expect(screen.getByRole("dialog")).toBe(dialog);
    expect(dialog.contains(document.activeElement)).toBe(true);
    fireEvent.keyDown(dialog, { key: "k" });
    fireEvent.keyDown(dialog, { key: "k" });
    await screen.findByRole("dialog", {
      name: "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
    });
    expect(onItem).toHaveBeenLastCalledWith(erpItemId("ERP-151"), {
      replace: true,
    });
  });

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
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Sao chép liên kết" }),
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
      const dialog = await screen.findByRole("dialog", { name: title });
      const pill = await waitFor(() => {
        const button = within(dialog).getByRole("button", {
          name: /^Trạng thái: Cần làm/,
        });
        expect(button.getAttribute("aria-disabled")).not.toBe("true");
        return button;
      });
      fireEvent.keyDown(pill, { key: "Enter" });
      fireEvent.click(screen.getByRole("menuitem", { name: "Đang làm" }));
      return dialog;
    };
    const dialog = await pickDoing(
      "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    );
    // The next item in lane order, then a second status write.
    fireEvent.keyDown(dialog, { key: "j" });
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
      }),
    ).toBeTruthy();
  });

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
    const more = await waitFor(() =>
      within(dialog).getByRole("button", { name: "Thêm" }),
    );
    fireEvent.keyDown(more, { key: "Enter" });
    fireEvent.click(screen.getByRole("menuitem", { name: "Lưu trữ" }));
    await waitFor(() =>
      expect(cards.archiveCard).toHaveBeenCalledWith(
        erpItemId("ERP-142"),
        expect.objectContaining({ idempotencyKey: expect.any(String) }),
      ),
    );
    expect(cards.deleteCard).not.toHaveBeenCalled();
    await waitFor(() => expect(laneCount("Đang làm")).toBe("2"));
  });

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

  it("shows as many cards per lane as the saved lane limit", async () => {
    renderKanban(erpOwnerAccess(), {}, { viewConfig: { laneCardLimit: 2 } });
    await ready();
    await waitFor(() =>
      expect(
        within(lane("Cần làm")).getByRole("button", {
          name: "Xem thêm 4 việc",
        }),
      ).toBeTruthy(),
    );
  });
});
