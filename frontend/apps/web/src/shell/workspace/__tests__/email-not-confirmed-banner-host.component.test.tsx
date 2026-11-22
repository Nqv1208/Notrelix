import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";

const h = vi.hoisted(() => ({
  user: null as null | { email: string; emailConfirmed?: boolean },
  resend: vi.fn(),
  success: vi.fn(),
  error: vi.fn(),
}));

vi.mock("@notrelix/features-auth", () => ({
  useCurrentUser: () => h.user,
  createAuthFlowService: () => ({ resendEmailVerification: h.resend }),
}));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: {} } }),
}));
vi.mock("sonner", () => ({
  toast: { success: h.success, error: h.error },
}));

import { EmailNotConfirmedBannerHost } from "../email-not-confirmed-banner-host";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | null = null;
let host: HTMLDivElement | null = null;
beforeEach(() => {
  vi.useFakeTimers({ toFake: ["setInterval", "clearInterval", "Date"] });
  h.user = { email: "owner@example.com", emailConfirmed: false };
  h.resend.mockReset().mockResolvedValue(undefined);
  h.success.mockReset();
  h.error.mockReset();
});
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  vi.useRealTimers();
});

async function mount(locale: "en" | "vi" = "en") {
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  await act(async () => {
    root!.render(
      <I18nProvider catalogs={[]} initialLocale={locale}>
        <EmailNotConfirmedBannerHost />
      </I18nProvider>,
    );
  });
}
const text = () => host?.textContent ?? "";
const button = () => host?.querySelector("button") as HTMLButtonElement | null;
const press = async () =>
  act(async () => {
    button()?.click();
  });

describe("EmailNotConfirmedBannerHost", () => {
  it("is a status bar naming the address with a real resend button while the email is not confirmed", async () => {
    await mount();
    const bar = host!.querySelector('[role="status"]');
    expect(bar).not.toBeNull();
    expect(text()).toContain("Your email is not confirmed.");
    expect(text()).toContain("owner@example.com");
    expect(button()?.tagName).toBe("BUTTON");
    expect(button()?.textContent).toBe("Resend email");
    expect(button()?.disabled).toBe(false);
  });

  it("speaks Vietnamese when the language is Vietnamese", async () => {
    await mount("vi");
    expect(text()).toContain("Email của bạn chưa được xác nhận.");
    expect(button()?.textContent).toBe("Gửi lại email");
  });

  it.each([
    ["a confirmed email", { email: "a@b.test", emailConfirmed: true }],
    ["a profile that does not say", { email: "a@b.test" }],
    ["nobody signed in", null],
  ])("is hidden for %s", async (_name, user) => {
    h.user = user;
    await mount();
    expect(text()).toBe("");
  });

  it("sends the link once, says so, and waits 60 seconds before another", async () => {
    await mount();
    await press();
    expect(h.resend).toHaveBeenCalledTimes(1);
    expect(h.resend).toHaveBeenCalledWith("owner@example.com");
    expect(h.success).toHaveBeenCalledWith(
      "We sent a new confirmation link to owner@example.com.",
    );
    expect(button()?.disabled).toBe(true);
    expect(button()?.textContent).toBe("Resend in 60s");

    await press();
    expect(h.resend, "a disabled button sends nothing").toHaveBeenCalledTimes(
      1,
    );

    await act(async () => {
      vi.advanceTimersByTime(30_000);
    });
    expect(button()?.textContent).toBe("Resend in 30s");

    await act(async () => {
      vi.advanceTimersByTime(30_000);
    });
    expect(button()?.disabled).toBe(false);
    expect(button()?.textContent).toBe("Resend email");
    await press();
    expect(h.resend).toHaveBeenCalledTimes(2);
  });

  it("disables the button while the call is in flight", async () => {
    let finish: () => void = () => undefined;
    h.resend.mockReturnValue(
      new Promise<void>((resolve) => (finish = resolve)),
    );
    await mount();
    await press();
    expect(button()?.disabled).toBe(true);
    expect(button()?.textContent).toBe("Sending…");
    await press();
    expect(h.resend).toHaveBeenCalledTimes(1);
    await act(async () => finish());
    expect(button()?.textContent).toBe("Resend in 60s");
  });

  it("says it could not send, and lets the person try again at once", async () => {
    h.resend.mockRejectedValue({ kind: "network" });
    await mount();
    await press();
    expect(h.error).toHaveBeenCalledWith(
      "The confirmation email could not be sent. Try again shortly.",
    );
    expect(h.success).not.toHaveBeenCalled();
    expect(button()?.disabled).toBe(false);
    expect(button()?.textContent).toBe("Resend email");
  });

  it("goes away when the profile reads as confirmed", async () => {
    await mount();
    expect(text()).not.toBe("");
    h.user = { email: "owner@example.com", emailConfirmed: true };
    await act(async () => {
      root!.render(
        <I18nProvider catalogs={[]} initialLocale="en">
          <EmailNotConfirmedBannerHost />
        </I18nProvider>,
      );
    });
    expect(text()).toBe("");
  });
});
