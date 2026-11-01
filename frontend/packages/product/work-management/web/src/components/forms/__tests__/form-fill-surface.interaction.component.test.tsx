import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import {
  BOARD_FIELDS,
  formFixture,
  formsCopyFixture,
} from "../../../verification/forms-fixtures";
import { fillFormsCopy } from "../forms.copy";
import {
  FormFillSurface,
  type FormFillSurfaceProps,
} from "../form-fill-surface";

function render(
  locale: "vi" | "en",
  overrides: Partial<FormFillSurfaceProps> = {},
) {
  const props: FormFillSurfaceProps = {
    copy: formsCopyFixture(locale),
    status: "ready",
    form: formFixture({ status: "published" }),
    fields: BOARD_FIELDS,
    boardTitle: "Triển khai ERP",
    user: { name: "Hà Nguyễn", email: "ha.nguyen@minhan.vn" },
    todayLabel: "Thứ Hai 12/10/2026",
    canSubmit: true,
    onBack: vi.fn(),
    onHome: vi.fn(),
    onReload: vi.fn(),
    onSubmit: vi.fn(async () => ({ ok: true }) as const),
    ...overrides,
  };
  renderPureUi(
    // The date answer reads its copy from the host locale provider.
    <I18nProvider catalogs={[]} initialLocale={locale}>
      <FormFillSurface {...props} />
    </I18nProvider>,
  );
  return props;
}

const fui = (id: string, locale: string) =>
  locale === "vi" ? `FUI[wm.forms.fill:${id}] ` : "";
const box = (name: RegExp | string) => screen.getByRole("textbox", { name });

describe.each(["vi", "en"] as const)("form fill page (%s)", (locale) => {
  const copy = formsCopyFixture(locale);
  const f = copy.fill;

  it(`${fui("validation", locale)}checks required and email before it sends, with a summary that links to each question (${locale})`, async () => {
    const props = render(locale);
    fireEvent.change(box(/Email liên hệ/), { target: { value: "minh.an" } });
    fireEvent.click(screen.getByRole("button", { name: f.submit }));
    const summary = await screen.findByRole("alert");
    expect(summary.textContent).toContain(
      fillFormsCopy(f.summaryCount, { count: 3 }),
    );
    expect(screen.getByText(f.errEmail)).toBeTruthy();
    expect(props.onSubmit).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Bạn cần gì?" }));
    expect(document.activeElement?.getAttribute("aria-invalid")).toBe("true");
  });

  it(`${fui("submit", locale)}sends the answers by question key, locks while sending and then confirms (${locale})`, async () => {
    let finish: (value: { ok: true }) => void = () => undefined;
    const onSubmit = vi.fn(
      (_answers: Readonly<Record<string, unknown>>, _key: string) =>
        new Promise<{ ok: true }>((resolve) => (finish = resolve)),
    );
    render(locale, { onSubmit });
    fireEvent.change(box(/Bạn cần gì/), {
      target: { value: "Làm mới tên máy" },
    });
    fireEvent.change(screen.getByRole("combobox", { name: /Mức độ ưu tiên/ }), {
      target: { value: "p-high" },
    });
    // The date is typed as dd/mm/yyyy in the house date field; the answer is the day as yyyy-mm-dd.
    fireEvent.change(screen.getByLabelText(/Hạn mong muốn/), {
      target: { value: "16/10/2026" },
    });
    fireEvent.click(screen.getByRole("button", { name: f.submit }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0]).toEqual({
      title: "Làm mới tên máy",
      priority: "p-high",
      due: "2026-10-16",
    });
    expect(
      (screen.getByRole("button", { name: f.sending }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    finish({ ok: true });
    await screen.findByRole("heading", { name: f.thanks });
    expect(screen.getByRole("button", { name: f.another })).toBeTruthy();
  });

  it(`${fui("network", locale)}keeps the answers when sending fails and retries with the same key, a new key once an answer changes (${locale})`, async () => {
    const onSubmit = vi.fn(
      async () => ({ ok: false, reason: "failed" }) as const,
    );
    render(locale, { onSubmit });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "A" } });
    fireEvent.change(screen.getByRole("combobox", { name: /Mức độ ưu tiên/ }), {
      target: { value: "p-low" },
    });
    fireEvent.click(screen.getByRole("button", { name: f.submit }));
    await screen.findByText(f.networkError);
    expect((box(/Bạn cần gì/) as HTMLInputElement).value).toBe("A");
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(2));
    const keys = onSubmit.mock.calls.map(
      (call) => (call as unknown as [unknown, string])[1],
    );
    expect(keys[0]).toBe(keys[1]);
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "AB" } });
    fireEvent.click(screen.getByRole("button", { name: f.submit }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(3));
    expect(
      (onSubmit.mock.calls[2] as unknown as [unknown, string])[1],
    ).not.toBe(keys[0]);
  });

  it(`${fui("server-errors", locale)}shows the producer's refusal under the question it names (${locale})`, async () => {
    const onSubmit = vi.fn(
      async () =>
        ({ ok: false, reason: "validation", errors: { Email: "x" } }) as const,
    );
    render(locale, { onSubmit });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "A" } });
    fireEvent.change(screen.getByRole("combobox", { name: /Mức độ ưu tiên/ }), {
      target: { value: "p-low" },
    });
    fireEvent.click(screen.getByRole("button", { name: f.submit }));
    expect(await screen.findByText(f.errServer)).toBeTruthy();
  });

  it(`${fui("closed-midway", locale)}says the form closed while it was being filled and lets the answers be copied (${locale})`, async () => {
    render(locale, {
      onSubmit: vi.fn(async () => ({ ok: false, reason: "conflict" }) as const),
    });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "A" } });
    fireEvent.change(screen.getByRole("combobox", { name: /Mức độ ưu tiên/ }), {
      target: { value: "p-low" },
    });
    fireEvent.click(screen.getByRole("button", { name: f.submit }));
    await screen.findByText(f.closedMidwayTitle);
    expect(screen.getByRole("button", { name: f.copyAnswers })).toBeTruthy();
  });

  it(`${fui("states", locale)}shows closed, draft, not found, no permission, loading and load error pages (${locale})`, () => {
    const cases: [Partial<FormFillSurfaceProps>, string][] = [
      [{ form: formFixture({ status: "closed" }) }, f.closedTitle],
      [{ form: formFixture({ status: "draft" }) }, f.draftTitle],
      [{ status: "notFound", form: null }, f.notFoundTitle],
      [{ canSubmit: false }, f.cannotSubmitTitle],
    ];
    for (const [overrides, title] of cases) {
      render(locale, overrides);
      expect(screen.getByRole("heading", { name: title })).toBeTruthy();
      document.body.innerHTML = "";
    }
    render(locale, { status: "loading", form: null });
    expect(
      screen.getByRole("status", { name: copy.common.loading }),
    ).toBeTruthy();
    document.body.innerHTML = "";
    const props = render(locale, { status: "error", form: null });
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    expect(props.onReload).toHaveBeenCalled();
  });

  it(`${fui("a11y", locale)}ties each box to its label, marks required ones for screen readers and shows who sends (${locale})`, () => {
    render(locale);
    expect(box(/Bạn cần gì/).getAttribute("aria-required")).toBe("true");
    expect(screen.getAllByText(copy.common.required)[0]!.className).toContain(
      "sr-only",
    );
    expect(
      screen.getByText(
        fillFormsCopy(f.asUser, {
          name: "Hà Nguyễn",
          email: "ha.nguyen@minhan.vn",
        }),
      ),
    ).toBeTruthy();
    expect(
      screen.getByText(
        fillFormsCopy(f.todayIs, { date: "Thứ Hai 12/10/2026" }),
      ),
    ).toBeTruthy();
    expect(screen.queryByRole("button", { name: /⌘|Ctrl/ })).toBeNull();
  });
});
