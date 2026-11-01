import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { authCatalog } from "../../copy/auth.catalog";
import { authCopy } from "../../copy/auth.copy";
import type { AuthFlowService } from "../../../core";
import type { AuthFlowNavigation } from "../flow-navigation";
import { InviteFlow } from "../invite-flow";
import { ResetFlow } from "../reset-flow";
import { VerifyEmailFlow } from "../verify-email-flow";
import { SignInFlow } from "../sign-in-flow";

const copy = authCopy("vi");

function Providers({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return (
    <I18nProvider catalogs={[authCatalog]} initialLocale="vi">
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </I18nProvider>
  );
}

function navigation() {
  return {
    navigate: vi.fn(),
    startProviderSignIn: vi.fn(),
  } satisfies AuthFlowNavigation;
}

/** Test doubles only implement what a case needs. */
const asService = (service: object) => service as unknown as AuthFlowService;

describe("sign-in flow: provider sign-in", () => {
  it("sends the browser to the backend start path instead of calling it", () => {
    const nav = navigation();
    const service = {
      oauthStartPath: vi.fn(
        (provider: string, returnUrl?: string | null) =>
          `/auth/oauth/${provider}/start?returnUrl=${returnUrl}`,
      ),
    };
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService(service)}
          navigation={nav}
          redirect="/home"
        />
      </Providers>,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.email.google }));
    expect(service.oauthStartPath).toHaveBeenCalledWith("google", "/home");
    expect(nav.startProviderSignIn).toHaveBeenCalledWith(
      "/auth/oauth/google/start?returnUrl=/home",
    );
  });

  it("never hands an unsafe return path to the provider round trip", () => {
    const nav = navigation();
    const service = {
      oauthStartPath: vi.fn(() => "/auth/oauth/google/start"),
    };
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService(service)}
          navigation={nav}
          redirect="https://evil.com"
        />
      </Providers>,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.email.google }));
    expect(service.oauthStartPath).toHaveBeenCalledWith("google", null);
  });

  it("says the account signs in with its password when the provider sign-in needed a second step", () => {
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService({})}
          navigation={navigation()}
          providerError="identity.oauth.mfa-required"
        />
      </Providers>,
    );
    expect(screen.getByRole("alert").textContent).toContain(
      copy.email.mfaRequired,
    );
  });

  it("gives a plain message for any other provider failure", () => {
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService({})}
          navigation={navigation()}
          providerError="something.else"
        />
      </Providers>,
    );
    expect(screen.getByRole("alert").textContent).toContain(
      copy.email.providerFailed,
    );
  });
});

describe("sign-in flow: second step after a provider sign-in", () => {
  const code = "482931";
  const enterCode = () => {
    fireEvent.paste(screen.getByLabelText("Số 1 trên 6"), {
      clipboardData: { getData: () => code },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.otp.verify }));
  };
  const session = {
    kind: "session" as const,
    user: { id: "u", email: "a@b.vn", name: "A", avatarUrl: null },
    expiresAt: null,
  };

  function renderSecondStep(service: object, redirect?: string | null) {
    const nav = navigation();
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService(service)}
          navigation={nav}
          initialStep="mfa"
          redirect={redirect}
        />
      </Providers>,
    );
    return nav;
  }

  it("shows the second step without an email and submits the code without a challenge", async () => {
    const service = { verifyMfa: vi.fn(async () => session) };
    const nav = renderSecondStep(service, "/workspaces/w1/boards/b1");
    expect(
      screen.getByRole("heading", { level: 1, name: copy.otp.title }),
    ).toBeTruthy();
    expect(document.body.textContent).toContain(copy.otp.leadNoEmail);
    enterCode();
    await waitFor(() =>
      expect(nav.navigate).toHaveBeenCalledWith({
        to: "/workspaces/w1/boards/b1",
        replace: true,
      }),
    );
    expect(service.verifyMfa).toHaveBeenCalledWith({
      challengeToken: null,
      code,
    });
  });

  it("goes home when the return path is not an internal one", async () => {
    const service = { verifyMfa: vi.fn(async () => session) };
    const nav = renderSecondStep(service, "//evil.com");
    enterCode();
    await waitFor(() =>
      expect(nav.navigate).toHaveBeenCalledWith({ to: "/home", replace: true }),
    );
  });

  it("says the verification expired when the backend no longer has the challenge", async () => {
    const service = {
      verifyMfa: vi.fn(async () => {
        throw {
          status: 400,
          details: { errorCode: "identity.mfa.challenge-invalid" },
        };
      }),
    };
    renderSecondStep(service);
    enterCode();
    await waitFor(() =>
      expect(screen.getByRole("alert").textContent).toContain(copy.otp.expired),
    );
  });

  it("shows the wait when the API's volume limit answers 429", async () => {
    const service = {
      verifyMfa: vi.fn(async () => {
        throw {
          status: 429,
          details: { errorCode: "rate_limit.exceeded", retryAfter: 30 },
        };
      }),
    };
    renderSecondStep(service);
    enterCode();
    await waitFor(() =>
      expect(screen.getByText(/Thử lại sau \d+ giây/)).toBeTruthy(),
    );
  });

  it("an error on /sign-in wins over the second step", () => {
    const nav = navigation();
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService({})}
          navigation={nav}
          initialStep="mfa"
          providerError="something.else"
        />
      </Providers>,
    );
    expect(screen.getByRole("alert").textContent).toContain(
      copy.email.providerFailed,
    );
  });
});

const preview = {
  state: "valid" as const,
  inviterName: "Nguyễn Thu Hà",
  inviterInitials: "NH",
  orgName: "Minh An Group",
  boardTitle: "Triển khai ERP Q4",
  level: "editor" as const,
  expiresOn: "2027-01-31",
  email: "tuan@doitac.vn",
  name: null,
  hasAccount: false,
  workspaceRole: null,
};

function renderInvite(service: object, nav = navigation()) {
  renderPureUi(
    <Providers>
      <InviteFlow service={asService(service)} navigation={nav} token="tok" />
    </Providers>,
  );
  return nav;
}

function fillAndAccept() {
  fireEvent.change(screen.getByLabelText(copy.invite.nameLabel), {
    target: { value: "Tuấn" },
  });
  fireEvent.change(screen.getByLabelText(copy.invite.passwordLabel), {
    target: { value: "mot-mat-khau-du-dai" },
  });
  fireEvent.click(screen.getByRole("button", { name: copy.invite.accept }));
}

describe("invite flow", () => {
  it("names the workspace and the role when the invitation is to the workspace, not a board", async () => {
    const service = {
      getInvite: vi.fn(async () => ({
        ...preview,
        workspaceRole: "member" as const,
      })),
    };
    renderInvite(service);
    expect(
      await screen.findByText(
        copy.invite.workspaceTitle
          .replace("{{inviter}}", preview.inviterName)
          .replace("{{org}}", preview.orgName),
      ),
    ).toBeTruthy();
    expect(
      screen.getAllByText(new RegExp(copy.invite.workspaceRoles.member)).length,
    ).toBeGreaterThan(0);
    expect(screen.queryByText(copy.invite.seeBody)).toBeNull();
  });

  it("accepts without an account and lands where the backend says", async () => {
    const service = {
      getInvite: vi.fn(async () => preview),
      acceptInviteWithRegistration: vi.fn(async () => ({
        redirectTo: "/workspaces/w1/boards/b1",
        alreadyAccepted: false,
        session: {
          user: { id: "u", email: "a", name: "n", avatarUrl: null },
          expiresAt: null,
        },
      })),
    };
    const nav = renderInvite(service);
    await screen.findByLabelText(copy.invite.nameLabel);
    fillAndAccept();
    await waitFor(() =>
      expect(nav.navigate).toHaveBeenCalledWith({
        to: "/workspaces/w1/boards/b1",
        replace: true,
      }),
    );
    expect(service.acceptInviteWithRegistration).toHaveBeenCalledWith("tok", {
      name: "Tuấn",
      password: "mot-mat-khau-du-dai",
    });
  });

  it("a 409 means the email has an account: the guest is sent to sign in and back to the link", async () => {
    const service = {
      getInvite: vi.fn(async () => preview),
      acceptInviteWithRegistration: vi.fn(async () => {
        throw { status: 409, details: { errorCode: "concurrency.conflict" } };
      }),
    };
    const nav = renderInvite(service);
    await screen.findByLabelText(copy.invite.nameLabel);
    fillAndAccept();
    await waitFor(() =>
      expect(nav.navigate).toHaveBeenCalledWith({
        to: "/sign-in",
        search: { redirect: "/invite/tok" },
        replace: true,
      }),
    );
  });

  it("a second press has no session, so the guest signs in to reach the board", async () => {
    const service = {
      getInvite: vi.fn(async () => preview),
      acceptInviteWithRegistration: vi.fn(async () => ({
        redirectTo: "/workspaces/w1/boards/b1",
        alreadyAccepted: true,
        session: null,
      })),
    };
    const nav = renderInvite(service);
    await screen.findByLabelText(copy.invite.nameLabel);
    fillAndAccept();
    await waitFor(() =>
      expect(nav.navigate).toHaveBeenCalledWith({
        to: "/sign-in",
        search: { redirect: "/workspaces/w1/boards/b1" },
        replace: true,
      }),
    );
  });

  it("an email with an account accepts with the signed-in session, and 401 goes to sign-in", async () => {
    const service = {
      getInvite: vi.fn(async () => ({ ...preview, hasAccount: true })),
      acceptInvite: vi.fn(async () => {
        throw { status: 401 };
      }),
      acceptInviteWithRegistration: vi.fn(),
    };
    const nav = renderInvite(service);
    await screen.findByLabelText(copy.invite.nameLabel);
    fireEvent.click(
      screen.getByRole("button", { name: copy.invite.acceptExisting }),
    );
    await waitFor(() =>
      expect(nav.navigate).toHaveBeenCalledWith({
        to: "/sign-in",
        search: { redirect: "/invite/tok" },
        replace: true,
      }),
    );
    expect(service.acceptInvite).toHaveBeenCalledWith("tok");
    expect(service.acceptInviteWithRegistration).not.toHaveBeenCalled();
  });

  it("shows the backend's word on a field", async () => {
    const service = {
      getInvite: vi.fn(async () => preview),
      acceptInviteWithRegistration: vi.fn(async () => {
        throw {
          status: 400,
          details: { errors: { Password: ["Mật khẩu quá yếu."] } },
        };
      }),
    };
    renderInvite(service);
    await screen.findByLabelText(copy.invite.nameLabel);
    fillAndAccept();
    expect(await screen.findByText("Mật khẩu quá yếu.")).toBeTruthy();
  });

  it("an unknown link is gone", async () => {
    renderInvite({
      getInvite: vi.fn(async () => {
        throw { status: 404 };
      }),
    });
    expect(await screen.findByText(copy.gone.missingTitle)).toBeTruthy();
  });

  it("declines with the token and says so", async () => {
    const service = {
      getInvite: vi.fn(async () => preview),
      declineInvite: vi.fn(async () => undefined),
    };
    renderInvite(service);
    await screen.findByLabelText(copy.invite.nameLabel);
    fireEvent.click(screen.getByRole("button", { name: copy.invite.decline }));
    expect(await screen.findByText(copy.invite.declined.title)).toBeTruthy();
    expect(service.declineInvite).toHaveBeenCalledWith("tok");
  });
});

describe("reset flow: the code and the new password", () => {
  const fill = (code = "123456") => {
    fireEvent.change(screen.getByLabelText(copy.reset.codeLabel), {
      target: { value: code },
    });
    fireEvent.change(screen.getByLabelText(copy.reset.passwordLabel), {
      target: { value: "mot-mat-khau-du-dai" },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.reset.submit }));
  };
  const open = (resetPassword: ReturnType<typeof vi.fn>) => {
    const nav = navigation();
    renderPureUi(
      <Providers>
        <ResetFlow
          service={asService({ resetPassword })}
          navigation={nav}
          initialEmail="ten@congty.vn"
        />
      </Providers>,
    );
    return nav;
  };

  it("sends the email, the code and the password, then goes to sign-in with a notice", async () => {
    const resetPassword = vi.fn().mockResolvedValue(undefined);
    const nav = open(resetPassword);
    fill();
    await waitFor(() =>
      expect(nav.navigate).toHaveBeenCalledWith({
        to: "/sign-in",
        search: { reason: "password-reset" },
        replace: true,
      }),
    );
    expect(resetPassword).toHaveBeenCalledWith({
      email: "ten@congty.vn",
      code: "123456",
      newPassword: "mot-mat-khau-du-dai",
    });
  });

  it.each([
    [
      { status: 400, details: { errorCode: "identity.auth.invalid-code" } },
      "rejected",
    ],
    [
      {
        status: 409,
        details: { errorCode: "identity.auth.too-many-attempts" },
      },
      "tooMany",
    ],
    [{ status: 429, details: {} }, "limited"],
    [{ status: 503, details: {} }, "unreachable"],
  ] as const)(
    "says what went wrong for %j and stays on the page",
    async (failure, key) => {
      const nav = open(vi.fn().mockRejectedValue(failure));
      fill();
      const alert = await screen.findByRole("alert");
      expect(alert.textContent).toContain(copy.reset[key]);
      expect(nav.navigate).not.toHaveBeenCalled();
    },
  );

  it("goes back to the request step with the email when a new code is asked for", () => {
    const nav = open(vi.fn());
    fireEvent.click(screen.getByRole("button", { name: copy.reset.sendAgain }));
    expect(nav.navigate).toHaveBeenCalledWith({
      to: "/forgot-password",
      search: { email: "ten@congty.vn" },
    });
  });
});

describe("sign-in flow: after a password reset", () => {
  it("says the password changed and every device signed out", () => {
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService({})}
          navigation={navigation()}
          passwordReset
        />
      </Providers>,
    );
    expect(screen.getByText(copy.email.passwordReset)).toBeTruthy();
  });
});

describe("verify email flow: the single-use link", () => {
  it("spends the token once however many times the page mounts, and says the address is confirmed", async () => {
    const confirmEmail = vi.fn().mockResolvedValue(undefined);
    const service = asService({ confirmEmail });
    const tree = (
      <Providers>
        <VerifyEmailFlow
          service={service}
          navigation={navigation()}
          token="v1.one-time-token"
        />
      </Providers>
    );
    const first = renderPureUi(tree);
    first.unmount();
    renderPureUi(tree);
    expect(
      await screen.findByRole("heading", { name: copy.verifyEmail.doneTitle }),
    ).toBeTruthy();
    expect(confirmEmail).toHaveBeenCalledTimes(1);
  });
});
