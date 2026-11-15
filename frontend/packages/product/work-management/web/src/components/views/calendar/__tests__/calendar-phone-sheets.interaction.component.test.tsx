import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { calendarCopy } from "../calendar.copy";
import {
  CalendarActionSheet,
  CalendarDueSheet,
} from "../calendar-phone-sheets";
import type { CalendarEntry } from "../calendar-model";

const copy = calendarCopy("vi");

describe("the phone Calendar sheets", () => {
  it("saves the picked day and time of an undated item", () => {
    const onSetDue = vi.fn();
    renderPureUi(
      <CalendarDueSheet
        copy={copy}
        locale="vi"
        today="2026-10-12"
        zoneLabel="Giờ Hà Nội (GMT+7)"
        open
        onOpenChange={() => undefined}
        undatedTotal={1}
        undated={[{ id: "a", itemKey: "ERP-153", title: "Rà soát hợp đồng" }]}
        filterActive={false}
        canEdit
        onSetDue={onSetDue}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", { name: /Đặt hạn cho ERP-153/ }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Ngày mai" }));
    fireEvent.change(screen.getByRole("combobox"), {
      target: { value: String(9 * 60 + 30) },
    });
    fireEvent.click(screen.getByRole("button", { name: "Lưu hạn" }));
    expect(onSetDue).toHaveBeenCalledWith("a", "2026-10-13", 570);
  });

  it("moves an item to another status from its row sheet", () => {
    const onChange = vi.fn();
    const entry: CalendarEntry = {
      id: "e",
      kind: "item",
      itemKey: "ERP-140",
      title: "Mẫu báo cáo",
      tone: "review",
      startDay: "2026-10-15",
      endDay: "2026-10-15",
      dueMinutes: 660,
      note: "",
      movable: true,
    };
    renderPureUi(
      <CalendarActionSheet
        copy={copy}
        locale="vi"
        entry={entry}
        canEdit
        onClose={() => undefined}
        onOpenItem={() => undefined}
        onChangeDue={() => undefined}
        statusMenu={{
          options: [
            { id: "todo", label: "Cần làm", color: "#888" },
            { id: "review", label: "Chờ duyệt", color: "#d99a00" },
          ],
          currentOf: () => "review",
          onChange,
        }}
      />,
    );
    fireEvent.click(screen.getByRole("menuitem", { name: /Chuyển sang…/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: /Cần làm/ }));
    expect(onChange).toHaveBeenCalledWith(entry, "todo");
  });
});
