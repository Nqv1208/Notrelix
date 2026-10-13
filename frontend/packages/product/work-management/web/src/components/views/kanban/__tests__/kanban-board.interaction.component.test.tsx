import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  groupCardsByStatus,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import { erpBoardScenario, erpItemId } from "@notrelix/work-management-testing";
import { KanbanBoard } from "../kanban-board";
import { getKanbanLaneDrop } from "../kanban-dnd";
import { kanbanCardCopy } from "../kanban-card.copy";
import { kanbanLaneCopy } from "../kanban-lane.copy";

/** 12/10/2026 10:40 in the runtime's own time zone. */
const NOW = new Date(2026, 9, 12, 10, 40);
const LOCALES = [
  ["vi", kanbanLaneCopy("vi"), kanbanCardCopy("vi")],
  ["en", kanbanLaneCopy("en"), kanbanCardCopy("en")],
] as const;

function erpLanes(): KanbanStatusLane[] {
  const { board, groups } = erpBoardScenario();
  return groupCardsByStatus({
    fieldDefinitions: board.fieldDefinitions,
    groups,
  });
}

function renderBoard(
  props: Partial<Parameters<typeof KanbanBoard>[0]> & {
    locale?: "vi" | "en";
  } = {},
) {
  const locale = props.locale ?? "vi";
  const onOpenDetails = vi.fn();
  const onDropCard = vi.fn();
  const onCreateItem = vi.fn();
  const view = renderPureUi(
    <KanbanBoard
      lanes={erpLanes()}
      copy={kanbanLaneCopy(locale)}
      cardCopy={kanbanCardCopy(locale)}
      now={NOW}
      filterActive={false}
      canAdd
      canDrag
      onOpenDetails={onOpenDetails}
      onDropCard={onDropCard}
      onCreateItem={onCreateItem}
      {...props}
    />,
  );
  return { ...view, onOpenDetails, onDropCard, onCreateItem };
}

function laneRegion(name: string) {
  return screen.getByRole("region", { name });
}

function cardButtons(region: HTMLElement) {
  return within(region)
    .getAllByRole("button")
    .filter((button) => button.hasAttribute("data-card-id"));
}

describe("KanbanBoard status lanes", () => {
  it("FUI[wm.kanban.board:board] shows the lanes in option order with their name, count and tint", () => {
    for (const [locale, copy] of LOCALES) {
      const { unmount } = renderBoard({ locale });
      const regions = screen.getAllByRole("region");
      expect(
        regions.map((region) => region.getAttribute("aria-label")),
      ).toEqual(["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"]);
      expect(regions.map((region) => region.dataset.laneCount)).toEqual([
        "6",
        "3",
        "2",
        "5",
      ]);
      expect(
        within(regions[0]!).getByText("6", { selector: "[aria-hidden]" }),
      ).toBeTruthy();
      expect(
        within(regions[0]!).getByText(
          copy.count_other.replace("{{count}}", "6"),
        ),
      ).toBeTruthy();
      expect(regions[0]!.className).toContain("bg-work-status-todo-lane");
      expect(regions[1]!.className).toContain("bg-work-status-doing-lane");
      expect(regions[2]!.className).toContain("bg-work-status-review-lane");
      expect(regions[3]!.className).toContain("bg-work-status-done-lane");
      expect(locale).toBeTruthy();
      unmount();
    }
  });

  it("FUI[wm.kanban.board:show-more] caps a lane at four cards and reveals the rest, focusing the first revealed card", async () => {
    for (const [locale, copy] of LOCALES) {
      const { unmount } = renderBoard({ locale });
      const todo = laneRegion("Cần làm");
      expect(cardButtons(todo)).toHaveLength(4);
      const more = within(todo).getByRole("button", {
        name: copy.showMore_other.replace("{{count}}", "2"),
      });
      fireEvent.click(more);
      const cards = cardButtons(laneRegion("Cần làm"));
      expect(cards).toHaveLength(6);
      await waitFor(() => expect(document.activeElement).toBe(cards[4]));
      expect(
        within(laneRegion("Cần làm")).queryByRole("button", {
          name: copy.showMore_other.replace("{{count}}", "2"),
        }),
      ).toBeNull();
      unmount();
    }
  });

  it("shows the selected card even when it sits past the cap", () => {
    renderBoard({ selectedItemId: erpItemId("ERP-153") });
    const todo = laneRegion("Cần làm");
    expect(cardButtons(todo)).toHaveLength(6);
    const selected = todo.querySelector("[aria-current='true']");
    expect(selected?.textContent).toContain("ERP-153");
  });

  it("FUI[wm.kanban.board:lane-menu] collapses, expands, hides and shows a lane on this view only", () => {
    for (const [locale, copy] of LOCALES) {
      const { unmount } = renderBoard({ locale });
      const options = within(laneRegion("Cần làm")).getByRole("button", {
        name: copy.laneOptions.replace("{{lane}}", "Cần làm"),
      });
      fireEvent.keyDown(options, { key: "Enter" });
      fireEvent.click(screen.getByRole("menuitem", { name: copy.collapse }));
      expect(cardButtons(laneRegion("Cần làm"))).toHaveLength(0);
      fireEvent.click(
        screen.getByRole("button", {
          name: copy.expand.replace("{{lane}}", "Cần làm"),
        }),
      );
      expect(cardButtons(laneRegion("Cần làm"))).toHaveLength(4);

      fireEvent.keyDown(
        within(laneRegion("Chờ duyệt")).getByRole("button", {
          name: copy.laneOptions.replace("{{lane}}", "Chờ duyệt"),
        }),
        { key: "Enter" },
      );
      fireEvent.click(screen.getByRole("menuitem", { name: copy.hide }));
      expect(screen.queryByRole("region", { name: "Chờ duyệt" })).toBeNull();
      fireEvent.click(
        screen.getByRole("button", {
          name: copy.showHidden_one.replace("{{count}}", "1"),
        }),
      );
      expect(laneRegion("Chờ duyệt")).toBeTruthy();
      unmount();
    }
  });

  it("FUI[wm.kanban.board:add] adds an item to a lane through the injected callback", () => {
    for (const [locale, copy] of LOCALES) {
      const { unmount, onCreateItem } = renderBoard({ locale });
      fireEvent.click(
        within(laneRegion("Đang làm")).getByRole("button", {
          name: copy.addTo.replace("{{lane}}", "Đang làm"),
        }),
      );
      const input = screen.getByRole("textbox", {
        name: copy.newItemLabel.replace("{{lane}}", "Đang làm"),
      });
      fireEvent.change(input, { target: { value: "Rà soát quyền" } });
      fireEvent.click(screen.getByRole("button", { name: copy.addSubmit }));
      expect(onCreateItem).toHaveBeenCalledWith(
        expect.objectContaining({ optionId: "status-doing" }),
        "Rà soát quyền",
      );
      unmount();
    }
  });

  it("FUI[wm.kanban.board:read-only] offers no add for a read-only board", () => {
    renderBoard({ canAdd: false, canDrag: false });
    expect(screen.queryByRole("button", { name: /^Thêm vào/ })).toBeNull();
    // Cards still open the item.
    expect(cardButtons(laneRegion("Đang làm"))).toHaveLength(3);
  });

  it("FUI[wm.kanban.board:empty-lane] states an empty lane, worded for an active filter", () => {
    const lanes = erpLanes().map((lane, index) =>
      index === 2 ? { ...lane, cards: [] } : lane,
    );
    const filtered = renderBoard({ lanes, filterActive: true });
    expect(
      within(laneRegion("Chờ duyệt")).getByText("Không có việc khớp bộ lọc"),
    ).toBeTruthy();
    expect(laneRegion("Chờ duyệt").dataset.laneCount).toBe("0");
    filtered.unmount();
    renderBoard({ lanes, filterActive: false, locale: "en" });
    expect(
      within(laneRegion("Chờ duyệt")).getByText("No items yet"),
    ).toBeTruthy();
  });

  it("names the lane for items without a status", () => {
    const lanes: KanbanStatusLane[] = [
      ...erpLanes(),
      {
        optionId: null,
        name: "",
        category: null,
        color: null,
        cards: [],
        total: 0,
      },
    ];
    renderBoard({ lanes });
    expect(laneRegion("Không có trạng thái")).toBeTruthy();
  });

  it("opens a card through the injected callback", () => {
    const { onOpenDetails } = renderBoard();
    fireEvent.click(
      within(laneRegion("Đang làm")).getByRole("button", {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      }),
    );
    expect(onOpenDetails).toHaveBeenCalledWith(erpItemId("ERP-142"));
  });

  it("FUI[wm.kanban.board:move] reads a drop on another lane as that lane's status and a drop in the lane as a reorder", () => {
    const lanes = erpLanes();
    const active = lanes[0]!.cards[1]!;
    const event = (over: unknown) =>
      ({
        active: {
          id: active.id,
          data: { current: { type: "kanban-card", card: active } },
        },
        over,
      }) as unknown as Parameters<typeof getKanbanLaneDrop>[0];
    const doingCard = lanes[1]!.cards[0]!;
    expect(
      getKanbanLaneDrop(
        event({
          id: doingCard.id,
          data: { current: { type: "kanban-card", card: doingCard } },
        }),
        lanes,
      ),
    ).toEqual({
      kind: "status",
      cardId: active.id,
      optionId: lanes[1]!.optionId,
      fromOptionId: lanes[0]!.optionId,
    });
    const todoCard = lanes[0]!.cards[0]!;
    expect(
      getKanbanLaneDrop(
        event({
          id: todoCard.id,
          data: { current: { type: "kanban-card", card: todoCard } },
        }),
        lanes,
      ),
    ).toMatchObject({ kind: "reorder", cardId: active.id });
  });
});
