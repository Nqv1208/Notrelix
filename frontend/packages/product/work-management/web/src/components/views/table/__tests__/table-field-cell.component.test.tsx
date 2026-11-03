import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  renderHook,
  renderPureUi,
  screen,
} from "@notrelix/testing";
import type { FieldDefinition } from "@notrelix/work-management-core";
import { TABLE_CUSTOM_FIELDS } from "../../../../verification/table-field-fixtures";
import { mainTableCopy } from "../main-table.copy";
import { TableFieldCell, type CellWrite } from "../table-field-cell";
import { useFieldCellWrites } from "../use-field-cell-writes";

const copy = mainTableCopy("en");
const field = (id: string) =>
  TABLE_CUSTOM_FIELDS.find((entry) => entry.id === id) as FieldDefinition;

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

function cell(
  id: string,
  value: unknown,
  extra: {
    editable?: boolean;
    write?: CellWrite;
    onWrite?: (value: unknown, stale?: boolean) => void;
    onKeepMine?: () => void;
    onUseTheirs?: () => void;
    onRetry?: () => void;
  } = {},
) {
  return renderPureUi(
    <TableFieldCell
      field={field(id)}
      cell={{ value }}
      rowKey="ERP-142"
      copy={copy}
      members={[]}
      editable={extra.editable ?? true}
      write={extra.write}
      onWrite={extra.onWrite ?? vi.fn()}
      onRetry={extra.onRetry ?? vi.fn()}
      onKeepMine={extra.onKeepMine ?? vi.fn()}
      onUseTheirs={extra.onUseTheirs ?? vi.fn()}
      onReloadSchema={vi.fn()}
    />,
  );
}

describe("a custom field cell of the table", () => {
  it("saves a typed number once on Enter and sends nothing when the value is unchanged", () => {
    const onWrite = vi.fn();
    cell("field-points", 13, { onWrite });
    fireEvent.click(
      screen.getByRole("button", { name: /Edit Điểm of ERP-142/ }),
    );
    const input = screen.getByRole("textbox", { name: /Edit Điểm of ERP-142/ });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onWrite).not.toHaveBeenCalled();

    fireEvent.click(
      screen.getByRole("button", { name: /Edit Điểm of ERP-142/ }),
    );
    const next = screen.getByRole("textbox", { name: /Edit Điểm of ERP-142/ });
    fireEvent.change(next, { target: { value: "14" } });
    fireEvent.keyDown(next, { key: "Enter" });
    expect(onWrite).toHaveBeenCalledTimes(1);
    expect(onWrite).toHaveBeenCalledWith(14, false);
  });

  it("keeps a number that is not a number open with its reason and sends nothing", () => {
    const onWrite = vi.fn();
    cell("field-points", 13, { onWrite });
    fireEvent.click(
      screen.getByRole("button", { name: /Edit Điểm of ERP-142/ }),
    );
    const input = screen.getByRole("textbox", { name: /Edit Điểm of ERP-142/ });
    fireEvent.change(input, { target: { value: "13a" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(screen.getByRole("alert").textContent).toContain("Enter a number");
    expect(onWrite).not.toHaveBeenCalled();
  });

  it("refuses a link that is not http or https", () => {
    const onWrite = vi.fn();
    cell("field-link", "https://erp.minhan.vn/t/142", { onWrite });
    fireEvent.click(
      screen.getByRole("button", { name: /Edit Liên kết of ERP-142/ }),
    );
    const input = screen.getByRole("textbox", {
      name: /Edit Liên kết of ERP-142/,
    });
    fireEvent.change(input, { target: { value: "javascript:alert(1)" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(screen.getByRole("alert").textContent).toContain("http");
    expect(onWrite).not.toHaveBeenCalled();
  });

  it("puts the old text back on Escape", () => {
    const onWrite = vi.fn();
    cell("field-link", "https://erp.minhan.vn/t/142", { onWrite });
    fireEvent.click(
      screen.getByRole("button", { name: /Edit Liên kết of ERP-142/ }),
    );
    const input = screen.getByRole("textbox", {
      name: /Edit Liên kết of ERP-142/,
    });
    fireEvent.change(input, { target: { value: "https://other.vn" } });
    fireEvent.keyDown(input, { key: "Escape" });
    expect(onWrite).not.toHaveBeenCalled();
  });

  it("flips a checkbox with one click", () => {
    const onWrite = vi.fn();
    cell("field-accepted", false, { onWrite });
    fireEvent.click(screen.getByRole("checkbox"));
    expect(onWrite).toHaveBeenCalledWith(true);
  });

  it("gives a viewer the value alone, with no editor", () => {
    cell("field-points", 13, { editable: false });
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.getByText("13")).toBeTruthy();
  });

  it("shows Saving while a write is out, and the typed value in place of the old one", () => {
    cell("field-points", 13, { write: { status: "pending", value: 14 } });
    expect(screen.getByRole("status").textContent).toContain("Saving");
    expect(screen.getByText("14")).toBeTruthy();
  });

  it("keeps the typed value after a failed write and offers Retry", () => {
    const onRetry = vi.fn();
    cell("field-points", 13, {
      write: { status: "error", value: 14 },
      onRetry,
    });
    expect(screen.getByText("14")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Retry" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("names the new value on a conflict and offers both choices", () => {
    const onKeepMine = vi.fn();
    const onUseTheirs = vi.fn();
    cell("field-points", 21, {
      write: { status: "conflict", value: 13 },
      onKeepMine,
      onUseTheirs,
    });
    const dialog = screen.getByRole("alertdialog", { name: "Value conflict" });
    expect(dialog.textContent).toContain("was just changed to 21");
    expect(dialog.textContent).toContain("Your value: 13");
    fireEvent.click(screen.getByRole("button", { name: "Keep my value" }));
    expect(onKeepMine).toHaveBeenCalledTimes(1);
    fireEvent.click(
      screen.getByRole("button", { name: "Use the new value (21)" }),
    );
    expect(onUseTheirs).toHaveBeenCalledTimes(1);
  });

  it("shows a formula as read only, with its error reason, never a number", () => {
    renderPureUi(
      <TableFieldCell
        field={field("field-double")}
        cell={{
          value: undefined,
          computed: { state: "error", errorCode: "divide-by-zero" },
        }}
        rowKey="ERP-142"
        copy={copy}
        members={[]}
        editable
        onWrite={vi.fn()}
        onRetry={vi.fn()}
        onKeepMine={vi.fn()}
        onUseTheirs={vi.fn()}
        onReloadSchema={vi.fn()}
      />,
    );
    expect(screen.queryByRole("button")).toBeNull();
    const shown = screen.getByRole("img");
    expect(shown.getAttribute("data-computed-state")).toBe("error");
    expect(shown.getAttribute("title")).toContain("Divided by zero");
  });
});

describe("the write state of the custom cells", () => {
  const send = vi.fn();
  const hook = (reload = vi.fn(async () => undefined)) =>
    renderHook(() => useFieldCellWrites(send, reload));

  it("is pending until the server answers, then gone", async () => {
    send.mockResolvedValueOnce(undefined);
    const { result } = hook();
    await act(async () => {
      result.current.write("row", "field", 14);
    });
    expect(result.current.writeOf("row", "field")).toBeUndefined();
    expect(send).toHaveBeenCalledWith("row", "field", 14);
  });

  it("keeps the typed value with an error when the write fails, and Retry sends it again", async () => {
    send.mockReset();
    send.mockRejectedValueOnce(new Error("offline"));
    send.mockResolvedValueOnce(undefined);
    const { result } = hook();
    await act(async () => {
      result.current.write("row", "field", 14);
    });
    expect(result.current.writeOf("row", "field")).toEqual({
      status: "error",
      value: 14,
    });
    await act(async () => {
      result.current.retry("row", "field");
    });
    expect(send).toHaveBeenCalledTimes(2);
    expect(result.current.writeOf("row", "field")).toBeUndefined();
  });

  it("turns a version conflict into the choice; Keep mine reads the board again and then sends my value; Use theirs sends nothing", async () => {
    send.mockReset();
    send.mockRejectedValueOnce({ kind: "conflict" });
    send.mockResolvedValueOnce(undefined);
    const reload = vi.fn(async () => undefined);
    const { result } = hook(reload);
    await act(async () => {
      result.current.write("row", "field", 14);
    });
    expect(result.current.writeOf("row", "field")?.status).toBe("conflict");

    await act(async () => {
      result.current.keepMine("row", "field");
    });
    expect(reload).toHaveBeenCalledTimes(1);
    expect(send).toHaveBeenLastCalledWith("row", "field", 14);

    send.mockReset();
    send.mockRejectedValueOnce({ kind: "conflict" });
    await act(async () => {
      result.current.write("row", "field", 15);
    });
    await act(async () => {
      result.current.takeTheirs("row", "field");
    });
    expect(result.current.writeOf("row", "field")).toBeUndefined();
    expect(send).toHaveBeenCalledTimes(1);
  });

  it("shows the conflict at once and sends nothing when the cell changed while it was open", async () => {
    send.mockReset();
    const { result } = hook();
    await act(async () => {
      result.current.write("row", "field", 14, true);
    });
    expect(result.current.writeOf("row", "field")?.status).toBe("conflict");
    expect(send).not.toHaveBeenCalled();
  });

  it("sends one person id for a single People field and a list for a multiple one", () => {
    const members = [
      { userId: "u-1", name: "An", initials: "A" },
      { userId: "u-2", name: "Bình", initials: "B" },
    ];
    const pick = (multiple: boolean) => {
      const onWrite = vi.fn();
      const { unmount } = renderPureUi(
        <TableFieldCell
          field={{ ...field("field-reviewer"), multiple }}
          cell={{ value: multiple ? ["u-1"] : "u-1" }}
          rowKey="ERP-142"
          copy={copy}
          members={members}
          editable
          onWrite={onWrite}
          onRetry={vi.fn()}
          onKeepMine={vi.fn()}
          onUseTheirs={vi.fn()}
          onReloadSchema={vi.fn()}
        />,
      );
      fireEvent.click(screen.getAllByRole("button")[0]!);
      fireEvent.click(screen.getByRole("option", { name: /Bình/ }));
      unmount();
      return onWrite;
    };

    expect(pick(false)).toHaveBeenCalledWith("u-2");
    expect(pick(true)).toHaveBeenCalledWith(["u-1", "u-2"]);
  });
});
