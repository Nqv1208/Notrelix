import { DndContext } from "@dnd-kit/core";
import { SortableContext } from "@dnd-kit/sortable";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { Card } from "@notrelix/work-management-core";
import { cardFixture, memberFixture } from "@notrelix/work-management-testing";
import { KanbanCard } from "../kanban-card";
import type { KanbanCardActions } from "../kanban-card-quick-actions";
import type { KanbanCardMove } from "../kanban-card-move-menu";
import { kanbanCardCopy } from "../kanban-card.copy";

const VI = kanbanCardCopy("vi");

function renderCard(
  actions: KanbanCardActions | undefined,
  overrides: Partial<Card> = {},
) {
  const card = cardFixture({ title: "Soạn hợp đồng", ...overrides });
  const onOpenDetails = vi.fn();
  renderPureUi(
    <DndContext>
      <SortableContext items={[card.id]}>
        <KanbanCard
          card={card}
          copy={VI}
          actions={actions}
          onOpenDetails={onOpenDetails}
        />
      </SortableContext>
    </DndContext>,
  );
  return { card, onOpenDetails };
}

const LANES: KanbanCardMove["lanes"] = [
  { id: "todo", title: "Cần làm", category: "todo", color: null },
  { id: "doing", title: "Đang làm", category: "in_progress", color: null },
  { id: "review", title: "Chờ duyệt", category: "review", color: null },
];

function openOptions() {
  fireEvent.keyDown(screen.getByRole("button", { name: "Thêm tuỳ chọn" }), {
    key: "Enter",
  });
}

describe("KanbanCard Chuyển sang", () => {
  it("names the current column on the item and moves the card to the column picked in the submenu", () => {
    const onMove = vi.fn();
    const { card } = renderCard({
      move: { lanes: LANES, currentLaneId: () => "doing", onMove },
    });
    openOptions();
    const item = screen.getByRole("menuitem", { name: "Chuyển sang Đang làm" });
    fireEvent.keyDown(item, { key: "ArrowRight" });
    const rows = screen.getAllByRole("menuitemradio");
    expect(rows.map((row) => row.textContent)).toEqual([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
    ]);
    expect(rows.map((row) => row.getAttribute("aria-checked"))).toEqual([
      "false",
      "true",
      "false",
    ]);
    // The current column is not a move.
    fireEvent.click(rows[1]!);
    expect(onMove).not.toHaveBeenCalled();
    openOptions();
    fireEvent.keyDown(
      screen.getByRole("menuitem", { name: "Chuyển sang Đang làm" }),
      { key: "ArrowRight" },
    );
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Chờ duyệt" }));
    expect(onMove).toHaveBeenCalledTimes(1);
    expect(onMove).toHaveBeenCalledWith(
      expect.objectContaining({ id: card.id }),
      "review",
    );
  });

  it("shows the item disabled with its reason when the viewer may not move cards", () => {
    const onMove = vi.fn();
    renderCard({
      onCopyLink: vi.fn(),
      move: {
        lanes: LANES,
        currentLaneId: () => "doing",
        onMove,
        disabledReason: "Bạn chỉ được xem board này, không chuyển thẻ được",
      },
    });
    openOptions();
    const item = screen.getByRole("menuitem", { name: "Chuyển sang" });
    expect(item.getAttribute("aria-disabled")).toBe("true");
    expect(
      screen.getByText("Bạn chỉ được xem board này, không chuyển thẻ được"),
    ).toBeTruthy();
    fireEvent.click(item);
    expect(onMove).not.toHaveBeenCalled();
  });
});

describe("KanbanCard quick actions", () => {
  it("offers assign to me, the due date and options as buttons that stay reachable by Tab", () => {
    renderCard({
      currentUserId: "user-me",
      onAssignMe: vi.fn(),
      onSetDue: vi.fn(),
      onCopyLink: vi.fn(),
    });
    expect(screen.getByRole("button", { name: "Giao cho tôi" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Đặt ngày hạn" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Thêm tuỳ chọn" })).toBeTruthy();
  });

  it("assigns the card to me from the button and from A on the focused card, without opening it", () => {
    const onAssignMe = vi.fn();
    const { card, onOpenDetails } = renderCard({
      currentUserId: "user-me",
      onAssignMe,
    });
    fireEvent.click(screen.getByRole("button", { name: "Giao cho tôi" }));
    expect(onAssignMe).toHaveBeenCalledTimes(1);
    expect(onAssignMe).toHaveBeenCalledWith(
      expect.objectContaining({ id: card.id }),
    );

    fireEvent.keyDown(screen.getByRole("button", { name: card.title }), {
      key: "a",
    });
    expect(onAssignMe).toHaveBeenCalledTimes(2);
    expect(onOpenDetails).not.toHaveBeenCalled();
  });

  it("does not offer assign to me when the member is already assigned", () => {
    renderCard(
      { currentUserId: "user-me", onAssignMe: vi.fn() },
      {
        members: [memberFixture({ id: "m-1", userId: "user-me", name: "Tôi" })],
      },
    );
    expect(screen.queryByRole("button", { name: "Giao cho tôi" })).toBeNull();
  });

  it("opens the due date picker with D and writes the chosen day", () => {
    const onSetDue = vi.fn();
    const { card } = renderCard({ onSetDue });
    fireEvent.keyDown(screen.getByRole("button", { name: card.title }), {
      key: "d",
    });
    const input = screen.getByLabelText("Ngày hạn") as HTMLInputElement;
    fireEvent.change(input, { target: { value: "2026-11-05" } });
    expect(onSetDue).toHaveBeenCalledWith(
      expect.objectContaining({ id: card.id }),
      "2026-11-05",
    );
  });

  it("shows no actions on a read-only board", () => {
    renderCard(undefined);
    expect(screen.queryByRole("button", { name: "Đặt ngày hạn" })).toBeNull();
  });
});
