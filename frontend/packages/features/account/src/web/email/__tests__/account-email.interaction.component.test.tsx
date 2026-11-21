import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { securityCopyFixture } from "../../../verification/account-security-fixtures";
import {
  AccountEmailSurface,
  type AccountEmailFlow,
  type AccountEmailSurfaceProps,
} from "../account-email-surface";

const LOCALES = ["vi", "en"] as const;

function renderPage(flow: AccountEmailFlow | null, locale: "vi" | "en") {
  const props: AccountEmailSurfaceProps = {
    copy: securityCopyFixture(locale),
    status: "ready",
    currentEmail: "ada@example.com",
    busy: false,
    error: null,
    notice: null,
    flow,
    onSubmitEmail: vi.fn(),
    onCloseFlow: vi.fn(),
  };
  renderPureUi(<AccountEmailSurface {...props} />);
  return props;
}

describe.each(LOCALES)("the sign-in email page (%s)", (locale) => {
  const t = securityCopyFixture(locale).email;

  it("shows the current address and keeps it read-only", () => {
    renderPage(null, locale);
    const current = screen.getByLabelText(t.current) as HTMLInputElement;
    expect(current.value).toBe("ada@example.com");
    expect(current.readOnly).toBe(true);
  });

  it("tells a malformed or current address before sending, and sends a good one", () => {
    const props = renderPage(null, locale);
    const field = screen.getByLabelText(t.label);
    const submit = screen.getByRole("button", { name: t.submit });

    fireEvent.change(field, { target: { value: "not-an-email" } });
    fireEvent.click(submit);
    expect(screen.getByRole("alert").textContent).toBe(t.invalid);

    fireEvent.change(field, { target: { value: "ADA@example.com" } });
    fireEvent.click(submit);
    expect(props.onSubmitEmail).not.toHaveBeenCalled();

    fireEvent.change(field, { target: { value: "new@example.com" } });
    fireEvent.click(submit);
    expect(props.onSubmitEmail).toHaveBeenCalledWith("new@example.com");
  });

  it("names the proof step", () => {
    renderPage(
      { kind: "stepUp", purpose: "changeEmail", factor: "code", error: null },
      locale,
    );
    expect(screen.getByRole("region", { name: t.proofTitle })).not.toBeNull();
  });

  it("says the same words once the link is on its way", () => {
    renderPage({ kind: "emailSent", email: "new@example.com" }, locale);
    expect(screen.getByRole("status").textContent).toContain("new@example.com");
  });
});
