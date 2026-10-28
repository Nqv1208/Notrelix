import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  statusOptionFormCopyFixture,
  statusOptionFormCreateScenario,
  statusOptionFormDuplicateScenario,
  statusOptionFormEdgeDataScenario,
  statusOptionFormEditScenario,
  statusOptionFormPendingScenario,
  type StatusOptionFormScenario,
} from "../../../verification/status-option-form-fixtures";
import { AddGroupPopover } from "../add-group-popover";
import { RemoveOptionDialog } from "../remove-option-dialog";
import { StatusOptionForm } from "../status-option-form";

const LOCALES = ["vi", "en"] as const;

function renderForm(scenario: StatusOptionFormScenario) {
  const onSubmit = vi.fn();
  const onCancel = vi.fn();
  const view = renderPureUi(
    <StatusOptionForm {...scenario} onSubmit={onSubmit} onCancel={onCancel} />,
  );
  return { onSubmit, onCancel, view };
}

function nameInput(_locale: "vi" | "en") {
  return screen.getByRole("textbox");
}

describe("StatusOptionForm", () => {
  it("FUI[wm.status-option.form:create] sends a trimmed name with the chosen colour and group on Enter", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormCreateScenario(locale),
      );
      expect(document.activeElement).toBe(nameInput(locale));
      fireEvent.change(nameInput(locale), {
        target: { value: "  Chờ khách duyệt  " },
      });
      fireEvent.click(screen.getByRole("radio", { name: copy.color_amber }));
      fireEvent.click(screen.getByRole("radio", { name: copy.category_done }));
      fireEvent.submit(nameInput(locale).closest("form")!);
      expect(onSubmit).toHaveBeenCalledTimes(1);
      expect(onSubmit).toHaveBeenCalledWith({
        name: "Chờ khách duyệt",
        color: "#C18A12",
        category: "done",
      });
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:default-color] starts in Đang làm with its blue and follows the group until a colour is picked", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormCreateScenario(locale),
      );
      expect(
        screen
          .getByRole("radio", { name: copy.category_in_progress })
          .getAttribute("aria-checked"),
      ).toBe("true");
      expect(
        screen
          .getByRole("radio", { name: copy.color_blue })
          .getAttribute("aria-checked"),
      ).toBe("true");
      fireEvent.click(screen.getByRole("radio", { name: copy.category_todo }));
      expect(
        screen
          .getByRole("radio", { name: copy.color_slate })
          .getAttribute("aria-checked"),
      ).toBe("true");
      fireEvent.click(screen.getByRole("radio", { name: copy.color_red }));
      fireEvent.click(screen.getByRole("radio", { name: copy.category_done }));
      fireEvent.change(nameInput(locale), { target: { value: "Huỷ" } });
      fireEvent.submit(nameInput(locale).closest("form")!);
      expect(onSubmit).toHaveBeenCalledWith({
        name: "Huỷ",
        color: "#C8143F",
        category: "done",
      });
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:validation] refuses an empty name and a duplicate (any case) and sends nothing", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormCreateScenario(locale),
      );
      const form = nameInput(locale).closest("form")!;
      fireEvent.submit(form);
      expect(screen.getByRole("alert").textContent).toBe(copy.errorEmpty);
      expect(nameInput(locale).getAttribute("aria-invalid")).toBe("true");
      expect(document.activeElement).toBe(nameInput(locale));
      fireEvent.change(nameInput(locale), { target: { value: " chờ DUYỆT " } });
      expect(screen.getByRole("alert").textContent).toBe(
        copy.errorDuplicate.replace("{{name}}", "chờ DUYỆT"),
      );
      fireEvent.submit(form);
      fireEvent.change(nameInput(locale), {
        target: { value: "x".repeat(201) },
      });
      expect(screen.getByRole("alert").textContent).toBe(copy.errorTooLong);
      fireEvent.submit(form);
      expect(onSubmit).not.toHaveBeenCalled();
      fireEvent.change(nameInput(locale), { target: { value: "Đã duyệt" } });
      expect(screen.queryByRole("alert")).toBeNull();
      fireEvent.submit(form);
      expect(onSubmit).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:duplicate-state] shows the duplicate message for a name that is taken", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormDuplicateScenario(locale),
      );
      expect(screen.getByRole("alert").textContent).toBe(
        copy.errorDuplicate.replace("{{name}}", "Chờ duyệt"),
      );
      fireEvent.submit(nameInput(locale).closest("form")!);
      expect(onSubmit).not.toHaveBeenCalled();
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:cancel] cancels on Escape and on the cancel button without sending", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, onCancel, view } = renderForm(
        statusOptionFormCreateScenario(locale),
      );
      fireEvent.change(nameInput(locale), { target: { value: "Tạm dừng" } });
      fireEvent.keyDown(nameInput(locale), { key: "Escape" });
      expect(onCancel).toHaveBeenCalledTimes(1);
      fireEvent.click(screen.getByRole("button", { name: copy.cancel }));
      expect(onCancel).toHaveBeenCalledTimes(2);
      expect(onSubmit).not.toHaveBeenCalled();
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:edit] fills the status in and allows keeping its own name", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormEditScenario(locale),
      );
      expect((nameInput(locale) as HTMLInputElement).value).toBe("Đang làm");
      expect(screen.queryByRole("alert")).toBeNull();
      expect(
        screen.getByRole("heading", { name: copy.titleEdit }),
      ).toBeTruthy();
      fireEvent.click(screen.getByRole("radio", { name: copy.color_green }));
      fireEvent.click(screen.getByRole("button", { name: copy.submitEdit }));
      expect(onSubmit).toHaveBeenCalledWith({
        name: "Đang làm",
        color: "#2E8B57",
        category: "in_progress",
      });
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:pending] keeps the values and cannot be sent again while a write is in flight", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormPendingScenario(locale),
      );
      const button = screen.getByRole("button", { name: copy.saving });
      expect((button as HTMLButtonElement).disabled).toBe(true);
      expect((nameInput(locale) as HTMLInputElement).value).toBe(
        "Chờ khách duyệt",
      );
      fireEvent.submit(nameInput(locale).closest("form")!);
      expect(onSubmit).not.toHaveBeenCalled();
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:no-category] names a value of another Select field with name and colour only", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormEdgeDataScenario(locale),
      );
      expect(
        screen.queryByRole("radio", { name: copy.category_todo }),
      ).toBeNull();
      expect(
        screen.getByRole("heading", { name: copy.valueTitleCreate }),
      ).toBeTruthy();
      expect(
        screen.getByRole("button", { name: copy.valueSubmitCreate }),
      ).toBeTruthy();
      fireEvent.submit(nameInput(locale).closest("form")!);
      expect(onSubmit).toHaveBeenCalledWith({
        name: (nameInput(locale) as HTMLInputElement).value,
        color: "#1E7BE0",
      });
      view.unmount();
    }
  });
});

const CHOICES = [
  { id: "o-todo", name: "Cần làm", color: "#7A8391" },
  { id: "o-doing", name: "Đang làm", color: "#1E7BE0" },
];

describe("RemoveOptionDialog", () => {
  it("FUI[wm.status-option.form:remove-with-items] makes the person choose where a status's items go, the first other status preselected", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const onConfirm = vi.fn();
      const view = renderPureUi(
        <RemoveOptionDialog
          copy={copy}
          subject="status"
          name="Chờ duyệt"
          itemCount={2}
          choices={CHOICES}
          onCancel={vi.fn()}
          onConfirm={onConfirm}
        />,
      );
      const dialog = screen.getByRole("alertdialog");
      expect(
        within(dialog).getByText(
          copy.removeTitle.replace("{{name}}", "Chờ duyệt"),
        ),
      ).toBeTruthy();
      expect(
        within(dialog)
          .getByRole("radio", { name: "Cần làm" })
          .getAttribute("aria-checked"),
      ).toBe("true");
      // A status has no "no value" way out.
      expect(
        within(dialog).queryByRole("radio", { name: copy.removeNoValue }),
      ).toBeNull();
      fireEvent.click(within(dialog).getByRole("radio", { name: "Đang làm" }));
      fireEvent.click(
        within(dialog).getByRole("button", { name: copy.removeMoveConfirm }),
      );
      expect(onConfirm).toHaveBeenCalledWith("o-doing");
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:remove-empty] confirms a status nobody uses without asking for a replacement", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const onConfirm = vi.fn();
      const onCancel = vi.fn();
      const view = renderPureUi(
        <RemoveOptionDialog
          copy={copy}
          subject="status"
          name="Chờ duyệt"
          itemCount={0}
          choices={CHOICES}
          onCancel={onCancel}
          onConfirm={onConfirm}
        />,
      );
      const dialog = screen.getByRole("alertdialog");
      expect(within(dialog).getByText(copy.removeEmptyBody)).toBeTruthy();
      expect(within(dialog).queryByRole("radio")).toBeNull();
      fireEvent.click(
        within(dialog).getByRole("button", { name: copy.cancel }),
      );
      expect(onCancel).toHaveBeenCalledTimes(1);
      expect(onConfirm).not.toHaveBeenCalled();
      fireEvent.click(
        within(dialog).getByRole("button", { name: copy.removeConfirm }),
      );
      expect(onConfirm).toHaveBeenCalledWith(null);
      view.unmount();
    }
  });

  it("FUI[wm.status-option.form:remove-value] lets the items of a deleted select value lose the value, and keeps the dialog while it runs", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const onConfirm = vi.fn();
      const view = renderPureUi(
        <RemoveOptionDialog
          copy={copy}
          subject="value"
          name="Khẩn"
          itemCount={1}
          choices={CHOICES}
          onCancel={vi.fn()}
          onConfirm={onConfirm}
        />,
      );
      const dialog = screen.getByRole("alertdialog");
      expect(
        within(dialog)
          .getByRole("radio", { name: copy.removeNoValue })
          .getAttribute("aria-checked"),
      ).toBe("true");
      fireEvent.click(
        within(dialog).getByRole("button", { name: copy.removeMoveConfirm }),
      );
      expect(onConfirm).toHaveBeenCalledWith(null);
      view.rerender(
        <RemoveOptionDialog
          copy={copy}
          subject="value"
          name="Khẩn"
          itemCount={1}
          choices={CHOICES}
          pending
          onCancel={vi.fn()}
          onConfirm={onConfirm}
        />,
      );
      expect(
        (
          within(screen.getByRole("alertdialog")).getByRole("button", {
            name: copy.removing,
          }) as HTMLButtonElement
        ).disabled,
      ).toBe(true);
      view.unmount();
    }
  });
});

describe("AddGroupPopover", () => {
  beforeAll(() => {
    Element.prototype.scrollIntoView = () => undefined;
    Element.prototype.hasPointerCapture = () => false;
    Element.prototype.releasePointerCapture = () => undefined;
    Element.prototype.setPointerCapture = () => undefined;
  });

  it("FUI[wm.status-option.form:add-group] names a board group with a colour, refuses a taken title and keeps the form when the add fails", async () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const onSubmit = vi
        .fn()
        .mockRejectedValueOnce(new Error("refused"))
        .mockResolvedValue(undefined);
      const view = renderPureUi(
        <AddGroupPopover
          copy={copy}
          label={copy.groupSubmit}
          existingTitles={["Sprint 1"]}
          onSubmit={onSubmit}
        />,
      );
      fireEvent.click(screen.getByRole("button", { name: copy.groupSubmit }));
      const name = screen.getByRole("textbox", { name: copy.groupNameLabel });
      expect(
        screen.queryByRole("radio", { name: copy.category_done }),
      ).toBeNull();
      fireEvent.change(name, { target: { value: "sprint 1" } });
      fireEvent.submit(name.closest("form")!);
      expect(onSubmit).not.toHaveBeenCalled();
      expect(screen.getByRole("alert")).toBeTruthy();

      fireEvent.change(name, { target: { value: "  Sprint 2  " } });
      fireEvent.click(screen.getByRole("radio", { name: copy.color_green }));
      fireEvent.submit(name.closest("form")!);
      await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
      expect(onSubmit).toHaveBeenCalledWith({
        title: "Sprint 2",
        color: "#2E8B57",
      });
      // The first add failed: the form stays with its values for another try.
      await waitFor(() =>
        expect(
          (
            screen.getByRole("textbox", {
              name: copy.groupNameLabel,
            }) as HTMLInputElement
          ).value,
        ).toBe("  Sprint 2  "),
      );
      fireEvent.submit(
        screen
          .getByRole("textbox", { name: copy.groupNameLabel })
          .closest("form")!,
      );
      await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(2));
      await waitFor(() => expect(screen.queryByRole("textbox")).toBeNull());
      view.unmount();
    }
  });
});
