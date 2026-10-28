import { DndContext } from "@dnd-kit/core";
import { SortableContext } from "@dnd-kit/sortable";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { Card } from "@notrelix/work-management-core";
import { cardFixture, memberFixture } from "@notrelix/work-management-testing";
import { KanbanCard } from "../kanban-card";
import type { KanbanCardActions } from "../kanban-card-quick-actions";
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
