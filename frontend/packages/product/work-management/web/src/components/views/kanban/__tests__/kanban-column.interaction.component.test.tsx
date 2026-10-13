import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { erpKanbanLanes } from "../../../../verification/kanban-board-fixtures";
import { KanbanColumn } from "../kanban-column";
import { kanbanLaneCopy } from "../kanban-lane.copy";

const LOCALES = [
  ["vi", kanbanLaneCopy("vi")],
  ["en", kanbanLaneCopy("en")],
] as const;

function renderLane(locale: "vi" | "en", collapsed = false) {
  const lane = erpKanbanLanes()[1]!;
  const onCollapsedChange = vi.fn();
  const onHide = vi.fn();
  const onCreateItem = vi.fn();
  const view = renderPureUi(
    <KanbanColumn
      lane={lane}
      name={lane.name}
      copy={kanbanLaneCopy(locale)}
      filterActive={false}
      canAdd
      canDrag
      collapsed={collapsed}
      onCollapsedChange={onCollapsedChange}
      onHide={onHide}
      onOpenDetails={vi.fn()}
      onCreateItem={onCreateItem}
    />,
  );
  return { ...view, onCollapsedChange, onHide, onCreateItem };
}

describe("KanbanColumn (status lane)", () => {
  it("FUI[wm.kanban.column:column] names the lane, states its count and offers collapse and hide", () => {
    for (const [locale, copy] of LOCALES) {
      const { unmount, onCollapsedChange, onHide } = renderLane(locale);
      const region = screen.getByRole("region", { name: "Đang làm" });
      expect(
        within(region).getByRole("heading", { name: "Đang làm" }),
      ).toBeTruthy();
      expect(
        within(region).getByText(copy.count_other.replace("{{count}}", "3")),
      ).toBeTruthy();
      const options = within(region).getByRole("button", {
        name: copy.laneOptions.replace("{{lane}}", "Đang làm"),
      });
      fireEvent.keyDown(options, { key: "Enter" });
      fireEvent.click(screen.getByRole("menuitem", { name: copy.collapse }));
      expect(onCollapsedChange).toHaveBeenCalledWith(true);
      fireEvent.keyDown(options, { key: "Enter" });
      fireEvent.click(screen.getByRole("menuitem", { name: copy.hide }));
      expect(onHide).toHaveBeenCalled();
      unmount();
    }
  });

  it("FUI[wm.kanban.column:escape] closes the add field on Escape and returns focus to its button", async () => {
    for (const [locale, copy] of LOCALES) {
      const { unmount, onCreateItem } = renderLane(locale);
      const add = screen.getByRole("button", {
        name: copy.addTo.replace("{{lane}}", "Đang làm"),
      });
      fireEvent.click(add);
      const input = screen.getByRole("textbox", {
        name: copy.newItemLabel.replace("{{lane}}", "Đang làm"),
      });
      fireEvent.change(input, { target: { value: "Bỏ đi" } });
      fireEvent.keyDown(input, { key: "Escape" });
      expect(screen.queryByRole("textbox")).toBeNull();
      expect(onCreateItem).not.toHaveBeenCalled();
      await waitFor(() =>
        expect(document.activeElement?.textContent).toBe(
          copy.addTo.replace("{{lane}}", "Đang làm"),
        ),
      );
      unmount();
    }
  });

  it("shows a collapsed lane as a strip that expands again", () => {
    const { onCollapsedChange } = renderLane("vi", true);
    const region = screen.getByRole("region", { name: "Đang làm" });
    expect(region.querySelectorAll("[data-card-id]")).toHaveLength(0);
    fireEvent.click(
      within(region).getByRole("button", { name: "Mở rộng cột Đang làm" }),
    );
    expect(onCollapsedChange).toHaveBeenCalledWith(false);
  });
});
