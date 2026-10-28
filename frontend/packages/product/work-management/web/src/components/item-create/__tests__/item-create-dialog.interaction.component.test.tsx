import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
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
    today: "2026-10-12",
    onOpenChange: vi.fn(),
    onSubmit: vi.fn(),
    ...overrides,
  };
}

function typeName(copy: ItemCreateCopy, value: string) {
  fireEvent.change(
    screen.getByRole("textbox", { name: /^Tên công việc|^Item name/ }),
    {
      target: { value },
    },
  );
  void copy;
}

describe.each([
  ["vi", viCopy],
  ["en", en],
] as const)("item create dialog (%s)", (locale, copy) => {
  it(`${fui("wm.item-create.dialog:required", locale)}asks for a name, puts the cursor there and creates nothing without one (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    fireEvent.click(
      screen.getByRole("button", { name: new RegExp(`^${copy.submit}`) }),
    );
    expect(screen.getByText(copy.nameRequired)).toBeTruthy();
    expect(onSubmit).not.toHaveBeenCalled();
    expect(document.activeElement?.id).toBe(
      screen.getByRole("textbox", { name: /^Tên công việc|^Item name/ }).id,
    );
  });

  it(`${fui("wm.item-create.dialog:submit", locale)}hands back the name and every fact that was set (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    typeName(copy, "  Move the customers  ");
    fireEvent.change(
      screen.getByRole("textbox", { name: new RegExp(`^${copy.description}`) }),
      { target: { value: "- first\n- second" } },
    );
    fireEvent.click(screen.getByRole("button", { name: copy.addPerson }));
    fireEvent.click(screen.getByRole("option", { name: /Ha Nguyen/ }));
    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
    });
    fireEvent.click(screen.getByRole("button", { name: "Data" }));
    fireEvent.change(screen.getByLabelText(copy.startDate), {
      target: { value: "02/11/2026" },
    });
    fireEvent.change(screen.getByLabelText(copy.dueDate), {
      target: { value: "09/11/2026" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: new RegExp(`^${copy.submit}`) }),
    );
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

  it(`${fui("wm.item-create.dialog:dates", locale)}refuses a due date before the start date and moves to it (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    typeName(copy, "Backwards");
    fireEvent.change(screen.getByLabelText(copy.startDate), {
      target: { value: "09/11/2026" },
    });
    fireEvent.change(screen.getByLabelText(copy.dueDate), {
      target: { value: "02/11/2026" },
    });
    expect(screen.getByText(copy.dueBeforeStart)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: new RegExp(`^${copy.submit}`) }),
    );
    expect(onSubmit).not.toHaveBeenCalled();
    expect(document.activeElement).toBe(screen.getByLabelText(copy.dueDate));
  });

  it(`${fui("wm.item-create.dialog:date-text", locale)}refuses a date that is not a day (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    typeName(copy, "Odd date");
    fireEvent.change(screen.getByLabelText(copy.dueDate), {
      target: { value: "31/02/2026" },
    });
    expect(screen.getByText(copy.dateInvalid)).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: new RegExp(`^${copy.submit}`) }),
    );
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it(`${fui("wm.item-create.dialog:quick-due", locale)}sets the due date from a quick pick and says how far it is (${locale})`, () => {
    renderPureUi(<ItemCreateDialog {...props(copy)} />);
    fireEvent.click(screen.getByRole("button", { name: copy.tomorrow }));
    expect(
      (screen.getByLabelText(copy.dueDate) as HTMLInputElement).value,
    ).toBe("13/10/2026");
    expect(screen.getByText(copy.dueTomorrowHint)).toBeTruthy();
    // 2026-10-12 is a Monday: the coming Friday is the 16th.
    fireEvent.click(
      screen.getByRole("button", {
        name: `${copy.weekdays.fri} 16/10`,
      }),
    );
    expect(
      (screen.getByLabelText(copy.dueDate) as HTMLInputElement).value,
    ).toBe("16/10/2026");
    expect(
      screen.getByText(
        copy.dueIn
          .replace("{{weekday}}", copy.weekdays.fri)
          .replace("{{days}}", "4"),
      ),
    ).toBeTruthy();
  });

  it(`${fui("wm.item-create.dialog:people", locale)}finds a person by search, shows them as a chip and removes them (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    fireEvent.click(screen.getByRole("button", { name: copy.addPerson }));
    fireEvent.change(screen.getByPlaceholderText(copy.searchPeople), {
      target: { value: "quan" },
    });
    expect(screen.queryByRole("option", { name: /Ha Nguyen/ })).toBeNull();
    fireEvent.click(screen.getByRole("option", { name: /Quan Tran/ }));
    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
    });
    const group = screen.getByRole("group", {
      name: new RegExp(`^${copy.assignees}`),
    });
    expect(within(group).getByText("Quan Tran")).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", {
        name: copy.removePerson.replace("{{name}}", "Quan Tran"),
      }),
    );
    expect(within(group).queryByText("Quan Tran")).toBeNull();
  });

  it(`${fui("wm.item-create.dialog:assign-me", locale)}offers to give the item to the signed-in member (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(
      <ItemCreateDialog {...props(copy, { onSubmit, currentUserId: "u-2" })} />,
    );
    typeName(copy, "Mine");
    fireEvent.click(screen.getByRole("button", { name: copy.assignMe }));
    fireEvent.click(
      screen.getByRole("button", { name: new RegExp(`^${copy.submit}`) }),
    );
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ assigneeIds: ["u-2"] }),
    );
  });

  it(`${fui("wm.item-create.dialog:ctrl-enter", locale)}creates from any field with Ctrl+Enter (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(<ItemCreateDialog {...props(copy, { onSubmit })} />);
    typeName(copy, "From the description");
    fireEvent.keyDown(
      screen.getByRole("textbox", { name: new RegExp(`^${copy.description}`) }),
      { key: "Enter", ctrlKey: true },
    );
    expect(onSubmit).toHaveBeenCalledTimes(1);
  });

  it(`${fui("wm.item-create.dialog:quick-add", locale)}carries a quick-add name and starts in the description (${locale})`, () => {
    renderPureUi(
      <ItemCreateDialog
        {...props(copy, {
          defaultTitle: "Book the training",
          focusDescription: true,
          context: { kind: "quick", name: "To do" },
        })}
      />,
    );
    expect(
      (
        screen.getByRole("textbox", {
          name: /^Tên công việc|^Item name/,
        }) as HTMLInputElement
      ).value,
    ).toBe("Book the training");
    return new Promise<void>((resolve) =>
      window.requestAnimationFrame(() => {
        expect(document.activeElement?.tagName).toBe("TEXTAREA");
        expect(
          screen.getByText(copy.fromQuickAdd, { exact: false }).textContent,
        ).toContain("To do");
        resolve();
      }),
    );
  });

  it(`${fui("wm.item-create.dialog:lane", locale)}names the lane it was opened from and drops the group field when they mirror statuses (${locale})`, () => {
    renderPureUi(
      <ItemCreateDialog
        {...props(copy, {
          hideGroup: true,
          context: { kind: "lane", name: "Doing" },
        })}
      />,
    );
    expect(screen.queryByText(copy.group)).toBeNull();
    expect(
      document.querySelector("[data-item-create-context]")?.textContent,
    ).toContain("Doing");
  });

  it(`${fui("wm.item-create.dialog:another", locale)}clears the name and description but keeps the rest after create another, and offers open and undo (${locale})`, () => {
    const onOpen = vi.fn();
    const onUndo = vi.fn();
    const onSubmit = vi.fn();
    const { rerender } = renderPureUi(
      <ItemCreateDialog {...props(copy, { onSubmit })} />,
    );
    typeName(copy, "First");
    fireEvent.click(screen.getByLabelText(new RegExp(`^${copy.another}`)));
    fireEvent.click(screen.getByRole("button", { name: "Data" }));
    fireEvent.click(
      screen.getByRole("button", { name: new RegExp(`^${copy.submit}`) }),
    );
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ another: true }),
    );
    rerender(
      <ItemCreateDialog
        {...props(copy, {
          onSubmit,
          created: { nonce: 1, title: "First", onOpen, onUndo },
        })}
      />,
    );
    expect(
      (
        screen.getByRole("textbox", {
          name: /^Tên công việc|^Item name/,
        }) as HTMLInputElement
      ).value,
    ).toBe("");
    expect(
      screen.getByRole("button", { name: "Data" }).getAttribute("aria-pressed"),
    ).toBe("true");
    const strip = screen.getByRole("status");
    expect(strip.textContent).toContain("First");
    fireEvent.click(within(strip).getByRole("button", { name: copy.open }));
    expect(onOpen).toHaveBeenCalledTimes(1);
    fireEvent.click(within(strip).getByRole("button", { name: copy.undo }));
    expect(onUndo).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("status")).toBeNull();
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

  it(`${fui("wm.item-create.dialog:prefill", locale)}starts with the day the Calendar or Timeline passed (${locale})`, () => {
    const onSubmit = vi.fn();
    renderPureUi(
      <ItemCreateDialog
        {...props(copy, {
          onSubmit,
          defaultStartDay: "2026-10-12",
          defaultDueDay: "2026-10-14",
        })}
      />,
    );
    expect(
      (screen.getByLabelText(copy.startDate) as HTMLInputElement).value,
    ).toBe("12/10/2026");
    expect(
      (screen.getByLabelText(copy.dueDate) as HTMLInputElement).value,
    ).toBe("14/10/2026");
    typeName(copy, "Plan the week");
    fireEvent.click(
      screen.getByRole("button", { name: new RegExp(`^${copy.submit}`) }),
    );
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ startDay: "2026-10-12", dueDay: "2026-10-14" }),
    );
  });

  it(`names why nothing can be added on a board without a group and adds one on request (${locale})`, () => {
    const onSubmit = vi.fn();
    const onAddGroup = vi.fn();
    renderPureUi(
      <ItemCreateDialog
        {...props(copy, {
          onSubmit,
          onAddGroup,
          groups: [],
          defaultGroupId: "",
        })}
      />,
    );
    expect(screen.getByText(copy.noGroup)).toBeTruthy();
    typeName(copy, "First item");
    const submit = screen.getByRole("button", {
      name: new RegExp(`^${copy.submit}`),
    }) as HTMLButtonElement;
    expect(submit.disabled).toBe(true);
    fireEvent.click(submit);
    expect(onSubmit).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: copy.addGroup }));
    expect(onAddGroup).toHaveBeenCalledTimes(1);
  });

  it(`names the missing board while the form cannot be submitted (${locale})`, () => {
    renderPureUi(<ItemCreateDialog {...props(copy, { unavailable: true })} />);
    expect(screen.getByText(copy.chooseBoard)).toBeTruthy();
  });
});
