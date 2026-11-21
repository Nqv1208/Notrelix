import { useEffect } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import {
  act,
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
} from "@notrelix/testing";
import type {
  PublicForm,
  PublicFormSubmitResult,
} from "@notrelix/work-management-core";
import {
  formsCopyFixture,
  question,
} from "../../../verification/forms-fixtures";
import type { CaptchaWidgetProps } from "../captcha-widget";
import {
  PublicFormSurface,
  type PublicFormSurfaceProps,
} from "../public-form-surface";

const FORM: PublicForm = {
  name: "Khảo sát khách hàng",
  description: "Cho chúng tôi biết ý kiến của bạn.",
  captchaSiteKey: "site-key",
  questions: [
    question("title", "", {
      key: "title",
      label: "Bạn cần gì?",
      type: "text",
      required: true,
    }),
    question("email", "", { key: "email", label: "Email", type: "email" }),
    question("kind", "", {
      key: "kind",
      label: "Loại",
      type: "select",
      options: [
        { key: "bug", label: "Lỗi" },
        { key: "idea", label: "Ý tưởng" },
      ],
    }),
  ],
};

/** A captcha that is solved the moment it shows and after every reset, and remembers each reset. */
function solvedCaptcha(resets: number[]) {
  return function SolvedCaptcha({ onToken, resetKey = 0 }: CaptchaWidgetProps) {
    useEffect(() => {
      resets.push(resetKey);
      onToken("captcha-test-pass");
      // The token is solved once per key: a real widget does the same after a reset.
    }, [resetKey]);
    return <div data-testid="captcha" />;
  };
}

let held: CaptchaWidgetProps | null = null;
/** A captcha that waits: the test solves it by hand. */
function HeldCaptcha(props: CaptchaWidgetProps) {
  held = props;
  return <div data-testid="captcha" />;
}

function render(
  locale: "vi" | "en",
  overrides: Partial<PublicFormSurfaceProps> = {},
) {
  const resets: number[] = [];
  const props: PublicFormSurfaceProps = {
    copy: formsCopyFixture(locale),
    status: "ready",
    form: FORM,
    Captcha: solvedCaptcha(resets),
    onReload: vi.fn(),
    onSubmit: vi.fn(async (): Promise<PublicFormSubmitResult> => ({
      ok: true,
    })),
    ...overrides,
  };
  const view = renderPureUi(
    <I18nProvider catalogs={[]} initialLocale={locale}>
      <PublicFormSurface {...props} />
    </I18nProvider>,
  );
  return { props, resets, view };
}

const box = (name: RegExp | string) => screen.getByRole("textbox", { name });

afterEach(() => {
  held = null;
});

describe.each(["vi", "en"] as const)("public form page (%s)", (locale) => {
  const copy = formsCopyFixture(locale);
  const p = copy.public;
  const send = () => screen.getByRole("button", { name: p.submit });

  it("shows a skeleton while loading, then the form", () => {
    const { view, props } = render(locale, { status: "loading", form: null });
    expect(
      screen.getByRole("status", { name: copy.common.loading }),
    ).toBeTruthy();
    view.rerender(
      <I18nProvider catalogs={[]} initialLocale={locale}>
        <PublicFormSurface {...props} status="ready" form={FORM} />
      </I18nProvider>,
    );
    expect(screen.getByRole("heading", { name: FORM.name })).toBeTruthy();
    expect(screen.getByText(FORM.description)).toBeTruthy();
  });

  it("keeps Send off until the captcha gives a token, then sends it", async () => {
    const { props } = render(locale, { Captcha: HeldCaptcha });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "Một yêu cầu" } });
    expect((send() as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText(p.captchaWaiting)).toBeTruthy();
    act(() => held!.onToken("captcha-test-pass"));
    expect((send() as HTMLButtonElement).disabled).toBe(false);
    act(() => held!.onExpire());
    expect((send() as HTMLButtonElement).disabled).toBe(true);
    act(() => held!.onToken("captcha-test-pass"));
    fireEvent.click(send());
    await waitFor(() => expect(props.onSubmit).toHaveBeenCalled());
  });

  it("sends the answers and the token with an empty honeypot, then thanks the person", async () => {
    let finish: (value: PublicFormSubmitResult) => void = () => undefined;
    const onSubmit = vi.fn(
      () =>
        new Promise<PublicFormSubmitResult>((resolve) => (finish = resolve)),
    );
    render(locale, { onSubmit });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "Một yêu cầu" } });
    fireEvent.change(screen.getByRole("combobox", { name: /Loại/ }), {
      target: { value: "idea" },
    });
    fireEvent.click(send());
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit).toHaveBeenCalledWith(
      { title: "Một yêu cầu", kind: "idea" },
      "captcha-test-pass",
      "",
    );
    expect(
      (screen.getByRole("button", { name: p.sending }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    await act(async () => finish({ ok: true }));
    expect(await screen.findByRole("heading", { name: p.thanks })).toBeTruthy();
  });

  it("checks the required answers before it sends anything", () => {
    const { props } = render(locale);
    fireEvent.click(send());
    expect(screen.getByText(copy.fill.errRequired)).toBeTruthy();
    expect(props.onSubmit).not.toHaveBeenCalled();
  });

  it("shows a form that is not available with no hint why, also when it has no captcha", () => {
    const { view, props } = render(locale, {
      status: "unavailable",
      form: null,
    });
    expect(
      screen.getByRole("heading", { name: p.unavailableTitle }),
    ).toBeTruthy();
    expect(screen.getByText(p.unavailableBody)).toBeTruthy();
    view.rerender(
      <I18nProvider catalogs={[]} initialLocale={locale}>
        <PublicFormSurface
          {...props}
          status="ready"
          form={{ ...FORM, captchaSiteKey: null }}
        />
      </I18nProvider>,
    );
    expect(
      screen.getByRole("heading", { name: p.unavailableTitle }),
    ).toBeTruthy();
    expect(screen.queryByRole("button", { name: p.submit })).toBeNull();
  });

  it("turns into the unavailable page when the link stops opening while the person fills it", async () => {
    render(locale, {
      onSubmit: async () => ({ ok: false, reason: "missing" }),
    });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "x" } });
    fireEvent.click(send());
    expect(
      await screen.findByRole("heading", { name: p.unavailableTitle }),
    ).toBeTruthy();
  });

  it("shows a load error with a retry", () => {
    const { props } = render(locale, { status: "error", form: null });
    expect(screen.getByText(copy.fill.loadError)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.common.retry }));
    expect(props.onReload).toHaveBeenCalled();
  });

  it("puts a server refusal of an answer under that question, keeps what was typed and restarts the captcha", async () => {
    const { resets } = render(locale, {
      onSubmit: async () => ({
        ok: false,
        reason: "validation",
        errors: { email: "Expected an email address." },
      }),
    });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "Một yêu cầu" } });
    fireEvent.click(send());
    expect(await screen.findByText(copy.fill.errServer)).toBeTruthy();
    expect((box(/Bạn cần gì/) as HTMLInputElement).value).toBe("Một yêu cầu");
    // Mounted once, reset once: the spent token is never reused.
    await waitFor(() => expect(resets).toEqual([0, 1]));
  });

  it("says the captcha failed, restarts it and stays on the form when the token is refused", async () => {
    const { resets } = render(locale, {
      onSubmit: async () => ({ ok: false, reason: "captcha" }),
    });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "x" } });
    fireEvent.click(send());
    expect((await screen.findByRole("alert")).textContent).toBe(p.captchaError);
    await waitFor(() => expect(resets).toEqual([0, 1]));
    // The restarted challenge was solved again: the message clears and Send is on.
    await waitFor(() => expect(screen.queryByText(p.captchaError)).toBeNull());
  });

  it("shows the server's own sentence when the visitor is rate limited", async () => {
    render(locale, {
      onSubmit: async () => ({
        ok: false,
        reason: "limited",
        message: "Bạn đã gửi quá nhiều lần. Vui lòng thử lại sau ít phút.",
      }),
    });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "x" } });
    fireEvent.click(send());
    expect(
      await screen.findByText(
        "Bạn đã gửi quá nhiều lần. Vui lòng thử lại sau ít phút.",
      ),
    ).toBeTruthy();
  });

  it("says the response was not sent on an unknown failure and keeps the answers", async () => {
    render(locale, { onSubmit: async () => ({ ok: false, reason: "failed" }) });
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "x" } });
    fireEvent.click(send());
    expect(await screen.findByText(p.failed)).toBeTruthy();
    expect((box(/Bạn cần gì/) as HTMLInputElement).value).toBe("x");
  });

  it("has a hidden honeypot named website that the page never fills", async () => {
    const { props } = render(locale);
    const honeypot = document.querySelector(
      'input[name="website"]',
    ) as HTMLInputElement;
    expect(honeypot).toBeTruthy();
    expect(honeypot.getAttribute("aria-hidden")).toBe("true");
    expect(honeypot.tabIndex).toBe(-1);
    expect(honeypot.value).toBe("");
    expect(screen.queryByRole("textbox", { name: "website" })).toBeNull();
    fireEvent.change(box(/Bạn cần gì/), { target: { value: "x" } });
    fireEvent.click(send());
    await waitFor(() => expect(props.onSubmit).toHaveBeenCalled());
    expect(vi.mocked(props.onSubmit).mock.calls[0]![2]).toBe("");
  });
});
