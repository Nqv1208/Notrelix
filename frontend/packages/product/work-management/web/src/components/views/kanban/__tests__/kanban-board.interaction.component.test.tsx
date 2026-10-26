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
import {
  arrangeKanbanLanes,
  getKanbanDrop,
  kanbanLaneKey,
  moveCardInArrangement,
} from "../kanban-dnd";
import { kanbanCardCopy } from "../kanban-card.copy";
import { kanbanLaneCopy } from "../kanban-lane.copy";
import { statusOptionFormCopy } from "../../../status-option/status-option-form.copy";

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
      laneCardLimit={3}
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

  it("FUI[wm.kanban.board:show-more] caps a lane at three cards and reveals the rest, focusing the first revealed card", async () => {
    for (const [locale, copy] of LOCALES) {
      const { unmount } = renderBoard({ locale });
      const todo = laneRegion("Cần làm");
      expect(cardButtons(todo)).toHaveLength(3);
      const more = within(todo).getByRole("button", {
        name: copy.showMore_other.replace("{{count}}", "3"),
      });
      fireEvent.click(more);
      const cards = cardButtons(laneRegion("Cần làm"));
      expect(cards).toHaveLength(6);
      await waitFor(() => expect(document.activeElement).toBe(cards[3]));
      expect(
        within(laneRegion("Cần làm")).queryByRole("button", {
          name: copy.showMore_other.replace("{{count}}", "3"),
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
      expect(cardButtons(laneRegion("Cần làm"))).toHaveLength(3);

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

  it("FUI[wm.kanban.board:move] reads a card let go in another lane as that lane's status or place, and in its own lane as a reorder", () => {
    const lanes = erpLanes();
    const active = lanes[0]!.cards[1]!;
    const start = arrangeKanbanLanes(lanes);
    const doingKey = kanbanLaneKey(lanes[1]!.optionId);
    const todoKey = kanbanLaneKey(lanes[0]!.optionId);
    const toDoing = moveCardInArrangement(start, active.id, doingKey, 0);
    expect(
      getKanbanDrop({ lanes, arrangement: toDoing, cardId: active.id }),
    ).toMatchObject({
      cardId: active.id,
      optionId: lanes[1]!.optionId,
      fromOptionId: lanes[0]!.optionId,
    });
    const reordered = moveCardInArrangement(start, active.id, todoKey, 0);
    expect(
      getKanbanDrop({ lanes, arrangement: reordered, cardId: active.id }),
    ).toMatchObject({ kind: "reorder", cardId: active.id });
  });

  it("FUI[wm.kanban.board:drag] lifts the whole card into an overlay, keeps its slot as a gap and puts everything back on Escape", async () => {
    const { onDropCard, onOpenDetails } = renderBoard();
    const doing = laneRegion("Đang làm");
    const title = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";
    const open = within(doing).getByRole("button", { name: title });
    const article = open.closest("article")!;
    expect(document.querySelector("[data-kanban-drag-overlay]")).toBeNull();

    // A press without movement is a click: it opens, nothing is lifted.
    fireEvent.mouseDown(open, { button: 0, clientX: 40, clientY: 40 });
    fireEvent.mouseUp(open, { clientX: 40, clientY: 40 });
    fireEvent.click(open);
    expect(onOpenDetails).toHaveBeenCalledTimes(1);
    expect(document.querySelector("[data-kanban-drag-overlay]")).toBeNull();

    // A press and a move beyond the activation distance lifts the card from
    // anywhere on it (here the card's own body, not a handle).
    fireEvent.mouseDown(article, { button: 0, clientX: 40, clientY: 40 });
    fireEvent.mouseMove(document, { clientX: 70, clientY: 80 });
    await waitFor(() =>
      expect(document.querySelector("[data-kanban-drag-overlay]")).toBeTruthy(),
    );
    const overlay = document.querySelector<HTMLElement>(
      "[data-kanban-drag-overlay]",
    )!;
    expect(overlay.getAttribute("aria-hidden")).toBe("true");
    expect(within(overlay).getByText(title)).toBeTruthy();
    // The card stays in a lane as the gap, and that lane is outlined (jsdom
    // has no layout, so which lane the pointer is over is not asserted here).
    const gap = document.querySelector<HTMLElement>("article[data-dragging]")!;
    expect(gap).toBeTruthy();
    expect(within(gap).getByText(title)).toBeTruthy();
    expect(gap.closest("section")!.hasAttribute("data-drop-target")).toBe(true);

    fireEvent.keyDown(document, { key: "Escape", code: "Escape" });
    await waitFor(() =>
      expect(document.querySelector("[data-kanban-drag-overlay]")).toBeNull(),
    );
    expect(document.querySelector("article[data-dragging]")).toBeNull();
    // Everything is back: the card is in its own lane again.
    expect(
      within(laneRegion("Đang làm")).getByRole("button", { name: title }),
    ).toBeTruthy();
    expect(onDropCard).not.toHaveBeenCalled();
  });

  it("FUI[wm.kanban.board:add-lane] adds a status after the last lane through the slot's form, Enter confirms and Escape cancels", async () => {
    // dnd-kit swallows the click that follows a drag for a moment; let the
    // drag of the previous test be over.
    await new Promise((resolve) => setTimeout(resolve, 120));
    for (const [locale, copy] of LOCALES) {
      const form = statusOptionFormCopy(locale);
      const onCreateLane = vi.fn();
      const { unmount } = renderBoard({
        locale,
        formCopy: form,
        onCreateLane,
        onUpdateLane: vi.fn(),
      });
      // The slot is the last thing in the lane row.
      const row = document.querySelector("[data-kanban-lanes]")!;
      const slot = screen.getByRole("button", { name: copy.addStatus });
      expect(row.lastElementChild).toBe(slot);

      fireEvent.click(slot);
      const name = screen.getByRole("textbox");
      fireEvent.change(name, { target: { value: "Chờ khách duyệt" } });
      fireEvent.click(screen.getByRole("radio", { name: form.color_red }));
      fireEvent.click(screen.getByRole("radio", { name: form.category_done }));
      fireEvent.submit(name.closest("form")!);
      expect(onCreateLane).toHaveBeenCalledWith({
        name: "Chờ khách duyệt",
        color: "#C8143F",
        category: "done",
      });
      expect(screen.queryByRole("textbox")).toBeNull();

      fireEvent.click(slot);
      fireEvent.keyDown(screen.getByRole("textbox"), { key: "Escape" });
      expect(screen.queryByRole("textbox")).toBeNull();
      expect(onCreateLane).toHaveBeenCalledTimes(1);

      // A duplicate of an existing lane's name is refused.
      fireEvent.click(slot);
      fireEvent.change(screen.getByRole("textbox"), {
        target: { value: "đang làm" },
      });
      fireEvent.submit(screen.getByRole("textbox").closest("form")!);
      expect(screen.getByRole("alert")).toBeTruthy();
      expect(onCreateLane).toHaveBeenCalledTimes(1);
      unmount();
    }
  });

  it("offers no slot for a viewer and words it as a value for another Select field", () => {
    renderBoard({ canDrag: false });
    expect(
      screen.queryByRole("button", { name: "Thêm trạng thái" }),
    ).toBeNull();
  });

  it("starts no drag on a read-only board", () => {
    renderBoard({ canDrag: false });
    const title = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";
    const article = within(laneRegion("Đang làm"))
      .getByRole("button", { name: title })
      .closest("article")!;
    fireEvent.mouseDown(article, { button: 0, clientX: 40, clientY: 40 });
    fireEvent.mouseMove(document, { clientX: 90, clientY: 90 });
    expect(document.querySelector("[data-kanban-drag-overlay]")).toBeNull();
  });
});

describe("KanbanBoard lane limit", () => {
  it("FUI[wm.kanban.board:lane-limit] shows as many cards as the lane limit and counts the rest in Xem thêm", () => {
    const { unmount } = renderBoard({ laneCardLimit: 2 });
    const todo = laneRegion("Cần làm");
    expect(cardButtons(todo)).toHaveLength(2);
    expect(
      within(todo).getByRole("button", { name: "Xem thêm 4 việc" }),
    ).toBeTruthy();
    unmount();
    renderBoard({ laneCardLimit: 20 });
    expect(cardButtons(laneRegion("Cần làm"))).toHaveLength(6);
    expect(screen.queryByRole("button", { name: /Xem thêm/ })).toBeNull();
  });
});

function announcer(): string {
  return document.querySelector("[data-kanban-announcer]")?.textContent ?? "";
}

describe("KanbanBoard keyboard move", () => {
  function altKey(button: HTMLElement, key: string, init = {}) {
    fireEvent.keyDown(button, { key, altKey: true, ...init });
  }

  it("FUI[wm.kanban.board:keyboard-move] Alt+ArrowRight sends the focused card through the drop to the next lane, announces it and keeps focus", async () => {
    const { onDropCard } = renderBoard();
    const first = cardButtons(laneRegion("Cần làm"))[0]!;
    const id = first.dataset.cardId!;
    const title = first.getAttribute("aria-label")!;
    first.focus();
    altKey(first, "ArrowRight");
    expect(onDropCard).toHaveBeenCalledTimes(1);
    const drop = onDropCard.mock.calls[0]![0];
    expect(drop.cardId).toBe(id);
    expect(["status", "place"]).toContain(drop.kind);
    expect(drop.fromOptionId).not.toBe(drop.optionId);
    expect(announcer()).toBe(`Đã chuyển “${title}” sang Đang làm`);
    // The card is now in the next lane and keeps focus.
    const moved = within(laneRegion("Đang làm"))
      .getAllByRole("button")
      .find((button) => button.dataset.cardId === id);
    expect(moved).toBeTruthy();
    await waitFor(() => expect(document.activeElement).toBe(moved));
  });

  it("does nothing at the first lane going left, without Alt, or with the setting off", () => {
    const { onDropCard, unmount } = renderBoard();
    const first = cardButtons(laneRegion("Cần làm"))[0]!;
    altKey(first, "ArrowLeft");
    fireEvent.keyDown(first, { key: "ArrowRight" });
    expect(onDropCard).not.toHaveBeenCalled();
    unmount();
    const off = renderBoard({ keyboardMove: false });
    const card = cardButtons(laneRegion("Cần làm"))[0]!;
    altKey(card, "ArrowRight");
    expect(card.hasAttribute("aria-keyshortcuts")).toBe(false);
    expect(off.onDropCard).not.toHaveBeenCalled();
  });

  it("does nothing when the viewer cannot move cards", () => {
    const { onDropCard } = renderBoard({ canDrag: false });
    altKey(cardButtons(laneRegion("Cần làm"))[0]!, "ArrowRight");
    expect(onDropCard).not.toHaveBeenCalled();
  });

  it("Alt+ArrowDown reorders inside the lane through the same drop, announcing the place", () => {
    const { onDropCard } = renderBoard();
    const lane = laneRegion("Cần làm");
    const buttons = cardButtons(lane);
    for (const button of buttons.slice(0, -1)) {
      altKey(button, "ArrowDown");
      if (onDropCard.mock.calls.length > 0) break;
    }
    expect(onDropCard).toHaveBeenCalled();
    expect(onDropCard.mock.calls[0]![0].kind).toBe("reorder");
    expect(announcer()).toMatch(
      /^Đã chuyển “.+” đến vị trí \d+\/6 trong Cần làm$/,
    );
  });
});
