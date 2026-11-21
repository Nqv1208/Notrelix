import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { authCatalog } from "../../copy/auth.catalog";
import { authCopy } from "../../copy/auth.copy";
import type { AuthFlowService } from "../../../core";
import type { AuthFlowNavigation } from "../flow-navigation";
import { SignInFlow } from "../sign-in-flow";

const copy = authCopy("en");
const EMAIL = "ha@minhan.vn";

function Providers({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return (
    <I18nProvider catalogs={[authCatalog]} initialLocale="en">
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </I18nProvider>
  );
}

const navigation = () =>
  ({
    navigate: vi.fn(),
    startProviderSignIn: vi.fn(),
    startSsoSignIn: vi.fn(),
  }) satisfies AuthFlowNavigation;

const company = (enforced: boolean) => ({
  method: "sso" as const,
  sso: {
    orgName: "Minh An Group",
    orgInitials: "MA",
    provider: "oidc",
    providerLabel: "OpenID Connect",
    enforced,
  },
});

const problem = (status: number, errorCode: string) => ({
  status,
  details: { errorCode },
});

function mount(
  service: object,
  props: { providerError?: string | null; redirect?: string | null } = {},
) {
  const nav = navigation();
  renderPureUi(
    <Providers>
      <SignInFlow
        service={service as unknown as AuthFlowService}
        navigation={nav}
        {...props}
      />
    </Providers>,
  );
  return nav;
}

async function continueWith(email: string) {
  fireEvent.change(screen.getByLabelText(copy.email.label), {
    target: { value: email },
  });
  fireEvent.click(screen.getByRole("button", { name: copy.email.continue }));
}

describe("sign-in: routing by email domain", () => {
  it("offers 'Continue with <company>' for a routable domain and sends the browser to the provider", async () => {
    const startSso = vi.fn(async () => ({
      authorizationUrl: "https://login.example/authorize?state=s1",
    }));
    const nav = mount(
      { identify: vi.fn(async () => company(false)), startSso },
      { redirect: "/home" },
    );
    await continueWith(EMAIL);
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Continue with Minh An Group",
      }),
    );
    await waitFor(() =>
      expect(nav.startSsoSignIn).toHaveBeenCalledWith(
        "https://login.example/authorize?state=s1",
      ),
    );
    expect(startSso).toHaveBeenCalledWith(EMAIL, "/home");
    expect(nav.startProviderSignIn).not.toHaveBeenCalled();
  });

  it("goes to the password step, with no company button, for any other domain", async () => {
    mount({ identify: vi.fn(async () => ({ method: "password", sso: null })) });
    await continueWith("a@gmail.com");
    expect(await screen.findByLabelText(copy.password.label)).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: /Continue with Minh/ }),
    ).toBeNull();
  });

  it("keeps a way to a password and says it still works when single sign-on is not required", async () => {
    mount({ identify: vi.fn(async () => company(false)) });
    await continueWith(EMAIL);
    expect(await screen.findByText(copy.sso.optionalTitle)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.sso.usePassword }));
    const password = await screen.findByLabelText(copy.password.label);
    expect(password).toBeTruthy();
    expect(
      (screen.getByLabelText(copy.email.label) as HTMLInputElement).value,
    ).toBe(EMAIL);
  });

  it("says honestly that only the account owner can use a password when it is required, and still reaches the password", async () => {
    mount({ identify: vi.fn(async () => company(true)) });
    await continueWith(EMAIL);
    expect(await screen.findByText(copy.sso.enforcedTitle)).toBeTruthy();
    expect(
      screen.getByText(/Only the account owner can still use a password/),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.sso.usePassword }));
    expect(await screen.findByLabelText(copy.password.label)).toBeTruthy();
  });

  it("returns to the company step, saying why, when a correct password is refused for single sign-on (403)", async () => {
    const login = vi.fn(async () => {
      throw problem(403, "identity.auth.sso-required");
    });
    mount({ identify: vi.fn(async () => company(true)), login });
    await continueWith(EMAIL);
    fireEvent.click(
      await screen.findByRole("button", { name: copy.sso.usePassword }),
    );
    fireEvent.change(await screen.findByLabelText(copy.password.label), {
      target: { value: "correct-horse" },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.password.signIn }));
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(
      "Minh An Group requires single sign-on",
    );
    // Not the wrong-password message.
    expect(screen.queryByText(copy.password.wrong)).toBeNull();
    expect(
      screen.getByRole("button", { name: "Continue with Minh An Group" }),
    ).toBeTruthy();
  });

  it("still says 'wrong' for a wrong password on a company that requires it (no hint it is a company)", async () => {
    const login = vi.fn(async () => {
      throw problem(401, "identity.auth.invalid-credentials");
    });
    mount({ identify: vi.fn(async () => company(true)), login });
    await continueWith(EMAIL);
    fireEvent.click(
      await screen.findByRole("button", { name: copy.sso.usePassword }),
    );
    fireEvent.change(await screen.findByLabelText(copy.password.label), {
      target: { value: "wrong" },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.password.signIn }));
    expect((await screen.findByRole("alert")).textContent).toContain(
      copy.password.wrong,
    );
  });

  it("sends the person back to the email step when the address no longer routes (404 unavailable)", async () => {
    const startSso = vi.fn(async () => {
      throw problem(404, "identity.sso.unavailable");
    });
    const nav = mount({
      identify: vi.fn(async () => company(false)),
      startSso,
    });
    await continueWith(EMAIL);
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Continue with Minh An Group",
      }),
    );
    expect((await screen.findByRole("alert")).textContent).toContain(
      copy.email.ssoUnavailable,
    );
    expect(nav.startSsoSignIn).not.toHaveBeenCalled();
  });

  it("says it could not reach Notrelix when the start fails, and lets the person try again", async () => {
    const startSso = vi
      .fn()
      .mockRejectedValueOnce({ kind: "network" })
      .mockResolvedValue({ authorizationUrl: "https://login.example/a" });
    const nav = mount({
      identify: vi.fn(async () => company(false)),
      startSso,
    });
    await continueWith(EMAIL);
    const button = () =>
      screen.findByRole("button", { name: "Continue with Minh An Group" });
    fireEvent.click(await button());
    expect((await screen.findByRole("alert")).textContent).toContain(
      copy.sso.unreachable,
    );
    fireEvent.click(await button());
    await waitFor(() =>
      expect(nav.startSsoSignIn).toHaveBeenCalledWith(
        "https://login.example/a",
      ),
    );
  });
});

describe("sign-in: company sign-in errors from the callback redirect", () => {
  it.each([
    ["identity.sso.state-invalid", copy.email.ssoState],
    ["identity.sso.denied", copy.email.ssoDenied],
    ["identity.sso.failed", copy.email.ssoFailed],
    ["identity.sso.email-unverified", copy.email.ssoEmailUnverified],
    ["identity.sso.domain-mismatch", copy.email.ssoDomainMismatch],
    ["identity.sso.identity-conflict", copy.email.ssoIdentityConflict],
    ["identity.sso.not-provisioned", copy.email.ssoNotProvisioned],
    ["identity.sso.unavailable", copy.email.ssoUnavailable],
    ["identity.oauth.sso-required", copy.email.ssoRequiredSocial],
  ])("shows a clear message for %s", (code, text) => {
    mount({}, { providerError: code });
    expect(screen.getByRole("alert").textContent).toContain(text);
  });

  it("reads any unknown company code as a plain failure, never as its own code", () => {
    mount({}, { providerError: "identity.sso.something-new" });
    expect(screen.getByRole("alert").textContent).toContain(
      copy.email.providerFailed,
    );
    expect(screen.queryByText(/something-new/)).toBeNull();
  });

  it("keeps every company message free of account-existence hints", () => {
    const messages = [
      copy.email.ssoState,
      copy.email.ssoDenied,
      copy.email.ssoFailed,
      copy.email.ssoEmailUnverified,
      copy.email.ssoDomainMismatch,
      copy.email.ssoIdentityConflict,
      copy.email.ssoNotProvisioned,
      copy.email.ssoUnavailable,
      copy.email.ssoRequiredSocial,
      copy.sso.refused,
    ];
    for (const text of messages)
      expect(text).not.toMatch(
        /already (has|have) an account|no such|does not exist/i,
      );
  });
});
