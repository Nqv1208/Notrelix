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
