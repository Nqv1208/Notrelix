import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { mainTableCopy } from "../main-table.copy";
import { TableBulkPhoneBar } from "../table-bulk-phone";

const copy = mainTableCopy("vi");

function phoneBar(over = false) {
  const onStatus = vi.fn();
  const onArchive = vi.fn();
  const onClear = vi.fn();
  renderPureUi(
    <TableBulkPhoneBar
      copy={copy}
      count={over ? 201 : 2}
      over={over}
      saving={false}
      statusOptions={[{ id: "s1", name: "Đang làm", category: "in_progress" }]}
      canEdit
      canArchive
      onStatus={onStatus}
      onDue={vi.fn()}
      onArchive={onArchive}
      onClear={onClear}
    />,
  );
  return { onStatus, onArchive, onClear };
}

describe("TableBulkPhoneBar", () => {
  it("shows the count, one Hành động button and close", () => {
    const { onClear } = phoneBar();
    expect(screen.getByText("2 đã chọn")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.bulk.clear }));
    expect(onClear).toHaveBeenCalledTimes(1);
  });

  it("opens the sheet and applies a status in one call", () => {
    const { onStatus } = phoneBar();
    fireEvent.click(screen.getByRole("button", { name: "Hành động" }));
    expect(screen.getByText("Hành động cho 2 việc")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Đổi trạng thái" }));
    fireEvent.click(screen.getByRole("button", { name: "Đang làm" }));
    expect(onStatus).toHaveBeenCalledWith("s1");
  });

  it("archives from the sheet", () => {
    const { onArchive } = phoneBar();
    fireEvent.click(screen.getByRole("button", { name: "Hành động" }));
    fireEvent.click(screen.getByRole("button", { name: "Lưu trữ" }));
    expect(onArchive).toHaveBeenCalledTimes(1);
  });

  it("past the limit the button is disabled and the way out stays open", () => {
    phoneBar(true);
    expect(
      (screen.getByRole("button", { name: "Hành động" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(
      (
        screen.getByRole("button", {
          name: copy.bulk.clear,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(false);
  });
});
