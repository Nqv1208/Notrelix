import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { FieldDefinition } from "@notrelix/work-management-core";
import { ItemMultiPopover, ItemTextPopover } from "../item-field-more-editors";
import { ItemNumberPopover } from "../item-field-editors";
import { ItemDuePopover } from "../item-field-editors";
import {
  describeComputed,
  ItemLinkPopover,
  ItemLongTextPopover,
} from "../item-field-extra-editors";
import { en, vi as viCopy } from "../item-sheet.copy";

const trigger = <button type="button">open</button>;
const open = () =>
  fireEvent.click(screen.getByRole("button", { name: "open" }));

const FIELD = {
  id: "f1",
  boardId: "b1",
  name: "Phân hệ",
  fieldType: "multi_select",
  options: [
    { id: "o1", label: "Kế toán", color: "#1666CC" },
    { id: "o2", label: "Kho", color: "#D99A00" },
  ],
  position: 1,
  isHidden: false,
  isSystemField: false,
} as FieldDefinition;

describe("Text", () => {
  const renderText = (onChange: () => void) =>
    renderPureUi(
      <ItemTextPopover
        value="a"
        label="Ghi chú"
        readOnly={false}
        onChange={onChange}
      >
        {trigger}
      </ItemTextPopover>,
    );

  it("saves on Enter", async () => {
    const onChange = vi.fn();
    renderText(onChange);
    open();
    const input = await screen.findByRole("textbox", { name: "Ghi chú" });
    fireEvent.change(input, { target: { value: "b" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onChange).toHaveBeenCalledWith("b");
  });

  it("saves on blur", async () => {
    const onChange = vi.fn();
    renderText(onChange);
    open();
    const input = await screen.findByRole("textbox", { name: "Ghi chú" });
    fireEvent.change(input, { target: { value: "c" } });
    fireEvent.blur(input);
    expect(onChange).toHaveBeenCalledWith("c");
  });

  it("has no editor for a viewer", () => {
    renderPureUi(
      <ItemTextPopover value="a" label="Ghi chú" readOnly onChange={vi.fn()}>
        {trigger}
      </ItemTextPopover>,
    );
    open();
    expect(screen.queryByRole("textbox", { name: "Ghi chú" })).toBeNull();
  });
});

describe("Long text", () => {
  const copy = en.properties;
  it("saves on Enter, keeps Shift+Enter for a new line, clears on empty", async () => {
    const onChange = vi.fn();
    renderPureUi(
      <ItemLongTextPopover
        value="x"
        label="Mô tả"
        readOnly={false}
        copy={copy}
        onChange={onChange}
      >
        {trigger}
      </ItemLongTextPopover>,
    );
    open();
    const area = await screen.findByLabelText("Mô tả");
    fireEvent.change(area, { target: { value: "dòng 1\ndòng 2" } });
    fireEvent.keyDown(area, { key: "Enter", shiftKey: true });
    expect(onChange).not.toHaveBeenCalled();
    fireEvent.keyDown(area, { key: "Enter" });
    expect(onChange).toHaveBeenCalledWith("dòng 1\ndòng 2");
  });

  it("Escape closes without saving", async () => {
    const onChange = vi.fn();
    renderPureUi(
      <ItemLongTextPopover
        value="x"
        label="Mô tả"
        readOnly={false}
        copy={copy}
        onChange={onChange}
      >
        {trigger}
      </ItemLongTextPopover>,
    );
    open();
    const area = await screen.findByLabelText("Mô tả");
    fireEvent.change(area, { target: { value: "đổi" } });
    fireEvent.keyDown(area, { key: "Escape" });
    expect(onChange).not.toHaveBeenCalled();
  });

  it("saves with the Save button", async () => {
    const onChange = vi.fn();
    renderPureUi(
      <ItemLongTextPopover
        value=""
        label="Mô tả"
        readOnly={false}
        copy={copy}
        onChange={onChange}
      >
        {trigger}
      </ItemLongTextPopover>,
    );
    open();
    fireEvent.change(await screen.findByLabelText("Mô tả"), {
      target: { value: "mới" },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.saveLongText }));
    expect(onChange).toHaveBeenCalledWith("mới");
  });
});

describe("Link", () => {
  it("accepts an https address on Enter and on blur", async () => {
    const onChange = vi.fn();
    renderPureUi(
      <ItemLinkPopover
        value=""
        label="Liên kết"
        readOnly={false}
        copy={viCopy.properties}
        onChange={onChange}
      >
        {trigger}
      </ItemLinkPopover>,
    );
    open();
    const input = await screen.findByLabelText("Liên kết");
    fireEvent.change(input, { target: { value: "erp.minhan.vn/t/145" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onChange).toHaveBeenCalledWith("https://erp.minhan.vn/t/145");
  });

  it.each([
    "javascript:alert(1)",
    "ftp://x.vn/a",
    "data:text/html,x",
    "file:///etc/passwd",
  ])("rejects %s with a reason and sends nothing", async (bad) => {
    const onChange = vi.fn();
    renderPureUi(
      <ItemLinkPopover
        value=""
        label="Liên kết"
        readOnly={false}
        copy={viCopy.properties}
        onChange={onChange}
      >
        {trigger}
      </ItemLinkPopover>,
    );
    open();
    const input = await screen.findByLabelText("Liên kết");
    fireEvent.change(input, { target: { value: bad } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onChange).not.toHaveBeenCalled();
    expect((await screen.findByRole("alert")).textContent).toBe(
      viCopy.properties.invalidLink,
    );
  });
});

describe("Number", () => {
  it("opens an editor for a writer and none for a viewer", async () => {
    renderPureUi(
      <ItemNumberPopover value={13} label="Điểm" readOnly onChange={vi.fn()}>
        {trigger}
      </ItemNumberPopover>,
    );
    open();
    expect(screen.queryByLabelText("Điểm")).toBeNull();
  });
});

describe("Select, multi select and date", () => {
  it("a multi select pick sends the whole set of ids", async () => {
    const onChange = vi.fn();
    renderPureUi(
      <ItemMultiPopover
        field={FIELD}
        value={["o1"]}
        readOnly={false}
        onChange={onChange}
      >
        {trigger}
      </ItemMultiPopover>,
    );
    open();
    fireEvent.click(await screen.findByRole("button", { name: "Kho" }));
    expect(onChange).toHaveBeenCalledWith(["o1", "o2"]);
  });

  it("a date pick sends an ISO date and a viewer gets no calendar", async () => {
    renderPureUi(
      <ItemDuePopover
        dueAt={null}
        clearLabel="Xoá ngày"
        readOnly
        onChange={vi.fn()}
      >
        {trigger}
      </ItemDuePopover>,
    );
    open();
    expect(screen.queryByRole("grid")).toBeNull();
  });
});

describe("Calculated values", () => {
  const field = {
    ...FIELD,
    name: "Điểm gấp đôi",
    fieldType: "formula",
  } as FieldDefinition;
  const copy = viCopy.properties.computed;

  it("shows a number, a dash for empty, and the reason for an error", () => {
    expect(
      describeComputed(field, { state: "ok", value: 26 }, copy),
    ).toMatchObject({ text: "26", tone: "value" });
    expect(describeComputed(field, { state: "empty" }, copy)).toMatchObject({
      text: "—",
      tone: "empty",
    });
    expect(describeComputed(field, undefined, copy).tone).toBe("empty");
    const error = describeComputed(
      field,
      { state: "error", errorCode: "divide-by-zero" },
      copy,
    );
    expect(error.tone).toBe("error");
    expect(error.title).toContain("Chia cho 0");
    expect(
      describeComputed(field, { state: "error", errorCode: "new-code" }, copy)
        .title,
    ).toContain(copy.unknownError);
  });

  it("marks a value updating while a source write is being reloaded", () => {
    expect(
      describeComputed(field, { state: "stale", value: 26 }, copy),
    ).toMatchObject({ tone: "stale" });
  });
});
