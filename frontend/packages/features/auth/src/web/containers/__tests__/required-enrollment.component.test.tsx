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
const TOKEN = "enroll-token-1";
const enrollment = {
  methodId: "m-1",
  secret: "JBSWY3DPEHPK3PXP",
  otpAuthUri: "otpauth://totp/Notrelix:a@b.vn?secret=JBSWY3DPEHPK3PXP",
};
const session = {
  user: { id: "u", email: "a@b.vn", name: "A", avatarUrl: null },
  expiresAt: null,
};

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
  }) satisfies AuthFlowNavigation;

const asService = (service: object) => service as unknown as AuthFlowService;

const problem = (errorCode: string, status = 400) => ({
  status,
  details: { errorCode },
});

/** Signs in with a password on a service whose login asks for enrolment. */
async function signInIntoEnrolment(service: object) {
  const nav = navigation();
  renderPureUi(
    <Providers>
      <SignInFlow
        service={asService({
          identify: vi.fn(async () => ({ method: "password", sso: null })),
          login: vi.fn(async () => ({
            kind: "enroll",
            enrollmentToken: TOKEN,
            expiresAt: null,
          })),
          ...service,
        })}
        navigation={nav}
        initialEmail="a@b.vn"
      />
    </Providers>,
  );
  fireEvent.change(screen.getByLabelText(copy.email.label), {
    target: { value: "a@b.vn" },
  });
  fireEvent.click(screen.getByRole("button", { name: copy.email.continue }));
  const password = await screen.findByLabelText(copy.password.label);
  fireEvent.change(password, { target: { value: "Pw-123456789" } });
  fireEvent.click(screen.getByRole("button", { name: copy.password.signIn }));
  return nav;
}

const typeCode = async (code: string) => {
  fireEvent.change(await screen.findByLabelText(copy.enroll.codeLabel), {
    target: { value: code },
  });
  fireEvent.click(screen.getByRole("button", { name: copy.enroll.verify }));
};

describe("required enrolment at sign-in", () => {
  it("shows the key and QR in place, then the recovery codes once, then continues into the app", async () => {
    const service = {
      startRequiredEnrollment: vi.fn(async () => enrollment),
      completeRequiredEnrollment: vi.fn(async () => ({
        ...session,
        recoveryCodes: ["AAAA-1111", "BBBB-2222"],
      })),
    };
    const nav = await signInIntoEnrolment(service);
    expect(await screen.findByText(enrollment.secret)).toBeTruthy();
    expect(screen.getByRole("img", { name: copy.enroll.qrLabel })).toBeTruthy();
    expect(service.startRequiredEnrollment).toHaveBeenCalledWith(TOKEN);

    await typeCode("123 456");
    expect(await screen.findByText("AAAA-1111")).toBeTruthy();
    expect(service.completeRequiredEnrollment).toHaveBeenCalledWith({
      enrollmentToken: TOKEN,
      methodId: "m-1",
      code: "123 456",
    });
    // The codes stay until the person says they saved them.
    expect(nav.navigate).not.toHaveBeenCalled();
    fireEvent.click(
      screen.getByRole("button", { name: copy.enroll.codesContinue }),
    );
    expect(nav.navigate).toHaveBeenCalledWith(
      expect.objectContaining({ to: "/home" }),
    );
  });

  it("says a wrong code is wrong and keeps the step open", async () => {
    const service = {
      startRequiredEnrollment: vi.fn(async () => enrollment),
      completeRequiredEnrollment: vi.fn(async () => {
        throw problem("identity.mfa.invalid-code");
      }),
    };
    await signInIntoEnrolment(service);
    await typeCode("000000");
    expect((await screen.findByRole("alert")).textContent).toContain(
      copy.enroll.wrong,
    );
    expect(screen.getByText(enrollment.secret)).toBeTruthy();
    await waitFor(() =>
      expect(
        (screen.getByLabelText(copy.enroll.codeLabel) as HTMLInputElement)
          .value,
      ).toBe(""),
    );
  });

  it("says the set-up expired and closes the code field when the token is gone", async () => {
    const service = {
      startRequiredEnrollment: vi.fn(async () => enrollment),
      completeRequiredEnrollment: vi.fn(async () => {
        throw problem("identity.mfa.challenge-invalid");
      }),
    };
    await signInIntoEnrolment(service);
    await typeCode("123456");
    expect((await screen.findByRole("alert")).textContent).toContain(
      copy.enroll.expired,
    );
    expect(
      (screen.getByLabelText(copy.enroll.codeLabel) as HTMLInputElement)
        .disabled,
    ).toBe(true);
  });

  it("starts from the cookie after a provider sign-in: no token is sent", async () => {
    const service = {
      startRequiredEnrollment: vi.fn(async () => enrollment),
      completeRequiredEnrollment: vi.fn(async () => ({
        ...session,
        recoveryCodes: ["CCCC-3333"],
      })),
    };
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService(service)}
          navigation={navigation()}
          initialStep="enroll"
        />
      </Providers>,
    );
    expect(await screen.findByText(enrollment.secret)).toBeTruthy();
    expect(service.startRequiredEnrollment).toHaveBeenCalledTimes(1);
    expect(service.startRequiredEnrollment).toHaveBeenCalledWith(null);
    await typeCode("123456");
    expect(await screen.findByText("CCCC-3333")).toBeTruthy();
    expect(service.completeRequiredEnrollment).toHaveBeenCalledWith({
      enrollmentToken: null,
      methodId: "m-1",
      code: "123456",
    });
  });

  it("tells a person whose refresh was refused that the Account now requires it", () => {
    renderPureUi(
      <Providers>
        <SignInFlow
          service={asService({})}
          navigation={navigation()}
          mfaEnrollmentRequired
        />
      </Providers>,
    );
    expect(screen.getByText(copy.email.mfaEnrollmentRequired)).toBeTruthy();
  });
});
