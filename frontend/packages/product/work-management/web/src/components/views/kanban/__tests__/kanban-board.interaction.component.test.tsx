import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { kanbanDefaultScenario } from "@notrelix/work-management-testing";
import { KanbanBoard } from "../kanban-board";
import { getKanbanCardMove } from "../kanban-dnd";

describe("KanbanBoard interactions", () => {
  it("submits a card through the injected callback", () => {
    const scenario = kanbanDefaultScenario();
    const onCreateCard = vi.fn();
    renderPureUi(
      <KanbanBoard
        board={scenario.board}
        columns={scenario.columns}
        onOpenDetails={vi.fn()}
        onMoveCard={vi.fn()}
        onReorderColumns={vi.fn()}
        onAdd={vi.fn()}
        onRenameColumn={vi.fn()}
        onColorChangeColumn={vi.fn()}
        onDeleteColumn={vi.fn()}
        onDuplicateCard={vi.fn()}
        onDeleteCard={vi.fn()}
        onCreateCard={onCreateCard}
      />,
    );
    fireEvent.click(
      screen.getAllByRole("button", { name: /Add card to Column 1/i })[0]!,
    );
    fireEvent.change(
      screen.getByRole("textbox", { name: /New card title for Column 1/i }),
      { target: { value: "Storybook interaction card" } },
    );
    fireEvent.click(screen.getByRole("button", { name: "Add card" }));
    expect(onCreateCard).toHaveBeenCalledWith(
      scenario.columns[0]!.id,
      "Storybook interaction card",
      expect.any(Number),
    );
    expect(Number.isFinite(onCreateCard.mock.calls[0]?.[2])).toBe(true);
  });

  it("computes representative card move positions from drag-end data", () => {
    const scenario = kanbanDefaultScenario();
    const active = scenario.columns[0]!.cards[0]!;
    const over = scenario.columns[1]!.cards[0]!;

    const move = getKanbanCardMove(
      {
        active: {
          id: active.id,
          data: { current: { type: "kanban-card", card: active } },
        },
        over: {
          id: over.id,
          data: { current: { type: "kanban-card", card: over } },
        },
      } as unknown as Parameters<typeof getKanbanCardMove>[0],
      scenario.columns,
    );

    expect(move).toMatchObject({
      cardId: active.id,
      listId: over.listId,
    });
    expect(Number.isFinite(move?.position)).toBe(true);
  });
});
