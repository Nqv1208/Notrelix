import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  BOARD_FIELDS,
  Q_OWN_CHOICES,
  Q_PRIORITY,
  formFixture,
  formsCopyFixture,
  question,
} from "../../../verification/forms-fixtures";
import { fillFormsCopy } from "../forms.copy";
import {
  FormBuilderSurface,
  type FormBuilderSurfaceProps,
} from "../form-builder-surface";

const ok = async () => ({ ok: true }) as const;

function render(
  locale: "vi" | "en",
  overrides: Partial<FormBuilderSurfaceProps> = {},
) {
  const props: FormBuilderSurfaceProps = {
    copy: formsCopyFixture(locale),
    status: "ready",
    form: formFixture(),
    fields: BOARD_FIELDS,
    boardTitle: "Triển khai ERP",
    selectedId: null,
    onSelect: vi.fn(),
    savedAt: "09:42",
    preview: false,
    onPreview: vi.fn(),
    linkUrl: "https://notrelix.test/workspaces/w/boards/b/forms/form-it",
    onUpdateDetails: vi.fn(ok),
    onAddQuestion: vi.fn(ok),
    onUpdateQuestion: vi.fn(ok),
    onRemoveQuestion: vi.fn(ok),
    onPublish: vi.fn(ok),
    onClose: vi.fn(ok),
    onCopyLink: vi.fn(async () => true),
    onReload: vi.fn(),
    onBack: vi.fn(),
    ...overrides,
  };
  renderPureUi(
    // The date answer reads its copy from the host locale provider.
    <I18nProvider catalogs={[]} initialLocale={locale}>
      <FormBuilderSurface {...props} />
    </I18nProvider>,
  );
  return props;
}

const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[wm.forms.builder:${id}] ` : "";

describe.each(["vi", "en"] as const)("form builder (%s)", (locale) => {
  const copy = formsCopyFixture(locale);
  const b = copy.builder;
  const up = (name: string) =>
    screen.getByRole("button", { name: fillFormsCopy(b.moveUp, { name }) });
  const down = (name: string) =>
    screen.getByRole("button", { name: fillFormsCopy(b.moveDown, { name }) });

  it(`${fui("rows", locale)}lists the questions in order with their type, mapping and required chip, the ends' buttons disabled (${locale})`, () => {
    render(locale);
    const list = screen.getByRole("list", { name: b.list });
    expect(
      within(list).getAllByRole("button", { pressed: false }),
    ).toHaveLength(5);
    expect(
      within(list).getByText(
        new RegExp(
          `${b.types.select} · ${fillFormsCopy(b.mappedTo, { field: "Ưu tiên" })}`,
        ),
      ),
    ).toBeTruthy();
    expect(
      within(list).getByText(new RegExp(`${b.types.email} · ${b.noMapping}`)),
    ).toBeTruthy();
    expect(up("Bạn cần gì?").getAttribute("aria-disabled")).toBe("true");
    expect(down("Email liên hệ").getAttribute("aria-disabled")).toBe("true");
    expect(down("Bạn cần gì?").getAttribute("aria-disabled")).toBeNull();
  });

  it(`${fui("reorder", locale)}moves a question with the Up and Down buttons by sending a key between its new neighbours (${locale})`, async () => {
    const props = render(locale);
    fireEvent.click(down("Bạn cần gì?"));
    await waitFor(() =>
      expect(props.onUpdateQuestion).toHaveBeenCalledTimes(1),
    );
    const [moved, changes] = vi.mocked(props.onUpdateQuestion).mock.calls[0]!;
    expect(moved.id).toBe("q-title");
    // Between "a1" (Mô tả) and "a2" (Ưu tiên).
    expect(changes.position > "a1" && changes.position < "a2").toBe(true);
    fireEvent.click(up("Mô tả chi tiết"));
    await waitFor(() =>
      expect(props.onUpdateQuestion).toHaveBeenCalledTimes(2),
    );
    expect(
      vi.mocked(props.onUpdateQuestion).mock.calls[1]![1].position < "a0",
    ).toBe(true);
    // The first question's Up does nothing.
    fireEvent.click(up("Bạn cần gì?"));
    expect(props.onUpdateQuestion).toHaveBeenCalledTimes(2);
  });

  it(`${fui("mapped-options", locale)}shows a mapped select's options as read-only text, with nothing to add, move or remove (${locale})`, () => {
    render(locale, { selectedId: Q_PRIORITY.id });
    const panel = screen.getByRole("complementary", { name: b.panel });
    const note = within(panel).getByTestId("mapped-options");
    expect(note.textContent).toContain("Thấp, Trung bình, Cao, Khẩn");
    expect(
      within(panel).queryByRole("button", { name: b.addOption }),
    ).toBeNull();
    expect(
      within(panel).queryByRole("textbox", { name: /Lựa chọn|Choice/ }),
    ).toBeNull();
    expect(
      within(panel).queryByRole("button", {
        name: /Xoá lựa chọn|Delete choice/,
      }),
    ).toBeNull();
  });

  it(`${fui("own-options", locale)}edits the choices of an unmapped select in place: add, rename, move, remove (${locale})`, async () => {
    const props = render(locale, {
      form: formFixture({ questions: [Q_OWN_CHOICES] }),
      selectedId: Q_OWN_CHOICES.id,
    });
    const panel = screen.getByRole("complementary", { name: b.panel });
    fireEvent.click(
      within(panel).getByRole("button", {
        name: fillFormsCopy(b.moveOptionDown, { name: "ERP" }),
      }),
    );
    await waitFor(() => expect(props.onUpdateQuestion).toHaveBeenCalled());
    expect(
      vi
        .mocked(props.onUpdateQuestion)
        .mock.calls[0]![1].options?.map((o) => o.label),
    ).toEqual(["Email", "ERP"]);
    fireEvent.click(
      within(panel).getByRole("button", {
        name: fillFormsCopy(b.removeOption, { name: "Email" }),
      }),
    );
    await waitFor(() =>
      expect(props.onUpdateQuestion).toHaveBeenCalledTimes(2),
    );
    expect(
      vi
        .mocked(props.onUpdateQuestion)
        .mock.calls[1]![1].options?.map((o) => o.label),
    ).toEqual(["ERP"]);
  });

  it(`${fui("duplicate", locale)}marks two questions on one field and keeps the form from publishing (${locale})`, () => {
    const second = question("q-urgent", "a5", {
      key: "urgent",
      label: "Mức độ khẩn cấp",
      type: "select",
      boardFieldId: Q_PRIORITY.boardFieldId,
    });
    render(locale, { form: formFixture({ questions: [Q_PRIORITY, second] }) });
    expect(screen.getAllByText(b.duplicate)).toHaveLength(2);
    expect(
      (screen.getByRole("button", { name: b.publish }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(screen.getByRole("status").textContent).toContain("Ưu tiên");
  });

  it(`${fui("publish", locale)}needs a question, then publishes and shows the member link to copy (${locale})`, async () => {
    render(locale, { form: formFixture({ questions: [] }) });
    expect(
      (screen.getByRole("button", { name: b.publish }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(screen.getByText(b.publishNoQuestions)).toBeTruthy();
    cleanup();
    const props = render(locale);
    fireEvent.click(screen.getByRole("button", { name: b.publish }));
    expect(props.onPublish).toHaveBeenCalled();
    cleanup();
    const published = render(locale, {
      form: formFixture({ status: "published" }),
    });
    expect(
      (screen.getByRole("textbox", { name: b.linkLabel }) as HTMLInputElement)
        .value,
    ).toContain("/forms/form-it");
    fireEvent.click(screen.getByRole("button", { name: b.copy }));
    await screen.findByRole("button", { name: b.copied });
    expect(published.onCopyLink).toHaveBeenCalled();
  });

  it(`${fui("autosave", locale)}saves a label when it is left, asks for one when it is empty, and names a refused save without losing the text (${locale})`, async () => {
    const onUpdateQuestion = vi.fn(
      async () => ({ ok: false, reason: "failed" }) as const,
    );
    render(locale, { selectedId: "q-title", onUpdateQuestion });
    const label = screen.getByRole("textbox", { name: b.label });
    fireEvent.change(label, { target: { value: "" } });
    fireEvent.blur(label);
    expect(screen.getByText(b.labelRequired)).toBeTruthy();
    expect(onUpdateQuestion).not.toHaveBeenCalled();
    fireEvent.change(label, { target: { value: "Bạn cần hỗ trợ gì?" } });
    fireEvent.blur(label);
    await screen.findByText(b.saveFailed);
    expect((label as HTMLInputElement).value).toBe("Bạn cần hỗ trợ gì?");
    expect(onUpdateQuestion).toHaveBeenCalledWith(
      expect.objectContaining({ id: "q-title" }),
      expect.objectContaining({ label: "Bạn cần hỗ trợ gì?" }),
    );
  });

  it(`${fui("conflict", locale)}tells a form someone else edited and reloads it, never saving over it (${locale})`, async () => {
    const onUpdateQuestion = vi.fn(
      async () => ({ ok: false, reason: "conflict" }) as const,
    );
    const props = render(locale, { selectedId: "q-title", onUpdateQuestion });
    fireEvent.click(screen.getByRole("switch", { name: b.requiredSwitch }));
    await screen.findByText(b.conflict);
    fireEvent.click(screen.getByRole("button", { name: b.reload }));
    expect(props.onReload).toHaveBeenCalled();
  });

  it(`${fui("remove", locale)}asks before it removes a question (${locale})`, async () => {
    const props = render(locale, { selectedId: "q-email" });
    fireEvent.click(screen.getByRole("button", { name: b.removeQuestion }));
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: copy.common.keep,
      }),
    );
    expect(props.onRemoveQuestion).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: b.removeQuestion }));
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: b.remove,
      }),
    );
    await waitFor(() =>
      expect(props.onRemoveQuestion).toHaveBeenCalledWith(
        expect.objectContaining({ id: "q-email" }),
      ),
    );
  });

  it(`${fui("add", locale)}adds a question of a type, offers files and signatures as coming soon, and an item's name once (${locale})`, async () => {
    const props = render(locale);
    fireEvent.click(screen.getByRole("button", { name: b.addQuestion }));
    expect(
      screen
        .getByRole("button", { name: new RegExp(b.types.file) })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    expect(
      (screen.getByRole("button", { name: b.mapTitle }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: b.types.number }));
    await waitFor(() =>
      expect(props.onAddQuestion).toHaveBeenCalledWith(
        expect.objectContaining({ type: "number", boardFieldId: null }),
      ),
    );
    expect(
      vi.mocked(props.onAddQuestion).mock.calls[0]![0].position > "a4",
    ).toBe(true);
  });

  it(`${fui("closed", locale)}is read-only when the form is closed (${locale})`, () => {
    render(locale, {
      form: formFixture({ status: "closed" }),
      selectedId: "q-title",
    });
    expect(screen.getByText(b.closedNote)).toBeTruthy();
    expect(screen.queryByRole("button", { name: b.addQuestion })).toBeNull();
    expect(screen.queryByRole("button", { name: b.removeQuestion })).toBeNull();
    expect(
      (screen.getByRole("textbox", { name: b.label }) as HTMLInputElement)
        .disabled,
    ).toBe(true);
  });

  it(`${fui("states", locale)}shows loading, a load error with retry, and no access without naming the form (${locale})`, () => {
    render(locale, { status: "loading", form: null });
    expect(
      screen.getByRole("status", { name: copy.common.loading }),
    ).toBeTruthy();
    cleanup();
    const props = render(locale, { status: "error", form: null });
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    expect(props.onReload).toHaveBeenCalled();
    cleanup();
    render(locale, { status: "missing", form: null });
    expect(screen.getByText(copy.common.noAccess)).toBeTruthy();
  });

  it(`${fui("preview", locale)}previews the member page with the send button locked (${locale})`, () => {
    render(locale, { preview: true });
    expect(screen.getByText(b.previewBanner)).toBeTruthy();
    expect(
      (
        screen.getByRole("button", {
          name: copy.fill.submit,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});

function cleanup() {
  document.body.innerHTML = "";
}
