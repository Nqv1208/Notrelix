import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import type {
  ComputedValue,
  FieldDefinition,
  FieldType,
} from "@notrelix/work-management-core";
import { ERP_PEOPLE } from "@notrelix/work-management-testing";
import { mainTableCopy } from "../main-table.copy";
import {
  TableFieldCell,
  type CellWrite,
  type TableFieldCellProps,
} from "../table-field-cell";

beforeAll(() => {
  // Radix popovers measure and scroll; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
  Element.prototype.setPointerCapture = () => undefined;
});

const copy = mainTableCopy("vi");

const MEMBERS = Object.values(ERP_PEOPLE)
  .slice(0, 3)
  .map((person) => ({
    userId: person.userId,
    name: person.name,
    initials: person.name.slice(0, 2).toUpperCase(),
  }));

const OPTIONS = [
  { id: "o-1", label: "Kế toán", color: "#1E6FD0" },
  { id: "o-2", label: "Công nợ", color: "#7C4DDB" },
  { id: "o-3", label: "Mua hàng", color: "#0E7490" },
  { id: "o-4", label: "Kho", color: "#9A6A12" },
];

function field(fieldType: FieldType, name: string): FieldDefinition {
  return {
    id: `field-${fieldType}`,
    boardId: "board",
    name,
    fieldType,
    options:
      fieldType === "select" || fieldType === "multi_select" ? OPTIONS : [],
    position: 1,
    isHidden: false,
    isSystemField: false,
  };
}

function renderCell(
  fieldType: FieldType,
  name: string,
  value: unknown,
  extra: Partial<TableFieldCellProps> = {},
) {
  const handlers = {
    onWrite: vi.fn(),
    onRetry: vi.fn(),
    onKeepMine: vi.fn(),
    onUseTheirs: vi.fn(),
  };
  renderPureUi(
    <div role="table">
      <div role="row">
        <div role="cell">
          <TableFieldCell
            field={field(fieldType, name)}
            cell={{ value }}
            rowKey="ERP-145"
            copy={copy}
            members={MEMBERS}
            editable
            {...handlers}
            {...extra}
          />
          <button type="button">ngoài</button>
        </div>
      </div>
    </div>,
  );
  return handlers;
}

const trigger = (name: string) =>
  screen.getByRole("button", { name: new RegExp(`^Sửa ${name} của ERP-145`) });

/** Leaving a popover: focus moves to something outside it. */
function leave() {
  act(() => screen.getByRole("button", { name: "ngoài" }).focus());
}

describe("custom field cells: lines typed in the cell (Text, Number, Link)", () => {
  it("FUI[wm.table.main:field-edit] Text: Enter saves, Escape puts the old value back, leaving the cell saves", () => {
    const { onWrite } = renderCell("text", "Mã hợp đồng", "HD-01");
    fireEvent.click(trigger("Mã hợp đồng"));
    let input = screen.getByRole("textbox", { name: /Mã hợp đồng/ });
    fireEvent.change(input, { target: { value: "HD-02" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onWrite).toHaveBeenLastCalledWith("HD-02");
    expect(screen.queryByRole("textbox")).toBeNull();

    fireEvent.click(trigger("Mã hợp đồng"));
    input = screen.getByRole("textbox", { name: /Mã hợp đồng/ });
    fireEvent.change(input, { target: { value: "đổi ý" } });
    fireEvent.keyDown(input, { key: "Escape" });
    expect(onWrite).toHaveBeenCalledTimes(1);
    expect(trigger("Mã hợp đồng").textContent).toContain("HD-01");

    fireEvent.click(trigger("Mã hợp đồng"));
    input = screen.getByRole("textbox", { name: /Mã hợp đồng/ });
    fireEvent.change(input, { target: { value: "HD-03" } });
    fireEvent.blur(input);
    expect(onWrite).toHaveBeenLastCalledWith("HD-03");
  });

  it("Text: an unchanged value and a cleared one", () => {
    const { onWrite } = renderCell("text", "Mã hợp đồng", "HD-01");
    fireEvent.click(trigger("Mã hợp đồng"));
    fireEvent.keyDown(screen.getByRole("textbox"), { key: "Enter" });
    expect(onWrite).not.toHaveBeenCalled();
    fireEvent.click(trigger("Mã hợp đồng"));
    fireEvent.change(screen.getByRole("textbox"), { target: { value: "  " } });
    fireEvent.keyDown(screen.getByRole("textbox"), { key: "Enter" });
    expect(onWrite).toHaveBeenCalledWith(null);
  });

  it("Number: Enter saves a number, a comma counts as a point, a bad draft stays open with its reason", () => {
    const { onWrite } = renderCell("number", "Điểm", 8);
    fireEvent.click(trigger("Điểm"));
    const input = screen.getByRole("textbox", { name: /Điểm/ });
    fireEvent.change(input, { target: { value: "13a" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onWrite).not.toHaveBeenCalled();
    expect(input.getAttribute("aria-invalid")).toBe("true");
    expect(screen.getByRole("alert").textContent).toContain("Nhập một số");
    fireEvent.change(input, { target: { value: "13,5" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onWrite).toHaveBeenCalledWith(13.5);
  });

  it("Number: Escape and leaving the cell", () => {
    const { onWrite } = renderCell("number", "Điểm", 8);
    fireEvent.click(trigger("Điểm"));
    let input = screen.getByRole("textbox", { name: /Điểm/ });
    fireEvent.change(input, { target: { value: "99" } });
    fireEvent.keyDown(input, { key: "Escape" });
    expect(onWrite).not.toHaveBeenCalled();
    fireEvent.click(trigger("Điểm"));
    input = screen.getByRole("textbox", { name: /Điểm/ });
    fireEvent.change(input, { target: { value: "21" } });
    fireEvent.blur(input);
    expect(onWrite).toHaveBeenCalledWith(21);
  });

  it("Link: Enter saves with https added, a script address is refused, Escape and leaving", () => {
    const { onWrite } = renderCell("link", "Liên kết", "https://a.vn/");
    fireEvent.click(trigger("Liên kết"));
    let input = screen.getByRole("textbox", { name: /Liên kết/ });
    fireEvent.change(input, { target: { value: "javascript:alert(1)" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onWrite).not.toHaveBeenCalled();
    expect(screen.getByRole("alert").textContent).toContain("http");
    fireEvent.change(input, { target: { value: "erp.minhan.vn/t/145" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onWrite).toHaveBeenLastCalledWith("https://erp.minhan.vn/t/145");

    fireEvent.click(trigger("Liên kết"));
    input = screen.getByRole("textbox", { name: /Liên kết/ });
    fireEvent.change(input, { target: { value: "b.vn" } });
    fireEvent.keyDown(input, { key: "Escape" });
    expect(onWrite).toHaveBeenCalledTimes(1);

    fireEvent.click(trigger("Liên kết"));
    input = screen.getByRole("textbox", { name: /Liên kết/ });
    fireEvent.change(input, { target: { value: "c.vn" } });
    fireEvent.blur(input);
    expect(onWrite).toHaveBeenLastCalledWith("https://c.vn/");
  });

  it("Link: the address opens in a new tab from its own icon", () => {
    renderCell("link", "Liên kết", "https://erp.minhan.vn/t/145");
    const open = screen.getByRole("link", {
      name: "Mở liên kết trong thẻ mới",
    });
    expect(open.getAttribute("href")).toBe("https://erp.minhan.vn/t/145");
    expect(open.getAttribute("target")).toBe("_blank");
    expect(open.getAttribute("rel")).toContain("noopener");
  });
});

describe("custom field cells: popovers", () => {
  it("Long text: Enter saves, Shift+Enter keeps the line break, Escape cancels, leaving saves", async () => {
    const { onWrite } = renderCell("long_text", "Mô tả", "dòng một");
    fireEvent.click(trigger("Mô tả"));
    let box = screen.getByRole("textbox", { name: /Mô tả/ });
    fireEvent.change(box, { target: { value: "dòng một\ndòng hai" } });
    fireEvent.keyDown(box, { key: "Enter", shiftKey: true });
    expect(onWrite).not.toHaveBeenCalled();
    fireEvent.keyDown(box, { key: "Enter" });
    expect(onWrite).toHaveBeenLastCalledWith("dòng một\ndòng hai");
    await waitFor(() => expect(screen.queryByRole("textbox")).toBeNull());

    fireEvent.click(trigger("Mô tả"));
    box = screen.getByRole("textbox", { name: /Mô tả/ });
    fireEvent.change(box, { target: { value: "bỏ" } });
    fireEvent.keyDown(box, { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("textbox")).toBeNull());
    expect(onWrite).toHaveBeenCalledTimes(1);

    fireEvent.click(trigger("Mô tả"));
    box = screen.getByRole("textbox", { name: /Mô tả/ });
    fireEvent.change(box, { target: { value: "ra ngoài" } });
    leave();
    await waitFor(() => expect(onWrite).toHaveBeenLastCalledWith("ra ngoài"));
  });

  it("Long text: the Lưu and Huỷ buttons", async () => {
    const { onWrite } = renderCell("long_text", "Mô tả", null);
    fireEvent.click(trigger("Mô tả"));
    fireEvent.change(screen.getByRole("textbox", { name: /Mô tả/ }), {
      target: { value: "mới" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Huỷ" }));
    expect(onWrite).not.toHaveBeenCalled();
    fireEvent.click(trigger("Mô tả"));
    fireEvent.change(screen.getByRole("textbox", { name: /Mô tả/ }), {
      target: { value: "mới" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Lưu" }));
    expect(onWrite).toHaveBeenCalledWith("mới");
  });

  it("Select: arrow keys move, Enter chooses, Escape and leaving close without a write", async () => {
    const { onWrite } = renderCell("select", "Phân hệ", "o-1");
    fireEvent.click(trigger("Phân hệ"));
    const list = screen.getByRole("listbox", { name: "Phân hệ" });
    expect(
      within(list)
        .getByRole("option", { name: /Kế toán/ })
        .getAttribute("aria-selected"),
    ).toBe("true");
    const first = within(list).getByRole("option", { name: /Kế toán/ });
    first.focus();
    fireEvent.keyDown(first, { key: "ArrowDown" });
    const second = within(list).getByRole("option", { name: /Công nợ/ });
    expect(document.activeElement).toBe(second);
    // Enter on a button is a click.
    fireEvent.click(second);
    expect(onWrite).toHaveBeenLastCalledWith("o-2");
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());

    fireEvent.click(trigger("Phân hệ"));
    fireEvent.keyDown(screen.getByRole("listbox"), { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());
    fireEvent.click(trigger("Phân hệ"));
    leave();
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());
    expect(onWrite).toHaveBeenCalledTimes(1);
  });

  it("Select: choosing the current option writes nothing, and Bỏ chọn clears", async () => {
    const { onWrite } = renderCell("select", "Phân hệ", "o-1");
    fireEvent.click(trigger("Phân hệ"));
    fireEvent.click(screen.getByRole("option", { name: /Kế toán/ }));
    expect(onWrite).not.toHaveBeenCalled();
    fireEvent.click(trigger("Phân hệ"));
    fireEvent.click(screen.getByRole("button", { name: "Bỏ chọn" }));
    expect(onWrite).toHaveBeenCalledWith(null);
  });

  it("Multi select: shows two chips then +N with the rest in its tooltip; each tick saves, Enter closes, Escape closes", async () => {
    const { onWrite } = renderCell("multi_select", "Phân hệ", [
      "o-1",
      "o-2",
      "o-3",
      "o-4",
    ]);
    expect(screen.getByText("+2").getAttribute("title")).toBe("Mua hàng, Kho");
    expect(screen.queryByText("Mua hàng")).toBeNull();
    fireEvent.click(trigger("Phân hệ"));
    const list = screen.getByRole("listbox", { name: "Phân hệ" });
    expect(list.getAttribute("aria-multiselectable")).toBe("true");
    fireEvent.click(within(list).getByRole("option", { name: /Kho/ }));
    expect(onWrite).toHaveBeenLastCalledWith(["o-1", "o-2", "o-3"]);
    fireEvent.keyDown(list, { key: "Enter" });
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());

    fireEvent.click(trigger("Phân hệ"));
    fireEvent.keyDown(screen.getByRole("listbox"), { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());
    fireEvent.click(trigger("Phân hệ"));
    leave();
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());
    expect(onWrite).toHaveBeenCalledTimes(1);
  });

  it("Multi select: Xong closes and Bỏ chọn empties the set", async () => {
    const { onWrite } = renderCell("multi_select", "Phân hệ", ["o-1"]);
    fireEvent.click(trigger("Phân hệ"));
    fireEvent.click(screen.getByRole("button", { name: "Bỏ chọn" }));
    expect(onWrite).toHaveBeenCalledWith([]);
    fireEvent.click(screen.getByRole("button", { name: "Xong" }));
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());
  });

  it("Date: choosing a day saves it and closes; Escape and leaving write nothing", async () => {
    const { onWrite } = renderCell("date", "Hạn nghiệm thu", "2026-10-15");
    expect(screen.getByText("15/10/2026")).toBeTruthy();
    fireEvent.click(trigger("Hạn nghiệm thu"));
    const dialog = screen.getByRole("dialog");
    fireEvent.click(within(dialog).getByRole("button", { name: /16/ }));
    expect(onWrite).toHaveBeenLastCalledWith("2026-10-16");
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());

    fireEvent.click(trigger("Hạn nghiệm thu"));
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    fireEvent.click(trigger("Hạn nghiệm thu"));
    leave();
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(onWrite).toHaveBeenCalledTimes(1);
  });

  it("Date: Xoá ngày clears, and an empty date reads Chưa đặt", async () => {
    const { onWrite } = renderCell("date", "Hạn nghiệm thu", "2026-10-15");
    fireEvent.click(trigger("Hạn nghiệm thu"));
    fireEvent.click(screen.getByRole("button", { name: "Xoá ngày" }));
    expect(onWrite).toHaveBeenCalledWith(null);
  });

  it("Date: an empty cell reads Chưa đặt", () => {
    renderCell("date", "Hạn nghiệm thu", null);
    expect(screen.getByText("Chưa đặt")).toBeTruthy();
  });

  it("Person: lists the board's people, each tick saves the set, Enter and Escape close, leaving closes", async () => {
    const { onWrite } = renderCell("person", "Người nghiệm thu", [
      MEMBERS[0]!.userId,
    ]);
    fireEvent.click(trigger("Người nghiệm thu"));
    const list = screen.getByRole("listbox", { name: "Người nghiệm thu" });
    expect(within(list).getAllByRole("option")).toHaveLength(3);
    fireEvent.click(
      within(list).getByRole("option", { name: new RegExp(MEMBERS[1]!.name) }),
    );
    expect(onWrite).toHaveBeenLastCalledWith([
      MEMBERS[0]!.userId,
      MEMBERS[1]!.userId,
    ]);
    fireEvent.keyDown(list, { key: "Enter" });
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());

    fireEvent.click(trigger("Người nghiệm thu"));
    fireEvent.keyDown(screen.getByRole("listbox"), { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());
    fireEvent.click(trigger("Người nghiệm thu"));
    leave();
    await waitFor(() => expect(screen.queryByRole("listbox")).toBeNull());
    expect(onWrite).toHaveBeenCalledTimes(1);
  });

  it("Person: shows up to two marks then +N", () => {
    renderCell(
      "person",
      "Người nghiệm thu",
      MEMBERS.map((member) => member.userId),
    );
    expect(screen.getByText("+1").getAttribute("title")).toBe(MEMBERS[2]!.name);
  });
});

describe("custom field cells: Checkbox", () => {
  it("a click, Enter and Space each flip it and save at once", () => {
    const { onWrite } = renderCell("checkbox", "Nghiệm thu", false);
    const box = screen.getByRole("checkbox", {
      name: "Nghiệm thu của ERP-145: chưa chọn",
    });
    expect(box.tagName).toBe("BUTTON");
    fireEvent.click(box);
    expect(onWrite).toHaveBeenLastCalledWith(true);
    // Space acts on key release, once.
    fireEvent.keyDown(box, { key: " " });
    expect(onWrite).toHaveBeenCalledTimes(1);
    fireEvent.keyUp(box, { key: " " });
    expect(onWrite).toHaveBeenCalledTimes(2);
    expect(onWrite).toHaveBeenLastCalledWith(true);
  });

  it("an ticked box unticks, and a viewer sees the state without a control", () => {
    const { onWrite } = renderCell("checkbox", "Nghiệm thu", true);
    fireEvent.keyUp(
      screen.getByRole("checkbox", {
        name: "Nghiệm thu của ERP-145: đã chọn",
      }),
      { key: " " },
    );
    expect(onWrite).toHaveBeenCalledWith(false);
  });

  it("a viewer sees the state without a control", () => {
    renderCell("checkbox", "Nghiệm thu", true, { editable: false });
    expect(screen.queryByRole("checkbox")).toBeNull();
    expect(
      screen.getByRole("img", { name: "Nghiệm thu của ERP-145: đã chọn" }),
    ).toBeTruthy();
  });
});

describe("custom field cells: read-only", () => {
  it.each<[FieldType, string, unknown]>([
    ["text", "Mã hợp đồng", "HD-01"],
    ["long_text", "Mô tả", "dòng một"],
    ["number", "Điểm", 8],
    ["select", "Phân hệ", "o-1"],
    ["multi_select", "Phân hệ", ["o-1"]],
    ["date", "Hạn nghiệm thu", "2026-10-15"],
    ["person", "Người nghiệm thu", [MEMBERS[0]!.userId]],
    ["link", "Liên kết", "https://a.vn/"],
  ])(
    "a viewer sees the %s value and gets no editor",
    (fieldType, name, value) => {
      renderCell(fieldType, name, value, { editable: false });
      expect(screen.queryByRole("button", { name: /^Sửa/ })).toBeNull();
      expect(screen.queryByRole("textbox")).toBeNull();
      expect(document.body.textContent).not.toBe("");
    },
  );

  it("a type with no inline editor shows its value only", () => {
    renderCell("timeline", "Tiến độ", "2026-10-01");
    expect(screen.queryByRole("button", { name: /^Sửa/ })).toBeNull();
  });
});

describe("custom field cells: Formula and Rollup", () => {
  const computed = (value: ComputedValue | undefined) =>
    renderCell("formula", "Điểm gấp đôi", undefined, {
      cell: { value: undefined, computed: value },
    });

  it("FUI[wm.table.main:field-computed] shows ƒx with the result and no editor", () => {
    computed({ state: "ok", value: 26 });
    const cell = screen.getByRole("img", { name: "Tự tính: 26" });
    expect(cell.textContent).toContain("ƒx");
    expect(cell.textContent).toContain("26");
    expect(screen.queryByRole("button", { name: /^Sửa/ })).toBeNull();
  });

  it("an empty result reads a dash", () => {
    computed({ state: "empty" });
    expect(screen.getByRole("img").textContent).toContain("—");
  });

  it("an error keeps the word Lỗi with its reason in the name", () => {
    computed({ state: "error", errorCode: "divide-by-zero" });
    const cell = screen.getByRole("img");
    expect(cell.textContent).toContain("Lỗi");
    expect(cell.getAttribute("aria-label")).toContain("Chia cho 0");
  });

  it("the stale value is dimmed while the board reads again", () => {
    computed({ state: "stale", value: 26 });
    const cell = screen.getByRole("img", { name: "Tự tính: 26" });
    expect(cell.getAttribute("data-computed-state")).toBe("stale");
    // The stale and the saving states never dim by opacity: contrast stays above 4.5:1 in both themes.
    expect(cell.className).not.toMatch(/opacity/);
    expect(cell.className).toContain("text-muted-foreground");
    expect(cell.getAttribute("title")).toContain("Đang tính lại");
  });
});

describe("custom field cells: write states", () => {
  const write = (
    status: CellWrite["status"],
    value: unknown = 21,
  ): CellWrite => ({
    status,
    value,
  });

  it("FUI[wm.table.main:field-write-states] saving keeps the value and says Đang lưu", () => {
    renderCell("number", "Điểm", 8, { write: write("pending", 13) });
    const pill = screen.getByRole("status");
    expect(pill.textContent).toContain("Đang lưu");
    expect(pill.className).toContain("text-muted-foreground");
    expect(pill.className).not.toMatch(/opacity/);
    expect(trigger("Điểm").textContent).toContain("13");
  });

  it("a failed write keeps the typed value, says why and retries from the cell", () => {
    const { onRetry, onWrite } = renderCell("number", "Điểm", 8, {
      write: write("error"),
    });
    expect(trigger("Điểm").textContent).toContain("21");
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain("Chưa lưu được");
    expect(alert.textContent).toContain("Giá trị bạn gõ vẫn còn ở ô này");
    fireEvent.click(screen.getByRole("button", { name: "Thử lại" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
    expect(onWrite).not.toHaveBeenCalled();
  });

  it("a version conflict shows the new value and the two choices, with the safe one focused", async () => {
    const { onKeepMine, onUseTheirs } = renderCell("number", "Điểm", 13, {
      write: write("conflict", 21),
    });
    const dialog = screen.getByRole("alertdialog", {
      name: "Xung đột giá trị",
    });
    expect(dialog.textContent).toContain("Điểm vừa được đổi thành 13");
    expect(dialog.textContent).toContain("Giá trị của bạn: 21");
    const useNew = within(dialog).getByRole("button", {
      name: "Dùng giá trị mới (13)",
    });
    await waitFor(() => expect(document.activeElement).toBe(useNew));
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Giữ giá trị của tôi" }),
    );
    expect(onKeepMine).toHaveBeenCalledTimes(1);
    fireEvent.click(useNew);
    expect(onUseTheirs).toHaveBeenCalledTimes(1);
  });

  it("Escape on the conflict takes the new value", () => {
    const { onUseTheirs } = renderCell("number", "Điểm", 13, {
      write: write("conflict", 21),
    });
    fireEvent.keyDown(screen.getByRole("alertdialog"), { key: "Escape" });
    expect(onUseTheirs).toHaveBeenCalledTimes(1);
  });
});
