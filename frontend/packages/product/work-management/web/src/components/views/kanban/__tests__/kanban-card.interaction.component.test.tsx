import { DndContext } from "@dnd-kit/core";
import { SortableContext } from "@dnd-kit/sortable";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { kanbanDefaultScenario } from "@notrelix/work-management-testing";
import { KanbanCard } from "../kanban-card";

describe("KanbanCard interactions", () => {
  it("opens by pointer and keyboard without bubbling nested actions", () => {
    const scenario = kanbanDefaultScenario();
    const card = scenario.columns[0]!.cards[0]!;
    const onOpenDetails = vi.fn();
    renderPureUi(
      <DndContext>
        <SortableContext items={[card.id]}>
          <KanbanCard
            board={scenario.board}
            card={card}
            onOpenDetails={onOpenDetails}
            onDuplicate={vi.fn()}
            onDelete={vi.fn()}
          />
        </SortableContext>
      </DndContext>,
    );
    const root = screen.getByRole("button", { name: card.title });
    fireEvent.click(root);
    fireEvent.keyDown(root, { key: "Enter" });
    fireEvent.keyDown(root, { key: " " });
    expect(onOpenDetails).toHaveBeenCalledTimes(3);
    fireEvent.click(screen.getByRole("button", { name: `Move ${card.title}` }));
    expect(onOpenDetails).toHaveBeenCalledTimes(3);
  });
});
