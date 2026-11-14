import { DndContext } from "@dnd-kit/core";
import { SortableContext } from "@dnd-kit/sortable";
import { expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { cardFixture } from "@notrelix/work-management-testing";
import { KanbanCard } from "../kanban-card";
import { kanbanCardCopy } from "../kanban-card.copy";

const VI = kanbanCardCopy("vi");

it("shows a refused move on the card and retries it", () => {
  const onRetry = vi.fn();
  const card = cardFixture({ title: "Chốt lịch đào tạo" });
  renderPureUi(
    <DndContext>
      <SortableContext items={[card.id]}>
        <KanbanCard
          card={card}
          copy={VI}
          onOpenDetails={vi.fn()}
          actions={{
            failureOf: () => ({ message: "Không chuyển được.", onRetry }),
          }}
        />
      </SortableContext>
    </DndContext>,
  );
  expect(screen.getByRole("alert").textContent).toContain("Không chuyển được.");
  fireEvent.click(screen.getByRole("button", { name: VI.retry }));
  expect(onRetry).toHaveBeenCalledTimes(1);
});
