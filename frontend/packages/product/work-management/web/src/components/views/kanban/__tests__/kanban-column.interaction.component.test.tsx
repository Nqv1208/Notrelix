import { describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { erpKanbanLanes } from "../../../../verification/kanban-board-fixtures";
import { statusOptionFormCopy } from "../../../status-option/status-option-form.copy";
import { KanbanColumn, type KanbanLaneEdit } from "../kanban-column";
import { kanbanLaneCopy } from "../kanban-lane.copy";

const LOCALES = [
  ["vi", kanbanLaneCopy("vi")],
  ["en", kanbanLaneCopy("en")],
] as const;

function renderLane(
  locale: "vi" | "en",
  collapsed = false,
  options: {
    edit?: boolean;
    optimistic?: boolean;
  } = {},
) {
  const base = erpKanbanLanes()[1]!;
  const lane = options.optimistic
    ? { ...base, optionId: "optimistic-option-1" }
    : base;
  const onEdit = vi.fn();
  const edit: KanbanLaneEdit | undefined = options.edit
    ? {
        formCopy: statusOptionFormCopy(locale),
        subject: "status",
        otherNames: ["Cần làm", "Chờ duyệt", "Hoàn thành"],
        onSubmit: onEdit,
      }
    : undefined;
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
      edit={edit}
      onCollapsedChange={onCollapsedChange}
      onHide={onHide}
      onOpenDetails={vi.fn()}
      onCreateItem={onCreateItem}
    />,
  );
  return { ...view, onCollapsedChange, onHide, onCreateItem, onEdit };
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
        expect(document.activeElement?.textContent).toContain(
          copy.addTo.replace("{{lane}}", "Đang làm"),
        ),
      );
      unmount();
    }
  });

  it("FUI[wm.kanban.column:edit] renames and recolours the lane's status from its menu", async () => {
    for (const locale of ["vi", "en"] as const) {
      const copy = kanbanLaneCopy(locale);
      const form = statusOptionFormCopy(locale);
      const { unmount, onEdit } = renderLane(locale, false, { edit: true });
      const options = screen.getByRole("button", {
        name: copy.laneOptions.replace("{{lane}}", "Đang làm"),
      });
      fireEvent.keyDown(options, { key: "Enter" });
      fireEvent.click(screen.getByRole("menuitem", { name: copy.editStatus }));
      const name = await screen.findByRole("textbox");
      expect((name as HTMLInputElement).value).toBe("Đang làm");
      // A name another lane has is refused; the lane's own name is fine.
      fireEvent.change(name, { target: { value: "Chờ duyệt" } });
      expect(screen.getByRole("alert")).toBeTruthy();
      fireEvent.change(name, { target: { value: "Đang triển khai" } });
      fireEvent.click(screen.getByRole("radio", { name: form.color_green }));
      fireEvent.click(screen.getByRole("button", { name: form.submitEdit }));
      expect(onEdit).toHaveBeenCalledWith({
        name: "Đang triển khai",
        color: "#2E8B57",
        category: "in_progress",
      });
      await waitFor(() => expect(screen.queryByRole("textbox")).toBeNull());
      unmount();
    }
  });

  it("offers no edit without the right, and none for a lane that exists only locally", () => {
    for (const options of [{}, { edit: true, optimistic: true }]) {
      const { unmount } = renderLane("vi", false, options);
      const trigger = screen.getByRole("button", {
        name: "Tuỳ chọn cột Đang làm",
      });
      fireEvent.keyDown(trigger, { key: "Enter" });
      expect(
        screen.queryByRole("menuitem", { name: "Sửa trạng thái" }),
      ).toBeNull();
      expect(
        screen.getByRole("menuitem", { name: "Thu gọn cột" }),
      ).toBeTruthy();
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
