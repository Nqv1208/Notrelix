import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import {
  ItemCreateDialog,
  type ItemCreateDialogProps,
} from "../item-create-dialog";
import { en, vi as viCopy, type ItemCreateCopy } from "../item-create.copy";

/** The interaction-case marker, once per case: on the Vietnamese run only. */
const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[${id}] ` : "";

function props(
  copy: ItemCreateCopy,
  overrides: Partial<ItemCreateDialogProps> = {},
): ItemCreateDialogProps {
  return {
    copy,
    open: true,
    groups: [
      { id: "g-1", label: "Plan" },
      { id: "g-2", label: "Build" },
    ],
    defaultGroupId: "g-1",
    statuses: [
      { id: "s-todo", label: "To do" },
      { id: "s-doing", label: "Doing" },
    ],
    defaultStatusId: "s-todo",
    priorities: [{ id: "p-high", label: "High" }],
    members: [
      { userId: "u-1", name: "Ha Nguyen", initials: "HN" },
      { userId: "u-2", name: "Quan Tran", initials: "QT" },
    ],
    labels: [{ id: "l-1", name: "Data", color: "#3b82f6" }],
    pending: false,
    error: null,
    onOpenChange: vi.fn(),
    onSubmit: vi.fn(),
    ...overrides,
  };
}

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("item create dialog (%s)", (locale, copy) => {
  it(`${fui("wm.item-create.dialog:required", locale)}asks for a name and creates nothing without one (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    fireEvent.click(screen.getByRole("button", { name: copy.submit }));
    expect(screen.getByText(copy.nameRequired)).toBeTruthy();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it(`${fui("wm.item-create.dialog:submit", locale)}hands back the name and every fact that was set (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    fireEvent.change(screen.getByRole("textbox", { name: copy.name }), {
      target: { value: "  Move the customers  " },
    });
    fireEvent.change(screen.getByRole("textbox", { name: copy.description }), {
      target: { value: "- first\n- second" },
    });
    fireEvent.click(screen.getByRole("button", { name: /Ha Nguyen/ }));
    fireEvent.click(screen.getByRole("button", { name: "Data" }));
    fireEvent.change(screen.getByLabelText(copy.startDate), {
      target: { value: "2026-11-02" },
    });
    fireEvent.change(screen.getByLabelText(copy.dueDate), {
      target: { value: "2026-11-09" },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.submit }));
    expect(onSubmit).toHaveBeenCalledWith({
      title: "Move the customers",
      descriptionMd: "- first\n- second",
      listId: "g-1",
      statusOptionId: "s-todo",
      priorityOptionId: null,
      assigneeIds: ["u-1"],
      labelIds: ["l-1"],
      startDay: "2026-11-02",
      dueDay: "2026-11-09",
      another: false,
    });
  });

  it(`${fui("wm.item-create.dialog:dates", locale)}refuses a due date before the start date (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    fireEvent.change(screen.getByRole("textbox", { name: copy.name }), {
      target: { value: "Backwards" },
    });
    fireEvent.change(screen.getByLabelText(copy.startDate), {
      target: { value: "2026-11-09" },
    });
    fireEvent.change(screen.getByLabelText(copy.dueDate), {
      target: { value: "2026-11-02" },
    });
    expect(screen.getByText(copy.dueBeforeStart)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.submit }));
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it(`${fui("wm.item-create.dialog:error", locale)}keeps what was typed and shows the failure (${locale})`, () => {
    renderPureUi(<ItemCreateDialog {...props(copy, { error: copy.failed })} />);
    expect(screen.getByRole("alert").textContent).toBe(copy.failed);
  });

  it(`${fui("wm.item-create.dialog:pending", locale)}disables the button while it saves (${locale})`, () => {
    renderPureUi(<ItemCreateDialog {...props(copy, { pending: true })} />);
    expect(
      (
        screen.getByRole("button", {
          name: copy.submitting,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});
