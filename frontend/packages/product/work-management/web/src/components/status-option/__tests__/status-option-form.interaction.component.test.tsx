import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import {
  statusOptionFormCopyFixture,
  statusOptionFormCreateScenario,
  statusOptionFormDuplicateScenario,
  statusOptionFormEdgeDataScenario,
  statusOptionFormEditScenario,
  statusOptionFormPendingScenario,
  type StatusOptionFormScenario,
} from "../../../verification/status-option-form-fixtures";
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

function nameInput(locale: "vi" | "en") {
  return screen.getByRole("textbox", {
    name: statusOptionFormCopyFixture(locale).nameLabel,
  });
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

  it("FUI[wm.status-option.form:no-category] omits the group for a field without groups", () => {
    for (const locale of LOCALES) {
      const copy = statusOptionFormCopyFixture(locale);
      const { onSubmit, view } = renderForm(
        statusOptionFormEdgeDataScenario(locale),
      );
      expect(
        screen.queryByRole("radio", { name: copy.category_todo }),
      ).toBeNull();
      fireEvent.submit(nameInput(locale).closest("form")!);
      expect(onSubmit).toHaveBeenCalledWith({
        name: (nameInput(locale) as HTMLInputElement).value,
        color: "#1E7BE0",
      });
      view.unmount();
    }
  });
});
